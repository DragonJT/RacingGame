using System.Numerics;

public sealed class RoadMask
{
    private readonly List<Vector3> points;
    private readonly float halfWidth;

    public RoadMask(List<Vector3> points, float width)
    {
        this.points = points;
        halfWidth = width * 0.5f;
    }

    public bool IsOnRoad(float x, float z)
    {
        return DistanceToRoad(x, z) <= halfWidth;
    }

    private float DistanceToRoad(float x, float z)
    {
        Vector2 p = new(x, z);

        float closest = float.MaxValue;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = new(points[i].X, points[i].Z);
            Vector2 b = new(points[i + 1].X, points[i + 1].Z);

            float distance = DistancePointToSegment(p, a, b);

            if (distance < closest)
                closest = distance;
        }

        return closest;
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