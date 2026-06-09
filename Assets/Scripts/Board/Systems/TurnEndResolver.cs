using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public sealed class TurnEndResolver
{
    private readonly NoMoveScanner _noMoveScanner;
    private readonly ShuffleSystem _shuffleSystem;

    public TurnEndResolver(NoMoveScanner noMoveScanner, ShuffleSystem shuffleSystem)
    {
        _noMoveScanner = noMoveScanner;
        _shuffleSystem = shuffleSystem;
    }

    public TurnEndResolution ResolveAfterBoardStable(bool isGameOver)
    {
        if (isGameOver)
        {
            return TurnEndResolution.SkippedGameOver();
        }

        if (_noMoveScanner == null || _noMoveScanner.HasPlayableMove())
        {
            return TurnEndResolution.PlayableMoveAvailable();
        }

        if (_shuffleSystem == null)
        {
            Debug.LogWarning("[TurnEndResolver] No playable moves and ShuffleSystem is not available.");
            return TurnEndResolution.NoMoveWithoutShuffle();
        }

        bool shuffleSucceeded = _shuffleSystem.TryShuffleNormalCubesUntilPlayable(out int attempts);
        if (!shuffleSucceeded)
        {
            Debug.LogWarning("[TurnEndResolver] Shuffle could not create a playable move.");
        }

        return TurnEndResolution.ShuffleAttempted(shuffleSucceeded, attempts);
    }
}

public readonly struct TurnEndResolution
{
    public TurnEndResolutionStatus Status { get; }
    public bool NoMoveDetected { get; }
    public bool ShuffleTriggered { get; }
    public bool ShuffleSucceeded { get; }
    public int ShuffleAttempts { get; }

    private TurnEndResolution(
        TurnEndResolutionStatus status,
        bool noMoveDetected,
        bool shuffleTriggered,
        bool shuffleSucceeded,
        int shuffleAttempts)
    {
        Status = status;
        NoMoveDetected = noMoveDetected;
        ShuffleTriggered = shuffleTriggered;
        ShuffleSucceeded = shuffleSucceeded;
        ShuffleAttempts = shuffleAttempts;
    }

    public static TurnEndResolution SkippedGameOver()
    {
        return new TurnEndResolution(TurnEndResolutionStatus.SkippedGameOver, false, false, false, 0);
    }

    public static TurnEndResolution PlayableMoveAvailable()
    {
        return new TurnEndResolution(TurnEndResolutionStatus.PlayableMoveAvailable, false, false, false, 0);
    }

    public static TurnEndResolution NoMoveWithoutShuffle()
    {
        return new TurnEndResolution(TurnEndResolutionStatus.NoMoveWithoutShuffle, true, false, false, 0);
    }

    public static TurnEndResolution ShuffleAttempted(bool succeeded, int attempts)
    {
        return new TurnEndResolution(TurnEndResolutionStatus.ShuffleAttempted, true, true, succeeded, attempts);
    }
}

public enum TurnEndResolutionStatus
{
    SkippedGameOver,
    PlayableMoveAvailable,
    NoMoveWithoutShuffle,
    ShuffleAttempted
}
}
