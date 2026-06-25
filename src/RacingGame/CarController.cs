using System.Numerics;

public sealed class CarController
{
    public Vector3 Position { get; private set; } = new(0, 0.15f, 0);
    public float HeadingRadians { get; private set; }
    public float Speed { get; private set; }

    public float Acceleration = 8.0f;
    public float BrakeAcceleration = 14.0f;
    public float Friction = 2.0f;
    public float MaxForwardSpeed = 30.0f;
    public float MaxReverseSpeed = -6.0f;
    public float TurnSpeed = 2.5f;

    public Matrix4x4 ModelMatrix
    {
        get
        {
            return
                Matrix4x4.CreateRotationY(HeadingRadians) *
                Matrix4x4.CreateTranslation(Position);
        }
    }

    public void Update(float deltaTime, float throttle, float brake, float steer)
    {
        throttle = Math.Clamp(throttle, 0f, 1f);
        brake = Math.Clamp(brake, 0f, 1f);
        steer = Math.Clamp(steer, -1f, 1f);

        if (throttle > 0)
        {
            Speed += throttle * Acceleration * deltaTime;
        }

        if (brake > 0)
        {
            Speed -= brake * BrakeAcceleration * deltaTime;
        }

        if (throttle == 0 && brake == 0)
        {
            ApplyFriction(deltaTime);
        }

        Speed = Math.Clamp(Speed, MaxReverseSpeed, MaxForwardSpeed);

        float speedPercent = MathF.Abs(Speed) / MaxForwardSpeed;

        if (MathF.Abs(Speed) > 0.05f)
        {
            HeadingRadians += steer * TurnSpeed * speedPercent * deltaTime;
        }

        Vector3 forward = GetForward();

        Position += forward * Speed * deltaTime;
    }

    private void ApplyFriction(float deltaTime)
    {
        if (Speed > 0)
        {
            Speed -= Friction * deltaTime;

            if (Speed < 0)
                Speed = 0;
        }
        else if (Speed < 0)
        {
            Speed += Friction * deltaTime;

            if (Speed > 0)
                Speed = 0;
        }
    }

    public Vector3 GetForward()
    {
        return new Vector3(
            MathF.Sin(HeadingRadians),
            0,
            MathF.Cos(HeadingRadians)
        );
    }
}