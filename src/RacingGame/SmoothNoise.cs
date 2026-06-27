using System.Numerics;

public sealed class SmoothNoise
{
    private readonly int seed;

    public SmoothNoise(int seed)
    {
        this.seed = seed;
    }

    public float Noise(float x, float z, float frequency, float amplitude)
    {
        return Noise(x * frequency, z * frequency) * amplitude;
    }

    public float Noise(float x, float z)
    {
        int x0 = FastFloor(x);
        int z0 = FastFloor(z);

        int x1 = x0 + 1;
        int z1 = z0 + 1;

        float tx = x - x0;
        float tz = z - z0;

        float sx = SmoothStep(tx);
        float sz = SmoothStep(tz);

        float n00 = RandomValue(x0, z0);
        float n10 = RandomValue(x1, z0);
        float n01 = RandomValue(x0, z1);
        float n11 = RandomValue(x1, z1);

        float ix0 = Lerp(n00, n10, sx);
        float ix1 = Lerp(n01, n11, sx);

        return Lerp(ix0, ix1, sz);
    }

    private float RandomValue(int x, int z)
    {
        int n = x * 374761393 + z * 668265263 + seed * 1442695041;

        n = (n ^ (n >> 13)) * 1274126177;
        n = n ^ (n >> 16);

        uint u = (uint)n;

        return u / (float)uint.MaxValue * 2.0f - 1.0f;
    }

    private static int FastFloor(float value)
    {
        int i = (int)value;

        return value < i ? i - 1 : i;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}