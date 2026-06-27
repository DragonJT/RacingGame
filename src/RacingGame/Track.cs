using System.Numerics;
using Silk.NET.Input;

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

public class TrackPoint
{
    public readonly Vector3 Position;
    public readonly int TurnSide;
    public readonly float TurnRadius;
    public readonly Vector3 Left;

    public TrackPoint(Vector3 position, int turnSide, float turnRadius, float headingRadians)
    {
        Position = position;
        TurnSide = turnSide;
        TurnRadius = turnRadius;
        Vector3 tangent = new Vector3(
            MathF.Sin(headingRadians),
            0,
            MathF.Cos(headingRadians)
        );
        Left = new Vector3(
            -tangent.Z,
            0,
            tangent.X
        );
    }
}

public sealed class Track
{
    private readonly float roadRadius;
    private readonly float outerRadius;
    private readonly TrackCommand[] commands;
    public float PointSpacing = 0.5f;

    public Track(float roadRadius, float outerRadius, TrackCommand[] commands)
    {
        this.roadRadius = roadRadius;
        this.outerRadius = outerRadius;
        this.commands = commands;
    }

    public List<Vector3> BuildTrackCenterline()
    {
        List<TrackPoint> points = BuildTrackPoints();

        List<Vector3> centerline = [];

        foreach (var point in points)
            centerline.Add(point.Position);

        return centerline;
    }

    public List<TrackPoint> BuildTrackPoints()
    {
        List<TrackPoint> points = [];

        Vector3 position = Vector3.Zero;
        float headingRadians = 0f;

        points.Add(new TrackPoint(position, 0, float.MaxValue, headingRadians));

        foreach (var command in commands)
        {
            switch (command.Type)
            {
                case TrackCommandType.Forward:
                {
                    AddStraight(
                        points, 
                        ref position, 
                        ref headingRadians, 
                        command.Distance
                    );
                    break;
                }

                case TrackCommandType.Left:
                {
                    AddTurn(
                        points,
                        ref position,
                        ref headingRadians,
                        command.AngleDegrees,
                        command.Distance
                    );
                    break;
                }

                case TrackCommandType.Right:
                {
                    AddTurn(
                        points,
                        ref position,
                        ref headingRadians,
                        -command.AngleDegrees,
                        command.Distance
                    );
                    break;
                }
            }
        }

        return points;
    }

