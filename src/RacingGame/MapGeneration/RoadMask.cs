using System.Numerics;

public sealed class RoadMask
{
    private readonly List<Vector3> points;
    private readonly float radius;
    private readonly float cellSize;
    private readonly int searchRadiusCells;

    private readonly Dictionary<(int x, int z), List<RoadSegment>> cells = [];

    public RoadMask(
        List<Vector3> points,
        float radius,
        float cellSize,
        int searchRadiusCells = 1)
    {
        this.points = points;
        this.radius = radius;
        this.cellSize = cellSize;
        this.searchRadiusCells = searchRadiusCells;

        BuildCells();
    }

    public bool IsOnRoad(float x, float z)
    {
        return DistanceToRoad(x, z) <= radius;
    }

    private float DistanceToRoad(float x, float z)
    {
        Vector2 p = new(x, z);

        int cellX = WorldToCell(x);
        int cellZ = WorldToCell(z);

        float closest = float.MaxValue;

        for (int dz = -searchRadiusCells; dz <= searchRadiusCells; dz++)
        {
            for (int dx = -searchRadiusCells; dx <= searchRadiusCells; dx++)
            {
                var key = (cellX + dx, cellZ + dz);

                if (!cells.TryGetValue(key, out var segments))
                    continue;

                foreach (var segment in segments)
                {
                    float distance = DistancePointToSegment(
                        p,
                        segment.A,
                        segment.B
                    );

                    if (distance < closest)
                        closest = distance;
                }
            }
        }

        return closest;
    }

    private void BuildCells()
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = new(points[i].X, points[i].Z);
            Vector2 b = new(points[i + 1].X, points[i + 1].Z);

            RoadSegment segment = new(a, b);

            int minX = WorldToCell(MathF.Min(a.X, b.X) - radius);
            int maxX = WorldToCell(MathF.Max(a.X, b.X) + radius);

            int minZ = WorldToCell(MathF.Min(a.Y, b.Y) - radius);
            int maxZ = WorldToCell(MathF.Max(a.Y, b.Y) + radius);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var key = (x, z);

                    if (!cells.TryGetValue(key, out var list))
                    {
                        list = [];
                        cells.Add(key, list);
                    }

                    list.Add(segment);
                }
            }
        }
    }

    private int WorldToCell(float value)
    {
        return (int)MathF.Floor(value / cellSize);
    }

    private readonly struct RoadSegment
    {
        public readonly Vector2 A;
        public readonly Vector2 B;

        public RoadSegment(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        float lengthSquared = ab.LengthSquared();

        if (lengthSquared < 0.0001f)
            return Vector2.Distance(p, a);

        float t = Vector2.Dot(p - a, ab) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);

        Vector2 closest = a + ab * t;

        return Vector2.Distance(p, closest);
    }
}