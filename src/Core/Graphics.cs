using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using System.Runtime.InteropServices;
using System.Reflection;

public class Material
{
    GL _gl;
    uint _program;

    public Material(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        _program = CreateProgram(vertexSource, fragmentSource);
    }

    public void Use()
    {
        _gl.UseProgram(_program);
    }

    public unsafe void SetMatrix4(string name, Matrix4x4 value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        _gl.UniformMatrix4(location, 1, false, (float*)&value);   
    }

    public void SetVector3(string name, Vector3 value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void SetVector4(string name, Vector4 value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    public void SetInt(string name, int value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        _gl.Uniform1(location, value);
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        uint program = _gl.CreateProgram();

        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);

        if (status == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            throw new Exception($"Shader program failed to link: {infoLog}");
        }

        _gl.DetachShader(program, vertexShader);
        _gl.DetachShader(program, fragmentShader);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);

        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);

        if (status == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"{type} failed to compile: {infoLog}");
        }

        return shader;
    }

    public void Delete()
    {
        _gl.DeleteProgram(_program);
    }
}

public enum GLLayoutType
{
    Float3, Float2, Byte4
}

public class Renderer<T> where T : unmanaged
{
    private GL _gl;
    private uint _vao;
    private uint _vbo;

    private uint _vertexCount;

    private unsafe void EnableAttribute(
        uint location,
        Type fieldType,
        int stride,
        int offset)
    {
        if (fieldType == typeof(float))
        {
            _gl.VertexAttribPointer(
                location,
                1,
                VertexAttribPointerType.Float,
                false,
                (uint)stride,
                (void*)offset
            );
        }
        else if (fieldType == typeof(Vector2))
        {
            _gl.VertexAttribPointer(
                location,
                2,
                VertexAttribPointerType.Float,
                false,
                (uint)stride,
                (void*)offset
            );
        }
        else if (fieldType == typeof(Vector3))
        {
            _gl.VertexAttribPointer(
                location,
                3,
                VertexAttribPointerType.Float,
                false,
                (uint)stride,
                (void*)offset
            );
        }
        else if (fieldType == typeof(Vector4))
        {
            _gl.VertexAttribPointer(
                location,
                4,
                VertexAttribPointerType.Float,
                false,
                (uint)stride,
                (void*)offset
            );
        }
        else if (fieldType == typeof(Color32))
        {
            _gl.VertexAttribPointer(
                location,
                4,
                VertexAttribPointerType.UnsignedByte,
                true,
                (uint)stride,
                (void*)offset
            );
        }
        else
        {
            throw new NotSupportedException(
                $"Unsupported vertex field type '{fieldType.Name}' on vertex '{typeof(T).Name}'."
            );
        }

        _gl.EnableVertexAttribArray(location);
    }

    private void SetupVertexAttributes()
    {
        int stride = Marshal.SizeOf<T>();

        FieldInfo[] fields = typeof(T)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(f => Marshal.OffsetOf<T>(f.Name).ToInt32())
            .ToArray();

        for (uint location = 0; location < fields.Length; location++)
        {
            FieldInfo field = fields[location];
            int offset = Marshal.OffsetOf<T>(field.Name).ToInt32();

            EnableAttribute(location, field.FieldType, stride, offset);
        }
    }

    public Renderer(GL gl)
    {
        _gl = gl;
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        SetupVertexAttributes();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public unsafe void SetVertices(
        ReadOnlySpan<T> vertices,
        BufferUsageARB usage = BufferUsageARB.StaticDraw)
    {
        _vertexCount = (uint)vertices.Length;

        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(vertices);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (byte* v = bytes)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)bytes.Length,
                v,
                usage
            );
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public void Render()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
        _gl.BindVertexArray(0);
    }

    public void Delete()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
    }
}

public class Texture
{
    GL _gl;
    uint _texture;

    public unsafe Texture(GL gl, uint width, uint height, byte[] pixels)
    {
        _gl = gl;
        _texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        fixed (byte* pixelsPtr = pixels)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.R8,
                width,
                height,
                0,
                PixelFormat.Red,
                PixelType.UnsignedByte,
                pixelsPtr
            );
        }

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear
        );

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear
        );

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge
        );

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge
        );

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(uint slot = 0)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)slot);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
    }

    public void Delete()
    {
        _gl.DeleteTexture(_texture);
    }
}

public static class Input
{
    static Vector2 _mousePosition;
    static Vector2 _mouseDelta;
    static List<MouseButton> _mouseDown = [];
    static List<MouseButton> _mousePressed = [];
    static List<Key> _keyDown = [];
    static List<Key> _keyPressed = [];

    public static Vector2 MousePosition => _mousePosition;

