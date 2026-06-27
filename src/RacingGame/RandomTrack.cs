public static class RandomTrack
{
    public static TrackCommand[] Create(
        int pieces,
        Random random,
        float minStraight = 8f,
        float maxStraight = 25f,
        float minTurnDistance = 8f,
        float maxTurnDistance = 22f,
        float maxTurnAngle = 35f)
    {
        List<TrackCommand> commands = [];

        // Start with a straight so the car has room.
        commands.Add(TrackCommand.Forward(20f));

        float currentTurnBias = 0f;

        for (int i = 0; i < pieces; i++)
        {
            float choice = random.NextSingle();

            if (choice < 0.45f)
            {
                float distance = Lerp(minStraight, maxStraight, random.NextSingle());
                commands.Add(TrackCommand.Forward(distance));
            }
            else
            {
                float angle = Lerp(5f, maxTurnAngle, random.NextSingle());
                float distance = Lerp(minTurnDistance, maxTurnDistance, random.NextSingle());

                // Try not to keep turning in the same direction forever.
                bool turnLeft;

                if (currentTurnBias > 60f)
                    turnLeft = false;
                else if (currentTurnBias < -60f)
                    turnLeft = true;
                else
                    turnLeft = random.Next(2) == 0;

                if (turnLeft)
                {
                    commands.Add(TrackCommand.Left(angle, distance));
                    currentTurnBias += angle;
                }
                else
                {
                    commands.Add(TrackCommand.Right(angle, distance));
                    currentTurnBias -= angle;
                }

                // Usually add a short straight after a corner.
                if (random.NextSingle() < 0.7f)
                {
                    float straight = Lerp(5f, 16f, random.NextSingle());
                    commands.Add(TrackCommand.Forward(straight));
                }
            }

            currentTurnBias *= 0.92f;
        }

        // Finish with a straight.
        commands.Add(TrackCommand.Forward(25f));

        return [.. commands];
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}