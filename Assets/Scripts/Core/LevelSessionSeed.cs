using System;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
public static class LevelSessionSeed
{
    public static int BeginSession(int levelNumber, bool useOverride, int overrideSeed)
    {
        int seed = ResolveSeed(levelNumber, useOverride, overrideSeed);
        GameRng.SetSharedSeed(seed);
        return seed;
    }

    public static int ResolveSeed(int levelNumber, bool useOverride, int overrideSeed)
    {
        if (useOverride)
        {
            return overrideSeed;
        }

        return CreateRuntimeSeed(levelNumber);
    }

    public static int CreateDeterministicSeed(int levelNumber)
    {
        unchecked
        {
            return 1000003 * Math.Max(1, levelNumber) + 9176;
        }
    }

    private static int CreateRuntimeSeed(int levelNumber)
    {
        unchecked
        {
            int timePart = Environment.TickCount;
            int levelPart = CreateDeterministicSeed(levelNumber);
            return timePart ^ levelPart;
        }
    }
}
}
