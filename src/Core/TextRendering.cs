using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using static StbTrueTypeSharp.StbTrueType;

public struct Glyph
{
    public Vector2 Size;
    public Vector2 Bearing;
    public float Advance;
    public Vector2 UVMin;
    public Vector2 UVMax;
}

public sealed unsafe class Font : IDisposable
{
    public Texture Texture {get;}
    public int PixelSize { get; }
    public int AtlasWidth { get; }
    public int AtlasHeight { get; }

    public float Ascent { get; }
    public float Descent { get; }
    public float LineGap { get; }
    public float LineHeight { get; }

    public Vector2 WhitePixelUV { get; private set; }

    public Dictionary<char, Glyph> Glyphs { get; } = new();

    public Font(string path, int pixelSize)
    {
        PixelSize = pixelSize;

        AtlasWidth = 1024;
        AtlasHeight = 1024;

        byte[] fontData = File.ReadAllBytes(path);
        stbtt_fontinfo info = new stbtt_fontinfo();

        fixed (byte* fontPtr = fontData)
        if (stbtt_InitFont(info, fontPtr, 0) == 0)
            throw new Exception("Failed to initialize font.");

        int ascent;
        int descent;
        int lineGap;
        stbtt_GetFontVMetrics(info, &ascent, &descent, &lineGap);

        float fontScale = stbtt_ScaleForPixelHeight(info, pixelSize);

        Ascent = ascent * fontScale;
        Descent = descent * fontScale;
        LineGap = lineGap * fontScale;
        LineHeight = (ascent - descent + lineGap) * fontScale;

        byte[] atlasPixels = new byte[AtlasWidth * AtlasHeight];

        const int firstChar = 32;
        const int charCount = 95; // ASCII 32..126

        stbtt_packedchar[] packedChars = new stbtt_packedchar[charCount];

        fixed (byte* fontPtr = fontData)
        fixed (byte* pixelsPtr = atlasPixels)
        fixed (stbtt_packedchar* packedPtr = packedChars)
        {
            var context = new stbtt_pack_context();

            int beginResult = stbtt_PackBegin(
                context,
                pixelsPtr,
                AtlasWidth,
                AtlasHeight,
                0,
                1,
                null
            );

            if (beginResult == 0)
                throw new Exception("Failed to begin font packing.");

            stbtt_PackSetOversampling(context, 2, 2);

            int packResult = stbtt_PackFontRange(
                context,
                fontPtr,
                0,
                pixelSize,
                firstChar,
                charCount,
                packedPtr
            );

            stbtt_PackEnd(context);

            if (packResult == 0)
                throw new Exception("Failed to pack font. Try increasing the atlas size.");

            for (int i = 0; i < charCount; i++)
            {
                char c = (char)(firstChar + i);
                stbtt_packedchar p = packedChars[i];

                float x0 = p.x0;
                float y0 = p.y0;
                float x1 = p.x1;
                float y1 = p.y1;

                Glyphs[c] = new Glyph
                {
                     Size = new Vector2(p.xoff2 - p.xoff, p.yoff2 - p.yoff),

                    // Offset from cursor/baseline to glyph quad top-left
                    Bearing = new Vector2(p.xoff, p.yoff),

                    // Cursor movement after this glyph
                    Advance = p.xadvance,

                    // Texture coordinates inside atlas
                    UVMin = new Vector2(x0 / AtlasWidth, y0 / AtlasHeight),
                    UVMax = new Vector2(x1 / AtlasWidth, y1 / AtlasHeight)
                };
            }
        }

        int whiteX = AtlasWidth - 1;
        int whiteY = AtlasHeight - 1;

        atlasPixels[whiteY * AtlasWidth + whiteX] = 255;

        WhitePixelUV = new Vector2(
            (whiteX + 0.5f) / AtlasWidth,
            (whiteY + 0.5f) / AtlasHeight
        );

        Texture = Graphics.CreateTexture((uint)AtlasWidth, (uint)AtlasHeight, atlasPixels);
    }

    public void Bind(uint slot)
    {
        Texture.Bind(slot);
    }

    public bool TryGetGlyph(char c, out Glyph glyph)
    {
        return Glyphs.TryGetValue(c, out glyph);
    }