    public static Vector2 MouseDelta => _mouseDelta;

    private static Vector2 GetFramebufferMousePosition(Vector2 mousePosition)
    {
        var windowSize = Graphics.WindowSize;
        var framebufferSize = Graphics.FrameBufferSize;

        float scaleX = framebufferSize.X / (float)windowSize.X;
        float scaleY = framebufferSize.Y / (float)windowSize.Y;

        return new Vector2(
            mousePosition.X * scaleX,
            mousePosition.Y * scaleY
        );
    }

    public static bool KeyDown(Key key)
    {
        return _keyDown.Contains(key);
    }

    public static bool KeyPressed(Key key)
    {
        return _keyPressed.Contains(key);
    }

    public static bool MouseDown(MouseButton mouseButton)
    {
        return _mouseDown.Contains(mouseButton);
    }

    public static bool MousePressed(MouseButton mouseButton)
    {
        return _mousePressed.Contains(mouseButton);
    }

    private static void SetKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        _keyPressed.Add(key);
        _keyDown.Add(key);
    }

    private static void SetKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        _keyDown.Remove(key);
    }

    private static void SetMouseDown(IMouse mouse, MouseButton mouseButton)
    {
        _mousePressed.Add(mouseButton);
        _mouseDown.Add(mouseButton);
    }

    private static void SetMouseUp(IMouse mouse, MouseButton mouseButton)
    {
        _mouseDown.Remove(mouseButton);
    }

    private static void SetMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        var mousePos = GetFramebufferMousePosition(mousePosition);
        _mouseDelta += mousePos - _mousePosition;
        _mousePosition = mousePos;
    }

    public static void Init(IWindow _window)
    {
        var input = _window.CreateInput();

        foreach(var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += SetKeyDown;
            keyboard.KeyUp += SetKeyUp;
        }

        foreach (IMouse mouse in input.Mice)
        {
            mouse.MouseDown += SetMouseDown;
            mouse.MouseUp += SetMouseUp;
            mouse.MouseMove += SetMouseMove;
        }
    }

    public static void EndOfFrame()
    {
        _mouseDelta = Vector2.Zero;
        _keyPressed.Clear();
        _mousePressed.Clear();
    }
}

public static class Graphics
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static Action onLoad = null!;
    private static Action<float> onRender = null!;
    private static Action onClose = null!;

    public static void Init(
        string title,
        Action onLoad, 
        Action<float> onRender,
        Action onClose)
    {
        Graphics.onLoad = onLoad;
        Graphics.onClose = onClose;
        Graphics.onRender = onRender;

        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Vector2D<int>(1000, 800);
        options.WindowState = WindowState.Maximized;
        options.WindowBorder = WindowBorder.Resizable;
        options.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.ForwardCompatible,
            new APIVersion(3, 3)
        );

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClose;

        _window.Run();
    }

    public static Vector2D<int> WindowSize => _window.Size;
    public static Vector2D<int> FrameBufferSize => _window.FramebufferSize;

    public static void ClearColor(float r, float g, float b, float a)
    {
        _gl.ClearColor(r,g,b,a);
    }

    public static void Clear(ClearBufferMask mask)
    {
        _gl.Clear(mask);
    }

    public static void Viewport(Vector2D<int> size)
    {
        _gl.Viewport(size);
    }

    public static void Viewport(Rectangle viewport)
    {
        _gl.Viewport((int)viewport.X, (int)viewport.Y, (uint)viewport.Width, (uint)viewport.Height);
    }

    public static void Enable(EnableCap cap)
    {
        _gl.Enable(cap);
    }

    public static void Disable(EnableCap cap)
    {
        _gl.Disable(cap);
    }

    public static void BlendFunc(BlendingFactor sfactor, BlendingFactor dfactor)
    {
        _gl.BlendFunc(sfactor, dfactor);
    }

    public static double Time => _window.Time;

    private static void OnLoad()
    {
        _gl = _window.CreateOpenGL();
        Input.Init(_window);
        TextRenderer.OnLoad();
        onLoad();
    }

    private static void OnRender(double deltaTime)
    {
        onRender((float)deltaTime);
        Input.EndOfFrame();
    }

    public static void Close()
    {
        _window.Close();
    }

    private static void OnClose()
    {
        _gl.Dispose();
        onClose();
    }

    public static Material CreateMaterial(string vertexSource, string fragmentSource)
    {
        return new Material(_gl, vertexSource, fragmentSource);
    }

    public static Renderer<T> CreateRenderer<T>() where T: unmanaged
    {
        return new Renderer<T>(_gl);
    }

    public static Texture CreateTexture(uint width, uint height, byte[] pixels)
    {
        return new Texture(_gl, width, height, pixels);
    }
}