using System;

public sealed class GameRng
{
    private Random _random;

    public static GameRng Shared { get; private set; } = new GameRng();

    public GameRng()
    {
        _random = new Random();
    }

    public GameRng(int seed)
    {
        _random = new Random(seed);
    }

    public static void SetSharedSeed(int seed)
    {
        Shared = new GameRng(seed);
    }

    public static void ResetShared()
    {
        Shared = new GameRng();
    }

    public void SetSeed(int seed)
    {
        _random = new Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        return _random.Next(minInclusive, maxExclusive);
    }

    public float Value()
    {
        return (float)_random.NextDouble();
    }
}
