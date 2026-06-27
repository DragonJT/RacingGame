using System.Numerics;

public sealed class Ground
{
    private readonly Terrain terrain;
    private readonly List<ModellingFace> faces;

    public Ground(Terrain terrain, ModellingMesh roadMesh)
    {
        this.terrain = terrain;
        faces = roadMesh.Faces;
    }

    public float GetHeight(float x, float z)
    {
        if(TryGetHeight(x, z, out var height))
        {
            return height;
        }
        return terrain.GetHeight(x, z);
    }

    private bool TryGetHeight(float x, float z, out float height)
    {
        Vector2 point = new(x, z);

        foreach (var face in faces)
        {
            var verts = face.Vertices;

            if (verts.Length < 3)
                continue;

            // Your road faces are quads, so split each quad into 2 triangles.
            if (TryTriangleHeight(point, verts[0], verts[1], verts[2], out height))
                return true;

            if (verts.Length >= 4 &&
                TryTriangleHeight(point, verts[0], verts[2], verts[3], out height))
                return true;
        }

        height = 0f;
        return false;
    }

    private static bool TryTriangleHeight(
        Vector2 point,
        ModellingVertex a,
        ModellingVertex b,
        ModellingVertex c,
        out float height)
    {
        Vector2 p0 = new(a.Position.X, a.Position.Z);
        Vector2 p1 = new(b.Position.X, b.Position.Z);
        Vector2 p2 = new(c.Position.X, c.Position.Z);

        if (!PointInTriangle(point, p0, p1, p2, out float w0, out float w1, out float w2))
        {
            height = 0f;
            return false;
        }

        height =
            a.Position.Y * w0 +
            b.Position.Y * w1 +
            c.Position.Y * w2;

        return true;
    }

    private static bool PointInTriangle(
        Vector2 p,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        out float wa,
        out float wb,
        out float wc)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;

        if (MathF.Abs(denom) < 0.000001f)
        {
            wa = wb = wc = 0f;
            return false;
        }

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        wa = u;
        wb = v;
        wc = w;

        const float epsilon = 0.001f;

        return
            u >= -epsilon &&
            v >= -epsilon &&
            w >= -epsilon;
    }
}