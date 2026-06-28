
using System.Runtime.InteropServices;
using System.Numerics;

public static class SaveLoadData
{
    public static void SaveTrackTriangles(BinaryWriter writer, Triangle[] triangles)
    {
        writer.Write(triangles.Length);

        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(triangles);

        writer.Write(bytes);
    }

    public static Triangle[] LoadTrackTriangles(BinaryReader reader)
    {
        int count = reader.ReadInt32();

        Triangle[] triangles = new Triangle[count];

        Span<byte> bytes = MemoryMarshal.AsBytes(triangles.AsSpan());

        reader.BaseStream.ReadExactly(bytes);

        return triangles;
    }

    public static void SaveTerrain(BinaryWriter writer, Terrain terrain)
    {
        writer.Write(terrain.VertexCount);
        writer.Write(terrain.CellSize);

        ReadOnlySpan<float> heightSpan =
            MemoryMarshal.CreateReadOnlySpan(
                ref terrain.Heights[0, 0],
                terrain.VertexCount * terrain.VertexCount
            );

        ReadOnlySpan<byte> bytes =
            MemoryMarshal.AsBytes(heightSpan);

        writer.Write(bytes);
    }

    public static Terrain LoadTerrain(BinaryReader reader)
    {
        int vertexCount = reader.ReadInt32();
        float cellSize = reader.ReadSingle();

        float[,] heights = new float[vertexCount, vertexCount];

        Span<float> heightSpan =
            MemoryMarshal.CreateSpan(
                ref heights[0, 0],
                vertexCount * vertexCount
            );

        Span<byte> bytes =
            MemoryMarshal.AsBytes(heightSpan);

        reader.BaseStream.ReadExactly(bytes);

        return new Terrain(heights, cellSize);
    }

    public static void SaveVertexData(BinaryWriter writer, Vertex[] vertices)
    {
        writer.Write(vertices.Length);

        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes<Vertex>(vertices);

        writer.Write(bytes);
    }

    public static Vertex[] LoadVertexData(BinaryReader reader)
    {
        int count = reader.ReadInt32();

        Vertex[] vertices = new Vertex[count];

        Span<byte> bytes = MemoryMarshal.AsBytes(vertices.AsSpan());

        reader.BaseStream.ReadExactly(bytes);

        return vertices;
    }

    public static void SaveVertexCells(
        BinaryWriter writer,
        Dictionary<(int x, int z), Vertex[]> cells)
    {
        writer.Write(cells.Count);

        foreach (var pair in cells)
        {
            writer.Write(pair.Key.x);
            writer.Write(pair.Key.z);

            SaveVertexData(writer, pair.Value);
        }
    }

    public static Dictionary<(int x, int z), Vertex[]> LoadVertexCells(
        BinaryReader reader)
    {
        int cellCount = reader.ReadInt32();

        Dictionary<(int x, int z), Vertex[]> cells = new(cellCount);

        for (int i = 0; i < cellCount; i++)
        {
            int x = reader.ReadInt32();
            int z = reader.ReadInt32();

            Vertex[] vertices = LoadVertexData(reader);

            cells.Add((x, z), vertices);
        }

        return cells;
    }

    public static List<CollisionCircle> LoadCollisionTrees(BinaryReader reader)
    {
        int count = reader.ReadInt32();

        List<CollisionCircle> collisionTrees = new(count);

        for (int i = 0; i < count; i++)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float radius = reader.ReadSingle();

            collisionTrees.Add(new CollisionCircle(
                new Vector2(x, y),
                radius
            ));
        }
        return collisionTrees;
    }

    public static void SaveCollisionTrees(BinaryWriter writer, List<CollisionCircle> treeColliders)
    {
        writer.Write(treeColliders.Count);

        foreach (var circle in treeColliders)
        {
            writer.Write(circle.Center.X);
            writer.Write(circle.Center.Y);
            writer.Write(circle.Radius);
        }
    }
}

public class Map
{
    public readonly int Seed;
    public readonly int Size;
    public readonly int CellSize;
    public readonly Terrain terrain;
    public readonly Triangle[] trackTriangles;
    public readonly Dictionary<(int, int), Vertex[]> VertexCells = [];
    public readonly List<CollisionCircle> treeColliders;
    const float collisionCellSize = 100;

    public Map(int seed, int size, int cellSize)
    {
        Seed = seed;
        Size = size;
        CellSize = cellSize;

        if(File.Exists("map.bin"))
        {
            Console.WriteLine("Loading map from map.bin");
            using (var stream = File.OpenRead("map.bin"))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var readSeed = reader.ReadInt32();
                    var readSize = reader.ReadInt32();
                    var readCellSize = reader.ReadInt32();
                    if(readSize != Size || readSeed != Seed || readCellSize != CellSize)
                    {
                        Console.WriteLine($"Map file has different seed, size, or cell size. {Seed}, {Size}, {CellSize}");
                        File.Delete("map.bin");
                    }
                    else
                    {
                        terrain = SaveLoadData.LoadTerrain(reader);
                        trackTriangles = SaveLoadData.LoadTrackTriangles(reader);
                        VertexCells = SaveLoadData.LoadVertexCells(reader);
                        treeColliders = SaveLoadData.LoadCollisionTrees(reader);
                        return;
                    }
                }
            }
        }

        Console.WriteLine($"Generating map with seed: {Seed}, size: {Size}, cell size: {CellSize}");
        var random = new Random(Seed);
        var randomTrack = RandomTrack.Create(Size/3, random, 10, 20, 10, 30, 90);

        terrain = new Terrain(Size, 10, 250, random.Next());

        var track = new Track(8, 20, randomTrack);
        var trackMesh = track.GenerateWithShoulders(Color32.Gray, Color32.Green, terrain);

        trackTriangles = trackMesh.GetTriangles();
        var roadMask = new RoadMask(track.BuildTrackCenterline(), 9, collisionCellSize, 1);
        var mesh = terrain.GenerateMesh(roadMask);
        mesh.AddMesh(trackMesh);
        var groundHeight = new GroundHeight(new RoadCollider(trackTriangles), terrain);
        treeColliders = [];
        mesh.AddMesh(Forest.Create(treeColliders, groundHeight, roadMask, Size * Size, terrain.Size, random));

        var meshGrid = MeshGrid.FromMesh(mesh, CellSize);
        VertexCells = meshGrid.Cells.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Mesh.GetVertexData()
        );
        Console.WriteLine("Saving map to map.bin");

        const string mapPath = "map.bin";
        const string tempPath = "map.tmp";

        using (var stream = File.Create(tempPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Seed);
            writer.Write(Size);
            writer.Write(CellSize);
            SaveLoadData.SaveTerrain(writer, terrain);
            SaveLoadData.SaveTrackTriangles(writer, trackTriangles);
            SaveLoadData.SaveVertexCells(writer, VertexCells);
            SaveLoadData.SaveCollisionTrees(writer, treeColliders);
        }

        if (File.Exists(mapPath))
        {
            File.Delete(mapPath);
        }

        File.Move(tempPath, mapPath);

        Console.WriteLine("Map saved");
    }

    public GroundHeight CreateGroundHeight()
    {
        var roadCollider = new RoadCollider(trackTriangles);
        return new GroundHeight(roadCollider, terrain);
    }

    public TreeColliderGrid CreateTreeColliderGrid()
    {
        var treeColliderGrid = new TreeColliderGrid(collisionCellSize);
        foreach(var t in treeColliders)
        {
            treeColliderGrid.AddCircle(t);
        }
        return treeColliderGrid;
    }
}