using System.Numerics;

public sealed class MeshCell
{
    public readonly int X;
    public readonly int Z;
    public readonly ModellingMesh Mesh = new();

    public MeshCell(int x, int z)
    {
        X = x;
        Z = z;
    }
}

public sealed class MeshGrid
{
    public readonly float CellSize;
    public readonly Dictionary<(int x, int z), MeshCell> Cells = [];

    public MeshGrid(float cellSize)
    {
        CellSize = cellSize;
    }

    public void AddFace(ModellingFace face)
    {
        Vector3 center = face.GetCenter();

        int cellX = WorldToCell(center.X);
        int cellZ = WorldToCell(center.Z);

        var key = (cellX, cellZ);

        if (!Cells.TryGetValue(key, out MeshCell? cell))
        {
            cell = new MeshCell(cellX, cellZ);
            Cells.Add(key, cell);
        }

        cell.Mesh.Faces.Add(face);
    }

    public static MeshGrid FromMesh(ModellingMesh mesh, float cellSize)
    {
        MeshGrid grid = new(cellSize);

        foreach (var face in mesh.Faces)
        {
            grid.AddFace(face);
        }

        return grid;
    }

    public MeshCell? GetCellAt(float x, float z)
    {
        int cellX = WorldToCell(x);
        int cellZ = WorldToCell(z);

        Cells.TryGetValue((cellX, cellZ), out MeshCell? cell);

        return cell;
    }

    public List<MeshCell> GetCellsAround(float x, float z, int radius)
    {
        int centerX = WorldToCell(x);
        int centerZ = WorldToCell(z);

        List<MeshCell> result = [];

        for (int zOffset = -radius; zOffset <= radius; zOffset++)
        {
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                var key = (centerX + xOffset, centerZ + zOffset);

                if (Cells.TryGetValue(key, out MeshCell? cell))
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

    private int WorldToCell(float value)
    {
        return (int)MathF.Floor(value / CellSize);
    }
}