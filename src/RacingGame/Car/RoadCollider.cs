using System.Numerics;

public sealed class RoadCollider
{
    private readonly Triangle[] triangles;
    private readonly Dictionary<(int x, int z), List<Triangle>> cells = [];

    public readonly float CellSize;
    public readonly int SearchRadiusCells;

    public RoadCollider(
        Triangle[] triangles,
        float cellSize = 50f,
        int searchRadiusCells = 1)
    {
        this.triangles = triangles;
        CellSize = cellSize;
        SearchRadiusCells = searchRadiusCells;

        BuildCells();
    }

    public bool TryGetHeight(float x, float z, out float height)
    {
        Vector2 point = new(x, z);

        int cellX = WorldToCell(x);
        int cellZ = WorldToCell(z);

        for (int dz = -SearchRadiusCells; dz <= SearchRadiusCells; dz++)
        {
            for (int dx = -SearchRadiusCells; dx <= SearchRadiusCells; dx++)
            {
                var key = (cellX + dx, cellZ + dz);

                if (!cells.TryGetValue(key, out var cellTriangles))
                    continue;

                foreach (var t in cellTriangles)
                {
                    if (TryTriangleHeight(point, t.A, t.B, t.C, out height))
                        return true;
                }
            }
        }

        height = 0f;
        return false;
    }

    private void BuildCells()
    {
        foreach (var triangle in triangles)
        {
            AddTriangleToCells(triangle);
        }
    }

    private void AddTriangleToCells(Triangle triangle)
    {
        float minX = MathF.Min(triangle.A.X, MathF.Min(triangle.B.X, triangle.C.X));
        float maxX = MathF.Max(triangle.A.X, MathF.Max(triangle.B.X, triangle.C.X));

        float minZ = MathF.Min(triangle.A.Z, MathF.Min(triangle.B.Z, triangle.C.Z));
        float maxZ = MathF.Max(triangle.A.Z, MathF.Max(triangle.B.Z, triangle.C.Z));

        int minCellX = WorldToCell(minX);
        int maxCellX = WorldToCell(maxX);

        int minCellZ = WorldToCell(minZ);
        int maxCellZ = WorldToCell(maxZ);

        for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
        {
            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var key = (cellX, cellZ);

                if (!cells.TryGetValue(key, out var list))
                {
                    list = [];
                    cells.Add(key, list);
                }

                list.Add(triangle);
            }
        }
    }

    private int WorldToCell(float value)
    {
        return (int)MathF.Floor(value / CellSize);
    }

    private static bool TryTriangleHeight(
        Vector2 point,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        out float height)
    {
        Vector2 p0 = new(a.X, a.Z);
        Vector2 p1 = new(b.X, b.Z);
        Vector2 p2 = new(c.X, c.Z);

        if (!PointInTriangle(point, p0, p1, p2, out float w0, out float w1, out float w2))
        {
            height = 0f;
            return false;
        }

        height =
            a.Y * w0 +
            b.Y * w1 +
            c.Y * w2;

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