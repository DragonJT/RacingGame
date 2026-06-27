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

public class World
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
    public readonly FollowCamera FollowCamera;
    public readonly Material Material;
    public readonly Renderer<Vertex> CarRenderer;
    public readonly Renderer<Vertex> TrackRenderer;
    public readonly CarController CarController;
    public Matrix4x4 View {get; private set;}
    public Matrix4x4 Projection {get; private set;}
    public readonly Terrain Terrain;
    private readonly Ground ground;

    public World()
    {
        Material = Graphics.CreateMaterial(VertexShaderSource, FragmentShaderSource);
        TrackRenderer = Graphics.CreateRenderer<Vertex>();
        var random = new Random(12345);
        var randomTrack = RandomTrack.Create(100, random, 10, 20, 10, 30, 90);
        Terrain = new(200, 200, 10, 250, random.Next());
        var track = new Track(8, 20, randomTrack);
        var trackMesh = track.GenerateWithShoulders(Color32.Gray, Color32.Green, Terrain);

        var terrainMesh = Terrain.GenerateMesh(new RoadMask(track.BuildTrackCenterline(), 20));
        terrainMesh.AddMesh(trackMesh);
        ground = new (Terrain, trackMesh);
        TrackRenderer.SetVertices(terrainMesh.GetRenderVertices(), BufferUsageARB.StaticDraw);

        CarRenderer = Graphics.CreateRenderer<Vertex>();
        var carMesh = Car.Create();
        CarRenderer.SetVertices(carMesh.GetRenderVertices(), BufferUsageARB.StaticDraw);

        CarController = new CarController();
        FollowCamera = new FollowCamera();
    }

    public void Render()
    {
        var windowSize = Graphics.FrameBufferSize;
        Graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        viewport = new Rectangle(0, 0, windowSize.X, windowSize.Y);
        Graphics.Viewport(viewport);
        Graphics.Enable(EnableCap.DepthTest);

        View = FollowCamera.GetViewMatrix(CarController, ground);

        float aspect = windowSize.X/ (float)windowSize.Y;

        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            Library.DegreesToRadians(60.0f),
            aspect,
            0.1f,
            1000.0f
        );

        Material.Use();
        Material.SetVector3("lightPos", new Vector3(2000,2000,2000));
        Material.SetVector3("lightColor", new Vector3(1,1,1));
        Material.SetMatrix4("model", Matrix4x4.Identity);
        Material.SetMatrix4("view", View);
        Material.SetMatrix4("projection", Projection);
        TrackRenderer.Render();

        Material.SetMatrix4("model", CarController.ModelMatrix);
        CarRenderer.Render();
    }

    public void Update(float deltaTime)
    {
        float throttle = 0f;
        float brake = 0f;
        float steer = 0f;

        if (Input.KeyDown(Key.Up))
            throttle = 1f;

        if (Input.KeyDown(Key.Down))
            brake = 1f;

        if (Input.KeyDown(Key.Left))
            steer = 1f;

        if (Input.KeyDown(Key.Right))
            steer = -1f;

        if (Input.KeyPressed(Key.Escape))
        {
            Graphics.Close();
        }
        CarController.Update(ground, deltaTime, throttle, brake, steer);
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
    static World _modelViewer = null!;

    private static void Main()
    {
        Graphics.Init("GameEngine", OnLoad, OnRender, OnClose);
    }

    private static void OnLoad()
    {        
        Graphics.ClearColor(0.08f, 0.09f, 0.12f, 1.0f);
        _modelViewer = new();
    }

    private static void OnRender(float deltaTime)
    {
        _modelViewer.Update(deltaTime);
        _modelViewer.Render();
        TextRenderer.Render();
    }

    private static void OnClose()
    {
        _modelViewer.Delete();
    }
}