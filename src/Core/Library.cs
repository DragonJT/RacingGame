
using System.Numerics;

public readonly struct Color32
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public Color32(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color32 White => new(255, 255, 255, 255);
    public static Color32 Black => new(0, 0, 0, 255);
    public static Color32 Red => new(255, 0, 0, 255);
    public static Color32 Green => new(0, 255, 0, 255);
    public static Color32 DarkGreen => new(0, 100, 0, 255);
    public static Color32 Blue => new(0, 0, 255, 255);
    public static Color32 Orange => new(255, 150, 0, 255);
    public static Color32 SkyBlue => new(0, 150, 255, 255);
    public static Color32 Gray => new(100, 100, 100, 255);
    public static Color32 Yellow => new(255, 255, 0, 255);
}

public struct Rectangle
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= X &&
               point.Y >= Y &&
               point.X < X + Width &&
               point.Y < Y + Height;
    }

    public override string ToString()
    {
        return $"({X},{Y},{Width},{Height})";
    }
}

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }
}

public static class Library
{
    public static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180.0f;
    }
}