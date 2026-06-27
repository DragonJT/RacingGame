
using System.Numerics;

public sealed class ModellingVertex
{
    public Vector3 Position;

    public ModellingVertex(Vector3 position)
    {
        Position = position;
    }

    public ModellingVertex(float x, float y, float z)
    {
        Position = new(x,y,z);
    }
}

public sealed class ModellingFace
{
    public readonly ModellingVertex[] Vertices;
    public Color32 Color;

    public Vector3 GetNormal()
    {
        if (Vertices.Length < 3)
            return Vector3.UnitZ;

        Vector3 a = Vertices[0].Position;
        Vector3 b = Vertices[1].Position;
        Vector3 c = Vertices[2].Position;

        Vector3 ab = b - a;
        Vector3 ac = c - a;

        Vector3 normal = Vector3.Cross(ab, ac);

        if (normal.LengthSquared() == 0)
            return Vector3.UnitZ;

        return Vector3.Normalize(normal);
    }

    public Vector3 GetCenter()
    {
        Vector3 sum = Vector3.Zero;
        int count = 0;

        foreach (var v in Vertices)
        {
            sum += v.Position;
            count++;
        }

        return count == 0 ? Vector3.Zero : sum / count;
    }

    public ModellingFace(ModellingVertex[] vertices, Color32 color)
    {
        Vertices = vertices;
        Color = color;
    }
}

public sealed class ModellingMesh
{
    public readonly List<ModellingFace> Faces = [];

    public void AddBox(Vector3 center, Vector3 size, Color32 color)
    {
        Vector3 h = size * 0.5f;

        var v000 = new ModellingVertex(center + new Vector3(-h.X, -h.Y, -h.Z));
        var v100 = new ModellingVertex(center + new Vector3( h.X, -h.Y, -h.Z));
        var v110 = new ModellingVertex(center + new Vector3( h.X,  h.Y, -h.Z));
        var v010 = new ModellingVertex(center + new Vector3(-h.X,  h.Y, -h.Z));

        var v001 = new ModellingVertex(center + new Vector3(-h.X, -h.Y,  h.Z));
        var v101 = new ModellingVertex(center + new Vector3( h.X, -h.Y,  h.Z));
        var v111 = new ModellingVertex(center + new Vector3( h.X,  h.Y,  h.Z));
        var v011 = new ModellingVertex(center + new Vector3(-h.X,  h.Y,  h.Z));

        // Same style as your Cube() method.
        Faces.Add(new ModellingFace([v000, v010, v110, v100], color)); // back
        Faces.Add(new ModellingFace([v001, v101, v111, v011], color)); // front

        Faces.Add(new ModellingFace([v000, v001, v011, v010], color)); // left
        Faces.Add(new ModellingFace([v100, v110, v111, v101], color)); // right

        Faces.Add(new ModellingFace([v000, v100, v101, v001], color)); // bottom
        Faces.Add(new ModellingFace([v010, v011, v111, v110], color)); // top
    }

    public Vertex[] GetRenderVertices()
    {
        List<Vertex> vertices = [];
        foreach (var face in Faces)
        {
            AddFaceTriangles(vertices, face);
        }
        return [.. vertices];
    }

    private void AddFaceTriangles(List<Vertex> output, ModellingFace face)
    {
        var verts = face.Vertices;

        if (verts.Length < 3)
            return;

        Vector3 normal = face.GetNormal();
        var color = face.Color;
        for (int i = 1; i < verts.Length - 1; i++)
        {
            output.Add(ToRenderVertex(verts[0], normal, color));
            output.Add(ToRenderVertex(verts[i], normal, color));
            output.Add(ToRenderVertex(verts[i + 1], normal, color));
        }
    }

    private static Vertex ToRenderVertex(ModellingVertex v, Vector3 normal, Color32 color)
    {
        return new Vertex(v.Position, normal, color);
    }

    public void AddMesh(ModellingMesh other)
    {
        foreach (var face in other.Faces)
        {
            Faces.Add(face);
        }
    }
}