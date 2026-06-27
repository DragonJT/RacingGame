using System.Numerics;

public sealed class FollowCamera
{
    public Vector3 Offset = new(0, 5.0f, -10.0f);
    public Vector3 LookAtOffset = new(0, 1.0f, 0);

    public float MinimumHeightAboveGround = 2f;

    public Matrix4x4 GetViewMatrix(CarController car, Ground ground)
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

        float minimumCameraY = ground.GetHeight(cameraPosition.X, cameraPosition.Z) + MinimumHeightAboveGround;

        if (cameraPosition.Y < minimumCameraY)
        {
            cameraPosition.Y = minimumCameraY;
        }

        Vector3 target = car.Position + LookAtOffset;

        return Matrix4x4.CreateLookAt(
            cameraPosition,
            target,
            Vector3.UnitY
        );
    }
}