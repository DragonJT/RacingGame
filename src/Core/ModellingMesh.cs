
using System.Numerics;
using System.Runtime.CompilerServices;

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

    public ModellingFace(ModellingVertex[] vertices)
    {
        Vertices = vertices;
    }
}

public enum SelectionMode
{
    Vertex,
    Face,
    Edge,
}

public readonly struct ModellingEdge : IEquatable<ModellingEdge>
{
    public readonly ModellingVertex A;
    public readonly ModellingVertex B;

    public ModellingEdge(ModellingVertex a, ModellingVertex b)
    {
        A = a;
        B = b;
    }

    public bool Equals(ModellingEdge other)
    {
        return ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B)
            || ReferenceEquals(A, other.B) && ReferenceEquals(B, other.A);
    }

    public override bool Equals(object? obj)
    {
        return obj is ModellingEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashA = RuntimeHelpers.GetHashCode(A);
        int hashB = RuntimeHelpers.GetHashCode(B);

        return hashA ^ hashB;
    }
}

public sealed class ModellingMesh
{
    public SelectionMode SelectionMode{get;set;} = SelectionMode.Face;
    public readonly List<ModellingFace> Faces = [];
    public readonly HashSet<ModellingFace> SelectedFaces = [];
    public readonly HashSet<ModellingVertex> SelectedVertices = [];
    public readonly HashSet<ModellingEdge> SelectedEdges = [];

    public void SelectFace(ModellingFace modellingFace)
    {
        if(SelectionMode == SelectionMode.Face)
        {
            SelectedFaces.Add(modellingFace);
        }
        else if(SelectionMode == SelectionMode.Vertex)
        {
            foreach(var v in modellingFace.Vertices)
            {
                SelectedVertices.Add(v);
            }
        }
        else if(SelectionMode == SelectionMode.Edge)
        {
            var verts = modellingFace.Vertices;
            for(var i = 0; i < verts.Length; i++)
            {
                SelectedEdges.Add(new (verts[i], verts[(i+1)%verts.Length]));
            }
        }
    }

    private void AddSelectedFace(ModellingFace modellingFace)
    {
        Faces.Add(modellingFace);
        SelectFace(modellingFace);
    }

    public void Cube()
    {
        SelectedFaces.Clear();
        SelectedVertices.Clear();
        SelectedEdges.Clear();

        var v000 = new ModellingVertex(-0.5f, -0.5f, -0.5f);
        var v100 = new ModellingVertex( 0.5f, -0.5f, -0.5f);
        var v110 = new ModellingVertex( 0.5f,  0.5f, -0.5f);
        var v010 = new ModellingVertex(-0.5f,  0.5f, -0.5f);

        var v001 = new ModellingVertex(-0.5f, -0.5f,  0.5f);
        var v101 = new ModellingVertex( 0.5f, -0.5f,  0.5f);
        var v111 = new ModellingVertex( 0.5f,  0.5f,  0.5f);
        var v011 = new ModellingVertex(-0.5f,  0.5f,  0.5f);

        // Winding order: counter-clockwise when viewed from outside.
        AddSelectedFace(new ModellingFace([v000, v010, v110, v100])); // back
        AddSelectedFace(new ModellingFace([v001, v101, v111, v011])); // front

        AddSelectedFace(new ModellingFace([v000, v001, v011, v010])); // left
        AddSelectedFace(new ModellingFace([v100, v110, v111, v101])); // right

        AddSelectedFace(new ModellingFace([v000, v100, v101, v001])); // bottom
        AddSelectedFace(new ModellingFace([v010, v011, v111, v110])); // top
    }

    public void Inset(float scale)
    {
        Extrude(0);
        Scale(scale);
    }

    public void Extrude(float dist)
    {
        var oldSelectedFaces = SelectedFaces.ToArray();

        SelectedFaces.Clear();
        SelectedVertices.Clear();
        SelectedEdges.Clear();

        foreach (var face in oldSelectedFaces)
        {
            ExtrudeFace(face, dist);
        }
    }

    private void ExtrudeFace(ModellingFace face, float dist)
    {
        var oldVerts = face.Vertices;
        var normal = GetFaceNormal(face);

        var newVerts = new ModellingVertex[oldVerts.Length];

        for (int i = 0; i < oldVerts.Length; i++)
        {
            var v = oldVerts[i];

            newVerts[i] = new ModellingVertex(v.Position + normal * dist);
        }

        // New outer face.
        var topFace = new ModellingFace(newVerts);
        Faces.Add(topFace);
        Faces.Remove(face);

        // Side faces.
        for (int i = 0; i < oldVerts.Length; i++)
        {
            var oldA = oldVerts[i];
            var oldB = oldVerts[(i + 1) % oldVerts.Length];

            var newA = newVerts[i];
            var newB = newVerts[(i + 1) % newVerts.Length];

            Faces.Add(new ModellingFace([
                oldA,
                oldB,
                newB,
                newA
            ]));
        }

        SelectFace(topFace);
    }

    public void Scale(float scale)
    {
        foreach(var f in SelectedFaces)
        {
            Vector3 center = GetCenter(f.Vertices);

            foreach (var vertex in f.Vertices)
            {
                vertex.Position = center + (vertex.Position - center) * scale;
            }
        }
    }

    private static Vector3 GetFaceNormal(ModellingFace face)
    {
        var verts = face.Vertices;

        if (verts.Length < 3)
            return Vector3.UnitZ;

        Vector3 a = verts[0].Position;
        Vector3 b = verts[1].Position;
        Vector3 c = verts[2].Position;

        Vector3 ab = b - a;
        Vector3 ac = c - a;

        Vector3 normal = Vector3.Cross(ab, ac);

        if (normal.LengthSquared() == 0)
            return Vector3.UnitZ;

        return Vector3.Normalize(normal);
    }

    private static Vector3 GetCenter(IEnumerable<ModellingVertex> vertices)
    {
        Vector3 sum = Vector3.Zero;
        int count = 0;

        foreach (var v in vertices)
        {
            sum += v.Position;
            count++;
        }

        return count == 0 ? Vector3.Zero : sum / count;
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

        Vector3 normal = GetFaceNormal(face);
        var color = SelectedFaces.Contains(face) ? Color32.Orange : Color32.SkyBlue;
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
}