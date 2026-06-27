using System.Numerics;

public sealed class Terrain
{
    private readonly int cells;
    private readonly float cellSize;
    private readonly float halfSize;
    public float Size => cells * cellSize;

    private readonly float[,] heights;

    public Terrain(
        int cells,
        float cellSize,
        float heightScale,
        int seed)
    {
        this.cells = cells;
        this.cellSize = cellSize;

        halfSize = cells * cellSize * 0.5f;

        heights = new float[cells + 1, cells + 1];

        SmoothNoise noise = new(seed);

        for (int z = 0; z <= cells; z++)
        {
            for (int x = 0; x <= cells; x++)
            {
                float worldX = x * cellSize - halfSize;
                float worldZ = z * cellSize - halfSize;

                float height = GetGeneratedHeight(worldX, worldZ, noise, heightScale);

                heights[x, z] = height;
            }
        }
    }

    public ModellingMesh GenerateMesh(RoadMask roadMask)
    {
        ModellingMesh mesh = new();
        ModellingVertex[,] vertices = new ModellingVertex[cells + 1, cells + 1];

        for (int z = 0; z <= cells; z++)
        {
            for (int x = 0; x <= cells; x++)
            {
                float worldX = x * cellSize - halfSize;
                float worldZ = z * cellSize - halfSize;

                var offset = 0;
                if (roadMask.IsOnRoad(worldX, worldZ))
                {
                    offset = -5;
                }
                vertices[x, z] = new ModellingVertex(worldX,  heights[x, z] + offset, worldZ);
            }
        }

        for (int z = 0; z < cells; z++)
        {
            for (int x = 0; x < cells; x++)
            {
                var v00 = vertices[x, z];
                var v10 = vertices[x + 1, z];
                var v11 = vertices[x + 1, z + 1];
                var v01 = vertices[x, z + 1];

                float centerHeight =
                    (v00.Position.Y +
                     v10.Position.Y +
                     v11.Position.Y +
                     v01.Position.Y) * 0.25f;

                Color32 color = GetTerrainColor(centerHeight);

                mesh.Faces.Add(new ModellingFace([
                    v00,
                    v01,
                    v11,
                    v10
                ], color));
            }
        }

        return mesh;
    }

    public float GetHeight(float worldX, float worldZ)
    {
        float gridX = (worldX + halfSize) / cellSize;
        float gridZ = (worldZ + halfSize) / cellSize;

        if (gridX < 0 || gridZ < 0 || gridX >= cells || gridZ >= cells)
            return 0f;

        int x0 = (int)MathF.Floor(gridX);
        int z0 = (int)MathF.Floor(gridZ);

        int x1 = x0 + 1;
        int z1 = z0 + 1;

        float tx = gridX - x0;
        float tz = gridZ - z0;

        float h00 = heights[x0, z0];
        float h10 = heights[x1, z0];
        float h01 = heights[x0, z1];
        float h11 = heights[x1, z1];

        float hx0 = Lerp(h00, h10, tx);
        float hx1 = Lerp(h01, h11, tx);

        return Lerp(hx0, hx1, tz);
    }

    public Vector3 GetNormalAt(float worldX, float worldZ)
    {
        float sampleDistance = cellSize;

        float hL = GetHeight(worldX - sampleDistance, worldZ);
        float hR = GetHeight(worldX + sampleDistance, worldZ);
        float hD = GetHeight(worldX, worldZ - sampleDistance);
        float hU = GetHeight(worldX, worldZ + sampleDistance);

        Vector3 normal = new Vector3(
            hL - hR,
            sampleDistance * 2f,
            hD - hU
        );

        if (normal.LengthSquared() < 0.0001f)
            return Vector3.UnitY;

        return Vector3.Normalize(normal);
    }

    private static float GetGeneratedHeight(
        float x,
        float z,
        SmoothNoise noise,
        float heightScale)
    {
        float largeHills = noise.Noise(
            x,
            z,
            frequency: 0.0025f,
            amplitude: 1.0f
        );

        float smallDetails = noise.Noise(
            x,
            z,
            frequency: 0.01f,
            amplitude: 0.1f
        );

        return (smallDetails + largeHills + 0.6f) * heightScale;
    }

    private static Color32 GetTerrainColor(float height)
    {
        if (height < -40.0f)
            return Color32.DarkGreen;

        if (height < 15f)
            return Color32.Green;

        if (height < 100)
            return Color32.Black;

        return Color32.White;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}