
using System.Drawing;
using System.Numerics;

public enum TrackCommandType
{
    Forward,
    Left,
    Right
}

public readonly struct TrackCommand
{
    public readonly TrackCommandType Type;
    public readonly float AngleDegrees;
    public readonly float Distance;

    private TrackCommand(TrackCommandType type, float angleDegrees, float distance)
    {
        Type = type;
        AngleDegrees = angleDegrees;
        Distance = distance;
    }

    public static TrackCommand Forward(float distance)
    {
        return new TrackCommand(TrackCommandType.Forward, 0, distance);
    }

    public static TrackCommand Left(float angleDegrees, float distance)
    {
        return new TrackCommand(TrackCommandType.Left, angleDegrees, distance);
    }

    public static TrackCommand Right(float angleDegrees, float distance)
    {
        return new TrackCommand(TrackCommandType.Right, angleDegrees, distance);
    }
}

public static class Track
{
    private static List<Vector3> BuildTrackCenterline(TrackCommand[] commands)
    {
        List<Vector3> points = [];

        Vector3 position = Vector3.Zero;

        // Heading 0 means forward along +Z.
        float headingRadians = 0f;

        points.Add(position);

        foreach (var command in commands)
        {
            switch (command.Type)
            {
                case TrackCommandType.Forward:
                {
                    Vector3 dir = DirectionFromHeading(headingRadians);
                    position += dir * command.Distance;
                    points.Add(position);
                    break;
                }

                case TrackCommandType.Left:
                {
                    AddTurn(points, ref position, ref headingRadians, command.AngleDegrees, command.Distance);
                    break;
                }

                case TrackCommandType.Right:
                {
                    AddTurn(points, ref position, ref headingRadians, -command.AngleDegrees, command.Distance);
                    break;
                }
            }
        }

        return points;
    }

    private static void AddTurn(
        List<Vector3> points,
        ref Vector3 position,
        ref float headingRadians,
        float angleDegrees,
        float distance)
    {
        int steps = Math.Max(2, (int)(distance / 1.0f));

        float totalAngleRadians = MathF.PI / 180f * angleDegrees;
        float stepAngle = totalAngleRadians / steps;
        float stepDistance = distance / steps;

        for (int i = 0; i < steps; i++)
        {
            headingRadians += stepAngle;

            Vector3 dir = DirectionFromHeading(headingRadians);
            position += dir * stepDistance;

            points.Add(position);
        }
    }

    private static Vector3 DirectionFromHeading(float headingRadians)
    {
        return new Vector3(
            MathF.Sin(headingRadians),
            0,
            MathF.Cos(headingRadians)
        );
    }

    public static ModellingMesh Create(float roadWidth, Color32 color, params TrackCommand[] commands)
    {
        ModellingMesh m = new();

        List<Vector3> centerPoints = BuildTrackCenterline(commands);

        if (centerPoints.Count < 2)
            return m;

        List<ModellingVertex> leftVerts = [];
        List<ModellingVertex> rightVerts = [];

        float halfWidth = roadWidth * 0.5f;

        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 tangent;

            if (i == 0)
            {
                tangent = centerPoints[1] - centerPoints[0];
            }
            else if (i == centerPoints.Count - 1)
            {
                tangent = centerPoints[i] - centerPoints[i - 1];
            }
            else
            {
                Vector3 prev = Vector3.Normalize(centerPoints[i] - centerPoints[i - 1]);
                Vector3 next = Vector3.Normalize(centerPoints[i + 1] - centerPoints[i]);

                tangent = prev + next;

                if (tangent.LengthSquared() < 0.0001f)
                    tangent = next;
            }

            tangent = Vector3.Normalize(tangent);

            // For a flat XZ track, Y is up.
            Vector3 left = new Vector3(-tangent.Z, 0, tangent.X);

            leftVerts.Add(new ModellingVertex(centerPoints[i] + left * halfWidth));
            rightVerts.Add(new ModellingVertex(centerPoints[i] - left * halfWidth));
        }

        for (int i = 0; i < centerPoints.Count - 1; i++)
        {
            // Winding order gives upward-facing normals.
            var face = new ModellingFace([
                leftVerts[i],
                leftVerts[i + 1],
                rightVerts[i + 1],
                rightVerts[i]
            ], color);

            m.Faces.Add(face);
        }
        return m;
    }
}