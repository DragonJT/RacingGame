using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using Silk.NET.Input;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Color32 Color;

    public Vertex(Vector3 position, Vector3 normal, Color32 color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }

    public Vertex(
        float px, float py, float pz,
        float nx, float ny, float nz,
        Color32 color)
    {
        Position = new Vector3(px, py, pz);
        Normal = new Vector3(nx, ny, nz);
        Color = color;
    }
}

class ModelViewer
{
    private const string VertexShaderSource = """
    #version 330 core

    layout (location = 0) in vec3 aPosition;
    layout (location = 1) in vec3 aNormal;
    layout (location = 2) in vec4 aColor;

    out vec3 FragPos;
    out vec3 Normal;
    out vec4 VertexColor;

    uniform mat4 model;
    uniform mat4 view;
    uniform mat4 projection;

    void main()
    {
        FragPos = vec3(model * vec4(aPosition, 1.0));
        Normal = mat3(transpose(inverse(model))) * aNormal;
        VertexColor = aColor;

        gl_Position = projection * view * vec4(FragPos, 1.0);
    }
    """;

private const string FragmentShaderSource = """
    #version 330 core

    in vec3 FragPos;
    in vec3 Normal;
    in vec4 VertexColor;

    out vec4 FragColor;

    uniform vec3 lightColor;
    uniform vec3 lightPos;

    void main()
    {
        float ambientStrength = 0.2;
        vec3 ambient = ambientStrength * lightColor;

        vec3 normal = normalize(Normal);
        vec3 lightDir = normalize(lightPos - FragPos);

        float diffuse = max(dot(normal, lightDir), 0.0);

        vec3 litColor = (ambient + diffuse) * VertexColor.rgb;

        FragColor = vec4(litColor, VertexColor.a);
    }
    """;

    public Rectangle viewport;
    public readonly OrbitCamera OrbitCamera;
    public readonly Material Material;
    public readonly Renderer<Vertex> CarRenderer;
    public readonly Renderer<Vertex> TrackRenderer;
    public Matrix4x4 View {get; private set;}
    public Matrix4x4 Projection {get; private set;}

    public ModelViewer()
    {
        Material = Graphics.CreateMaterial(VertexShaderSource, FragmentShaderSource);
        TrackRenderer = Graphics.CreateRenderer<Vertex>();
        var trackMesh = Track.Create(2, Color32.Black, [
            TrackCommand.Forward(10),
            TrackCommand.Left(5, 8),
            TrackCommand.Forward(5),
            TrackCommand.Right(40, 4)
        ]);
        TrackRenderer.SetVertices(trackMesh.GetRenderVertices(), BufferUsageARB.StaticDraw);

        CarRenderer = Graphics.CreateRenderer<Vertex>();
        var carMesh = Car.Create();
        CarRenderer.SetVertices(carMesh.GetRenderVertices(), BufferUsageARB.StaticDraw);

        OrbitCamera = new OrbitCamera();
    }

    public void Render()
    {
        var windowSize = Graphics.WindowSize;
        Graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        viewport = new Rectangle(0, 0, windowSize.X, windowSize.Y);
        Graphics.Viewport(viewport);
        Graphics.Enable(EnableCap.DepthTest);

        View = OrbitCamera.GetViewMatrix();

        float aspect = windowSize.X/ (float)windowSize.Y;

        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            Library.DegreesToRadians(60.0f),
            aspect,
            0.1f,
            100.0f
        );

        Material.Use();
        Material.SetVector3("lightPos", new Vector3(20,20,20));
        Material.SetVector3("lightColor", new Vector3(1,1,1));
        Material.SetMatrix4("model", Matrix4x4.Identity);
        Material.SetMatrix4("view", View);
        Material.SetMatrix4("projection", Projection);
        TrackRenderer.Render();

        Material.SetMatrix4("model", Matrix4x4.Identity);
        CarRenderer.Render();
    }

    public void Delete()
    {
        Material.Delete();
        TrackRenderer.Delete();
        CarRenderer.Delete();
    }
}

internal static class Program
{ 
    static ModelViewer _modelViewer = null!;

    private static void Main()
    {
        Graphics.Init(
            "GameEngine", 
            OnLoad, 
            OnRender, 
            OnFramebufferResize, 
            OnKeyChar, 
            OnKeyDown, 
            OnMouseMove,
            OnMouseDown,
            OnMouseUp,
            OnMouseScroll,
            OnClose);
    }

    private static void OnLoad()
    {        
        Graphics.ClearColor(0.08f, 0.09f, 0.12f, 1.0f);
        _modelViewer = new();
    }

    private static void OnRender(double deltaTime)
    {
        _modelViewer.Render();
        TextRenderer.Render();
    }

    private static void OnKeyChar(char c)
    {
    }

    private static void OnKeyDown(Key key, int scancode)
    {
    }

    private static void OnMouseMove(Vector2 mousePosition, Vector2 mouseDelta)
    {
        if (Graphics.IsMouseButtonDown(MouseButton.Left))
        {
            _modelViewer.OrbitCamera.Rotate(mouseDelta);
        }
    }

    private static void OnMouseDown(MouseButton mouseButton)
    {
    }

    private static void OnMouseUp(MouseButton mouseButton)
    {
        
    }

    private static void OnMouseScroll(ScrollWheel scrollWheel)
    {
    }

    private static void OnFramebufferResize(Vector2D<int> size)
    {
    }

    private static void OnClose()
    {
        _modelViewer.Delete();
    }
}