    public void Dispose()
    {
        Texture.Delete();
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct TextVertex
{
    public Vector2 Position;
    public Vector2 UV;
    public Color32 Color;

    public TextVertex(Vector2 position, Vector2 uv, Color32 color)
    {
        Position = position;
        UV = uv;
        Color = color;
    }
}

public static class TextRenderer
{
    static Material _material = null!;
    static Renderer<TextVertex> _renderer = null!;
    static readonly List<TextVertex> _vertices = [];
    static Font _font = null!;

    static readonly string vertexSource = """
    #version 330 core

    layout (location = 0) in vec2 aPosition;
    layout (location = 1) in vec2 aTexCoord;
    layout (location = 2) in vec4 aColor;

    uniform mat4 projection;

    out vec2 texCoord;
    out vec4 vertexColor;

    void main()
    {
        texCoord = aTexCoord;
        vertexColor = aColor;
        gl_Position = projection * vec4(aPosition, 0.0, 1.0);
    }
    """;

    static readonly string fragmentSource = """
    #version 330 core

    in vec2 texCoord;
    in vec4 vertexColor;

    uniform sampler2D fontAtlas;

    out vec4 FragColor;

    void main()
    {
        float alpha = texture(fontAtlas, texCoord).r;
        FragColor = vec4(vertexColor.rgb, vertexColor.a * alpha);
    }
    """;

    public static void OnLoad()
    {
        _material = Graphics.CreateMaterial(vertexSource, fragmentSource);
        _renderer = Graphics.CreateRenderer<TextVertex>();
        _font = new Font("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 48);
    }

    public static void DrawText(Vector2 position, string text, float scale, Color32 color)
    {
        float x = position.X;
        float y = position.Y + _font.Ascent * scale;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                x = position.X;
                y += _font.PixelSize * scale;
                continue;
            }

            if (!_font.Glyphs.TryGetValue(c, out Glyph glyph))
                continue;

            float xpos = x + glyph.Bearing.X * scale;
            float ypos = y + glyph.Bearing.Y * scale;

            float w = glyph.Size.X * scale;
            float h = glyph.Size.Y * scale;

            Vector2 uvMin = glyph.UVMin;
            Vector2 uvMax = glyph.UVMax;

            // Triangle 1
            AddVertex(xpos,     ypos,     uvMin.X, uvMin.Y, color);
            AddVertex(xpos,     ypos + h, uvMin.X, uvMax.Y, color);
            AddVertex(xpos + w, ypos + h, uvMax.X, uvMax.Y, color);

            // Triangle 2
            AddVertex(xpos,     ypos,     uvMin.X, uvMin.Y, color);
            AddVertex(xpos + w, ypos + h, uvMax.X, uvMax.Y, color);
            AddVertex(xpos + w, ypos,     uvMax.X, uvMin.Y, color);

            x += glyph.Advance * scale;
        }
    }

    public static void DrawRect(Rectangle rect, Color32 color)
    {
        Vector2 uv = _font.WhitePixelUV;

        float x = rect.X;
        float y = rect.Y;
        float w = rect.Width;
        float h = rect.Height;

        AddVertex(x,     y,     uv.X, uv.Y, color);
        AddVertex(x,     y + h, uv.X, uv.Y, color);
        AddVertex(x + w, y + h, uv.X, uv.Y, color);

        AddVertex(x,     y,     uv.X, uv.Y, color);
        AddVertex(x + w, y + h, uv.X, uv.Y, color);
        AddVertex(x + w, y,     uv.X, uv.Y, color);
    }

    static void AddVertex(float x, float y, float u, float v, Color32 color)
    {
        _vertices.Add(new TextVertex(new (x, y), new (u, v), color));
    }

    public static void Render()
    {
        if (_vertices.Count == 0)
            return;

        var windowSize = Graphics.WindowSize;
        var viewport = new Rectangle(0, 0, windowSize.X, windowSize.Y);
        Graphics.Viewport(viewport);
        Graphics.Disable(EnableCap.DepthTest);
        Graphics.Enable(EnableCap.Blend);
        Graphics.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var projection = Matrix4x4.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -1, 1);

        _material.Use();
        _material.SetMatrix4("projection", projection);

        _font.Bind(0);
        _material.SetInt("fontAtlas", 0);

        _renderer.SetVertices(CollectionsMarshal.AsSpan(_vertices));
        _renderer.Render();
        _vertices.Clear();
    }    

    public static float LineHeight(float scale)
    {
        return _font.LineHeight * scale;
    }
}