

using System.Runtime.InteropServices;

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
}

public class Map
{
    public int seed;
    public int size;
    public Terrain terrain;
    public Triangle[] trackTriangles;
    public Vertex[] vertexData;

    public Map(int seed, int size)
    {
        this.seed = seed;
        this.size = size;

        if(File.Exists("map.bin"))
        {
            Console.WriteLine("Loading map from map.bin");
            using (var stream = File.OpenRead("map.bin"))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var readSeed = reader.ReadInt32();
                    var readSize = reader.ReadInt32();
                    if(readSize != size || readSeed != seed)
                    {
                        Console.WriteLine($"Map file has different seed or size. Expected seed: {seed}, size: {size}");
                        File.Delete("map.bin");
                    }
                    else
                    {
                        terrain = SaveLoadData.LoadTerrain(reader);
                        trackTriangles = SaveLoadData.LoadTrackTriangles(reader);
                        vertexData = SaveLoadData.LoadVertexData(reader);
                        Console.WriteLine($"Map loaded with seed: {seed}, size: {size}, vertices: {vertexData.Length}");
                        return;
                    }
                }
            }
        }

        Console.WriteLine($"Generating map with seed: {seed}, size: {size}");
        var random = new Random(seed);
        var randomTrack = RandomTrack.Create(size/3, random, 10, 20, 10, 30, 90);
        terrain = new Terrain(size, 10, 250, random.Next());
        var track = new Track(8, 20, randomTrack);
        var trackMesh = track.GenerateWithShoulders(Color32.Gray, Color32.Green, terrain);

        trackTriangles = trackMesh.GetTriangles();
        var roadMask = new RoadMask(track.BuildTrackCenterline(), 20);
        var mesh = terrain.GenerateMesh(roadMask);
        mesh.AddMesh(trackMesh);
        mesh.AddMesh(Forest.Create(terrain, roadMask, 10000, terrain.Size, random));
        vertexData = mesh.GetVertexData();

        Console.WriteLine($"Map generated with {vertexData.Length} vertices");

        Console.WriteLine("Saving map to map.bin");

        const string mapPath = "map.bin";
        const string tempPath = "map.tmp";

        using (var stream = File.Create(tempPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(this.seed);
            writer.Write(this.size);

            SaveLoadData.SaveTerrain(writer, terrain);
            SaveLoadData.SaveTrackTriangles(writer, trackTriangles);
            SaveLoadData.SaveVertexData(writer, vertexData);
        }

        if (File.Exists(mapPath))
        {
            File.Delete(mapPath);
        }

        File.Move(tempPath, mapPath);

        Console.WriteLine("Map saved");
    }

    public Collisions CreateCollisions()
    {
        var roadCollider = new RoadCollider(trackTriangles);
        return new Collisions(roadCollider, terrain);
    }
}