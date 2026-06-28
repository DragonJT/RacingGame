using System.Numerics;

public static class Forest
{
    public static ModellingMesh Create(
        List<CollisionCircle> collisionTrees,
        GroundHeight collisions,
        RoadMask roadMask,
        int treeCount,
        float areaSize,
        Random random)
    {
        ModellingMesh mesh = new();

        for (int i = 0; i < treeCount; i++)
        {
            float x = RandomRange(random, -areaSize * 0.5f, areaSize * 0.5f);
            float z = RandomRange(random, -areaSize * 0.5f, areaSize * 0.5f);

            if (roadMask.IsOnRoad(x, z))
            {
                continue;
            }

            float y = collisions.GetHeight(x, z);

            float height = RandomRange(random, 5, 20);
            float trunkWidth = RandomRange(random, 0.6f, 1.4f);
            float canopyWidth = RandomRange(random, 3, 10);

            var tree = Tree.Create(
                position: new Vector3(x, y, z),
                height: height,
                trunkWidth: trunkWidth,
                canopyWidth: canopyWidth,
                trunkColor: Color32.Brown,
                leafColor: Color32.DarkGreen
            );

            if(i % 100 == 0)
            {
                Console.WriteLine($"Tree {i}/{treeCount}");
            }
            collisionTrees.Add(new CollisionCircle(new(x,z), trunkWidth/2f));
            mesh.AddMesh(tree);
        }

        return mesh;
    }

    private static float RandomRange(Random random, float min, float max)
    {
        return min + random.NextSingle() * (max - min);
    }
}