    private void AddStraight(
        List<TrackPoint> points,
        ref Vector3 position,
        ref float headingRadians,
        float distance)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(distance / PointSpacing));
        float stepDistance = distance / steps;

        for (int i = 0; i < steps; i++)
        {
            Vector3 dir = DirectionFromHeading(headingRadians);
            position += dir * stepDistance;

            points.Add(new TrackPoint(position, 0, float.MaxValue, headingRadians));
        }
    }

    private void AddTurn(
        List<TrackPoint> points,
        ref Vector3 position,
        ref float headingRadians,
        float signedAngleDegrees,
        float distance)
    {
        int steps = Math.Max(6, (int)MathF.Ceiling(distance / PointSpacing));

        float totalAngleRadians = signedAngleDegrees * MathF.PI / 180f;
        float stepAngle = totalAngleRadians / steps;
        float stepDistance = distance / steps;

        int turnSide = signedAngleDegrees >= 0f ? 1 : -1;

        float angleRadiansAbs = MathF.Abs(totalAngleRadians);

        float turnRadius = angleRadiansAbs < 0.0001f
            ? float.MaxValue
            : distance / angleRadiansAbs;

        for (int i = 0; i < steps; i++)
        {
            Vector3 dir = DirectionFromHeading(headingRadians);
            position += dir * stepDistance;
            headingRadians += stepAngle;
            points.Add(new TrackPoint(position, turnSide, turnRadius, headingRadians));
        }
    }

    private Vector3 LeftPoint(TrackPoint p1, TrackPoint p2, Terrain terrain, float radius, bool centerHeight)
    {
        if(p1.TurnSide == -1 && p1.TurnRadius < radius)
        {
            radius = p1.TurnRadius;
        }
        if(p2.TurnSide == -1 && p2.TurnRadius < radius)
        {
            radius = p2.TurnRadius;
        }
        var pos = p1.Position + p1.Left * radius;
        if (centerHeight)
        {
            return new Vector3(pos.X, terrain.GetHeight(p1.Position.X, p1.Position.Z), pos.Z);
        }
        return new Vector3(pos.X, terrain.GetHeight(pos.X, pos.Z), pos.Z);
    }

    private Vector3 RightPoint(TrackPoint p1, TrackPoint p2, Terrain terrain, float radius, bool centerHeight)
    {
        if(p1.TurnSide == 1 && p1.TurnRadius < radius)
        {
            radius = p1.TurnRadius;
        }
        if(p2.TurnSide == 1 && p2.TurnRadius < radius)
        {
            radius = p2.TurnRadius;
        }
        var pos = p1.Position - p1.Left * radius;
        if (centerHeight)
        {
            return new Vector3(pos.X, terrain.GetHeight(p1.Position.X, p1.Position.Z), pos.Z);
        }
        return new Vector3(pos.X, terrain.GetHeight(pos.X, pos.Z), pos.Z);
    }

    static ModellingVertex GetVertex(List<ModellingVertex> vertices, Vector3 position)
    {
        var index = vertices
            .FindIndex(v=>
                (new Vector2(v.Position.X, v.Position.Z) - new Vector2(position.X, position.Z))
                .LengthSquared() < 0.1f);
        if (index >= 0)
        {
            return vertices[index];
        }
        var vertex = new ModellingVertex(position);
        vertices.Add(vertex);
        return vertex;
    }

    public ModellingMesh GenerateWithShoulders(
        Color32 roadColor,
        Color32 shoulderColor,
        Terrain terrain)
    {
        ModellingMesh mesh = new();

        List<TrackPoint> trackPoints = BuildTrackPoints();

        List<ModellingVertex> leftRoadVerts = [];
        List<ModellingVertex> rightRoadVerts = [];
        List<ModellingVertex> leftOuterVerts = [];
        List<ModellingVertex> rightOuterVerts = [];
        List<ModellingVertex> vertices = [];

        if (trackPoints.Count < 3)
            return mesh;

        for (int i = 0; i < trackPoints.Count; i++)
        {
            var p1 = trackPoints[i];
            var p2 = trackPoints[i];

            if(i < trackPoints.Count - 1)
            {
                p2 = trackPoints[i+1];
            }

            leftOuterVerts.Add(GetVertex(vertices, LeftPoint(p1, p2, terrain, outerRadius, false)));
            rightOuterVerts.Add(GetVertex(vertices, RightPoint(p1, p2, terrain, outerRadius, false)));
            leftRoadVerts.Add(GetVertex(vertices, LeftPoint(p1, p2, terrain, roadRadius, true)));
            rightRoadVerts.Add(GetVertex(vertices, RightPoint(p1, p2, terrain, roadRadius, true)));
        }

        for (int i = 0; i < trackPoints.Count - 1; i++)
        {
            // Left shoulder.
            AddQuad(
                mesh,
                leftOuterVerts[i],
                leftOuterVerts[i + 1],
                leftRoadVerts[i + 1],
                leftRoadVerts[i],
                shoulderColor
            );

            // Road.
            AddQuad(
                mesh,
                leftRoadVerts[i],
                leftRoadVerts[i + 1],
                rightRoadVerts[i + 1],
                rightRoadVerts[i],
                roadColor
            );

            // Right shoulder.
            AddQuad(
                mesh,
                rightRoadVerts[i],
                rightRoadVerts[i + 1],
                rightOuterVerts[i + 1],
                rightOuterVerts[i],
                shoulderColor
            );
        }

        return mesh;
    }

    private static void AddQuad(
        ModellingMesh mesh, 
        ModellingVertex a, 
        ModellingVertex b, 
        ModellingVertex c, 
        ModellingVertex d, 
        Color32 color)
    {
        var verts = new ModellingVertex[]{a,b,c,d};
        var finalVerts = verts.Distinct().ToArray();
        if(finalVerts.Length >= 3)
        {
            mesh.Faces.Add(new ModellingFace(finalVerts, color));
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
}
