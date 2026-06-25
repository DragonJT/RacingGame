using System.Numerics;

public static class Car
{
    public static ModellingMesh Create()
    {
        ModellingMesh mesh = new();

        mesh.AddBox(
            center: new Vector3(0, 0.35f, 0),
            size: new Vector3(1.8f, 0.4f, 3.2f),
            color: Color32.Red
        );

        mesh.AddBox(
            center: new Vector3(0, 0.75f, -0.25f),
            size: new Vector3(1.2f, 0.45f, 1.3f),
            color: Color32.Red
        );

        mesh.AddBox(
            center: new Vector3(-0.95f, 0.18f, 1.05f),
            size: new Vector3(0.25f, 0.35f, 0.45f),
            color: Color32.Black
        );

        mesh.AddBox(
            center: new Vector3(0.95f, 0.18f, 1.05f),
            size: new Vector3(0.25f, 0.35f, 0.45f),
            color: Color32.Black
        );

        mesh.AddBox(
            center: new Vector3(-0.95f, 0.18f, -1.05f),
            size: new Vector3(0.25f, 0.35f, 0.45f),
            color: Color32.Black
        );

        mesh.AddBox(
            center: new Vector3(0.95f, 0.18f, -1.05f),
            size: new Vector3(0.25f, 0.35f, 0.45f),
            color: Color32.Black
        );

        return mesh;
    }
}