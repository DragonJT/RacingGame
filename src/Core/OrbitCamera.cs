using System.Numerics;

public sealed class OrbitCamera
{
    public Vector3 Target = Vector3.Zero;

    public float Distance = 5.0f;

    public float Yaw = 0.0f;
    public float Pitch = 0.35f;

    public float RotationSpeed = 0.01f;
    public float ZoomSpeed = 0.25f;

    public float MinPitch = -1.5f;
    public float MaxPitch =  1.5f;

    public float MinDistance = 0.25f;
    public float MaxDistance = 100.0f;

    public Vector3 Position => GetPosition();

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            Position,
            Target,
            Vector3.UnitY
        );
    }

    public void Rotate(Vector2 mouseDelta)
    {
        Yaw -= mouseDelta.X * RotationSpeed;
        Pitch -= mouseDelta.Y * RotationSpeed;

        Pitch = Math.Clamp(Pitch, MinPitch, MaxPitch);
    }

    public void Zoom(float amount)
    {
        Distance -= amount * ZoomSpeed;
        Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
    }

    private Vector3 GetPosition()
    {
        float cosPitch = MathF.Cos(Pitch);

        Vector3 offset = new Vector3(
            MathF.Sin(Yaw) * cosPitch,
            MathF.Sin(Pitch),
            MathF.Cos(Yaw) * cosPitch
        );

        return Target + offset * Distance;
    }
}