
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

public class Track
{
    TrackCommand[] commands;

    public Track(TrackCommand[] commands)
    {
        this.commands = commands;
    }

    public List<Vector3> BuildTrackCenterline()
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

    public ModellingMesh GenerateWithShoulders(
        float roadWidth,
        float shoulderWidth,
        Color32 roadColor,
        Color32 shoulderColor,
        Terrain terrain)
    {
        ModellingMesh mesh = new();

        List<Vector3> centerPoints = BuildTrackCenterline();

        if (centerPoints.Count < 2)
            return mesh;

        List<ModellingVertex> leftRoadVerts = [];
        List<ModellingVertex> rightRoadVerts = [];
        List<ModellingVertex> leftOuterVerts = [];
        List<ModellingVertex> rightOuterVerts = [];

        float halfRoad = roadWidth * 0.5f;
        float halfOuter = halfRoad + shoulderWidth;

        float roadOffset = 0.18f;
        float shoulderOffset = 0.08f;

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

            Vector3 left = new Vector3(-tangent.Z, 0, tangent.X);

            Vector3 center = centerPoints[i];

            float roadHeight =
                terrain.GetHeight(center.X, center.Z) + roadOffset;

            Vector3 leftRoad = center + left * halfRoad;
            Vector3 rightRoad = center - left * halfRoad;

            Vector3 leftOuter = center + left * halfOuter;
            Vector3 rightOuter = center - left * halfOuter;

            leftRoad.Y = roadHeight;
            rightRoad.Y = roadHeight;

            leftOuter.Y =
                terrain.GetHeight(leftOuter.X, leftOuter.Z) + shoulderOffset;

            rightOuter.Y =
                terrain.GetHeight(rightOuter.X, rightOuter.Z) + shoulderOffset;

            leftRoadVerts.Add(new ModellingVertex(leftRoad));
            rightRoadVerts.Add(new ModellingVertex(rightRoad));
            leftOuterVerts.Add(new ModellingVertex(leftOuter));
            rightOuterVerts.Add(new ModellingVertex(rightOuter));
        }

        for (int i = 0; i < centerPoints.Count - 1; i++)
        {
            // Left shoulder
            mesh.Faces.Add(new ModellingFace([
                leftOuterVerts[i],
                leftOuterVerts[i + 1],
                leftRoadVerts[i + 1],
                leftRoadVerts[i]
            ], shoulderColor));

            // Road
            mesh.Faces.Add(new ModellingFace([
                leftRoadVerts[i],
                leftRoadVerts[i + 1],
                rightRoadVerts[i + 1],
                rightRoadVerts[i]
            ], roadColor));

            // Right shoulder
            mesh.Faces.Add(new ModellingFace([
                rightRoadVerts[i],
                rightRoadVerts[i + 1],
                rightOuterVerts[i + 1],
                rightOuterVerts[i]
            ], shoulderColor));
        }

        return mesh;
    }
}