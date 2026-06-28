using System.Numerics;

public sealed class FollowCamera
{
    public Vector3 Offset = new(0, 3.5f, -4.0f);
    public Vector3 LookAtOffset = new(0, 3f, 1.0f);

    public Matrix4x4 GetViewMatrix(CarController car, Collisions collisions)
    {
        Vector3 carForward = car.GetForward();

        Vector3 cameraPosition = car.Position + carForward * Offset.Z;
        cameraPosition.Y = collisions.GetHeight(cameraPosition.X, cameraPosition.Z) + Offset.Y;

        Vector3 target = car.Position + carForward * LookAtOffset.Z;
        target.Y = collisions.GetHeight(target.X, target.Z) + LookAtOffset.Y;

        return Matrix4x4.CreateLookAt(
            cameraPosition,
            target,
            Vector3.UnitY
        );
    }
}