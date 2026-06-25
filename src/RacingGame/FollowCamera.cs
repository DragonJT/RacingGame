using System.Numerics;

public sealed class FollowCamera
{
    public Vector3 Offset = new(0, 3.0f, -6.0f);
    public Vector3 LookAtOffset = new(0, 1.0f, 0);

    public Matrix4x4 GetViewMatrix(CarController car)
    {
        Vector3 carForward = car.GetForward();

        Vector3 carRight = new Vector3(
            carForward.Z,
            0,
            -carForward.X
        );

        Vector3 cameraPosition =
            car.Position +
            carRight * Offset.X +
            Vector3.UnitY * Offset.Y -
            carForward * MathF.Abs(Offset.Z);

        Vector3 target = car.Position + LookAtOffset;

        return Matrix4x4.CreateLookAt(
            cameraPosition,
            target,
            Vector3.UnitY
        );
    }
}