using System.Numerics;

public readonly struct CollisionCircle
{
    public readonly Vector2 Center;
    public readonly float Radius;

    public CollisionCircle(Vector2 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool Intersects(CollisionCircle other)
    {
        float radiusSum = Radius + other.Radius;
        float distanceSquared = Vector2.DistanceSquared(Center, other.Center);

        return distanceSquared <= radiusSum * radiusSum;
    }
}

public sealed class TreeColliderGrid
{
    private readonly Dictionary<(int x, int z), List<CollisionCircle>> cells = [];

    public readonly float CellSize;

    public TreeColliderGrid(float cellSize)
    {
        CellSize = cellSize;
    }

    public void AddTree(Vector3 position, float radius)
    {
        AddCircle(new CollisionCircle(
            new Vector2(position.X, position.Z),
            radius
        ));
    }

    public void AddCircle(CollisionCircle circle)
    {
        int minX = WorldToCell(circle.Center.X - circle.Radius);
        int maxX = WorldToCell(circle.Center.X + circle.Radius);

        int minZ = WorldToCell(circle.Center.Y - circle.Radius);
        int maxZ = WorldToCell(circle.Center.Y + circle.Radius);

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

                list.Add(circle);
            }
        }
    }

    public bool TryGetCollision(
        CollisionCircle carCircle,
        out CollisionCircle treeCircle)
    {
        int minX = WorldToCell(carCircle.Center.X - carCircle.Radius);
        int maxX = WorldToCell(carCircle.Center.X + carCircle.Radius);

        int minZ = WorldToCell(carCircle.Center.Y - carCircle.Radius);
        int maxZ = WorldToCell(carCircle.Center.Y + carCircle.Radius);

        HashSet<CollisionCircle> checkedCircles = [];

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!cells.TryGetValue((x, z), out var list))
                    continue;

                foreach (var circle in list)
                {
                    if (!checkedCircles.Add(circle))
                        continue;

                    if (carCircle.Intersects(circle))
                    {
                        treeCircle = circle;
                        return true;
                    }
                }
            }
        }

        treeCircle = default;
        return false;
    }

    public bool TryGetCollision(
        CollisionCircle[] carCircles,
        out CollisionCircle carCircle,
        out CollisionCircle treeCircle)
    {
        foreach (var circle in carCircles)
        {
            if (TryGetCollision(circle, out treeCircle))
            {
                carCircle = circle;
                return true;
            }
        }

        carCircle = default;
        treeCircle = default;
        return false;
    }

    private int WorldToCell(float value)
    {
        return (int)MathF.Floor(value / CellSize);
    }
}