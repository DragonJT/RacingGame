using System.Numerics;

public static class Tree
{
    public static ModellingMesh Create(
        Vector3 position,
        float height,
        float trunkWidth,
        float canopyWidth,
        Color32 trunkColor,
        Color32 leafColor)
    {
        ModellingMesh mesh = new();

        float trunkHeight = height * 0.45f;
        float canopyHeight = height * 0.55f;

        Vector3 trunkCenter = position + new Vector3(
            0,
            trunkHeight * 0.5f,
            0
        );

        mesh.AddBox(
            trunkCenter,
            new Vector3(trunkWidth, trunkHeight, trunkWidth),
            trunkColor
        );

        Vector3 canopyCenter = position + new Vector3(
            0,
            trunkHeight + canopyHeight * 0.5f,
            0
        );

        mesh.AddBox(
            canopyCenter,
            new Vector3(canopyWidth, canopyHeight, canopyWidth),
            leafColor
        );

        return mesh;
    }
}