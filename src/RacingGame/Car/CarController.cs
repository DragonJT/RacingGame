using System.Numerics;

public sealed class CarController
{
    public Vector3 Position { get; private set; } = new(0, 0.15f, 0);
    public Vector3 Velocity { get; private set; }

    public float HeadingRadians { get; private set; }

    public float Speed => Vector3.Dot(Velocity, GetForward());
    public bool IsDrifting { get; private set; }

    public float Acceleration = 20.0f;
    public float BrakeAcceleration = 25.0f;

    public float MaxForwardSpeed = 100.0f;
    public float MaxReverseSpeed = -10.0f;

    public float TurnSpeed = 3.5f;

    public float Grip = 5f;
    public float DriftGrip = 1f;

    public float Drag = 0.2f;
    public float RollingResistance = 1f;

    public float DriftStartSpeed = 10.0f;
    public float DriftSteerThreshold = 0.75f;
    public float DriftHoldTime = 0.35f;
    public float DriftStopSpeed = 8.0f;
    public float DriftRecoveryGrip = 2.0f;
    private float sharpTurnTimer;

    public float WheelBase = 2.4f;
    public float TrackWidth = 1.4f;
    public float GroundOffset = 0.35f;

    public float VerticalVelocity;
    public float Gravity = 20.0f;

    public bool IsGrounded { get; private set; }

    private float pitchRadians;
    private float rollRadians;

    public Matrix4x4 ModelMatrix
    {
        get
        {
            return
                Matrix4x4.CreateRotationX(pitchRadians) *
                Matrix4x4.CreateRotationZ(rollRadians) *
                Matrix4x4.CreateRotationY(HeadingRadians) *
                Matrix4x4.CreateTranslation(Position);
        }
    }
    public void Update(
        GroundHeight collisions,
        float deltaTime,
        float throttle,
        float brake,
        float steer)
    {
        throttle = Math.Clamp(throttle, 0f, 1f);
        brake = Math.Clamp(brake, 0f, 1f);
        steer = Math.Clamp(steer, -1f, 1f);

        UpdateDriftState(deltaTime, steer);

        Vector3 forward = GetForward();

        if (throttle > 0f)
        {
            Velocity += forward * Acceleration * throttle * deltaTime;
        }

        if (brake > 0f)
        {
            Velocity -= forward * BrakeAcceleration * brake * deltaTime;
        }

        ClampForwardSpeed();

        float speedAmount = Velocity.Length();
        float speedPercent = Math.Clamp(speedAmount / MaxForwardSpeed, 0f, 1f);

        if (speedAmount > 0.1f)
        {
            float forwardSpeed = Vector3.Dot(Velocity, forward);
            float reverseMultiplier = forwardSpeed >= 0f ? 1f : -1f;
            float driftTurnMultiplier = IsDrifting ? 1.45f : 1.0f;

            HeadingRadians +=
                steer *
                TurnSpeed *
                driftTurnMultiplier *
                speedPercent *
                reverseMultiplier *
                deltaTime;
        }

        ApplySideGrip(deltaTime);
        ApplyDrag(deltaTime);

        Position += Velocity * deltaTime;
        UpdateGrounding(collisions);
    }

    private void UpdateDriftState(float deltaTime, float steer)
    {
        float speedAmount = Velocity.Length();

        bool fastEnough = speedAmount >= DriftStartSpeed;
        bool steeringHard = MathF.Abs(steer) >= DriftSteerThreshold;

        if (fastEnough && steeringHard)
        {
            sharpTurnTimer += deltaTime;
        }
        else
        {
            sharpTurnTimer -= deltaTime * 2.0f;
        }

        sharpTurnTimer = Math.Clamp(sharpTurnTimer, 0f, DriftHoldTime);

        if (!IsDrifting)
        {
            if (sharpTurnTimer >= DriftHoldTime)
            {
                IsDrifting = true;
            }
        }
        else
        {
            bool tooSlow = speedAmount < DriftStopSpeed;
            bool stoppedSteering = MathF.Abs(steer) < 0.25f;

            if (tooSlow || stoppedSteering)
            {
                IsDrifting = false;
            }
        }
    }

    private void ClampForwardSpeed()
    {
        Vector3 forward = GetForward();
        float forwardSpeed = Vector3.Dot(Velocity, forward);

        if (forwardSpeed > MaxForwardSpeed)
        {
            Velocity += forward * (MaxForwardSpeed - forwardSpeed);
        }
        else if (forwardSpeed < MaxReverseSpeed)
        {
            Velocity += forward * (MaxReverseSpeed - forwardSpeed);
        }
    }

    private void ApplySideGrip(float deltaTime)
    {
        Vector3 right = GetRight();

        float sideSpeed = Vector3.Dot(Velocity, right);

        float grip = IsDrifting ? DriftGrip : Grip;

        Vector3 sideVelocity = right * sideSpeed;

        Velocity -= sideVelocity * grip * deltaTime;
    }

    private void ApplyDrag(float deltaTime)
    {
        float speed = Velocity.Length();

        if (speed < 0.01f)
        {
            Velocity = Vector3.Zero;
            return;
        }

        Vector3 direction = Vector3.Normalize(Velocity);

        Velocity -= direction * RollingResistance * deltaTime;
        Velocity -= Velocity * Drag * deltaTime;

        if (Vector3.Dot(Velocity, direction) < 0f)
        {
            Velocity = Vector3.Zero;
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

    public Vector3 GetRight()
    {
        Vector3 forward = GetForward();

        return new Vector3(
            forward.Z,
            0,
            -forward.X
        );
    }

    private void UpdateGrounding(GroundHeight collisions)
    {
        Vector3 forward = GetForward();
        Vector3 right = GetRight();

        float halfWheelBase = WheelBase * 0.5f;
        float halfTrackWidth = TrackWidth * 0.5f;

        Vector3 frontLeft =
            Position + forward * halfWheelBase - right * halfTrackWidth;

        Vector3 frontRight =
            Position + forward * halfWheelBase + right * halfTrackWidth;

        Vector3 rearLeft =
            Position - forward * halfWheelBase - right * halfTrackWidth;

        Vector3 rearRight =
            Position - forward * halfWheelBase + right * halfTrackWidth;

        float hFL = collisions.GetHeight(frontLeft.X, frontLeft.Z);
        float hFR = collisions.GetHeight(frontRight.X, frontRight.Z);
        float hRL = collisions.GetHeight(rearLeft.X, rearLeft.Z);
        float hRR = collisions.GetHeight(rearRight.X, rearRight.Z);

        float frontHeight = (hFL + hFR) * 0.5f;
        float rearHeight = (hRL + hRR) * 0.5f;
        float leftHeight = (hFL + hRL) * 0.5f;
        float rightHeight = (hFR + hRR) * 0.5f;

        pitchRadians = MathF.Atan2(rearHeight - frontHeight, WheelBase);
        rollRadians = MathF.Atan2(rightHeight - leftHeight, TrackWidth);

        float averageGroundHeight =
            (hFL + hFR + hRL + hRR) * 0.25f;

        float targetY = averageGroundHeight + GroundOffset;
        Position = new Vector3(Position.X, targetY, Position.Z);
    }
}