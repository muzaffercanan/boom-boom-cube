using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class TurnLogger
{
    private readonly SessionLog _sessionLog;
    private readonly GridSystem _gridSystem;
    private readonly GoalTracker _goalTracker;
    private readonly MoveCounter _moveCounter;

    public TurnLogger(
        SessionLog sessionLog,
        GridSystem gridSystem,
        GoalTracker goalTracker,
        MoveCounter moveCounter)
    {
        _sessionLog = sessionLog;
        _gridSystem = gridSystem;
        _goalTracker = goalTracker;
        _moveCounter = moveCounter;
    }

    public string CaptureSnapshot()
    {
        if (_sessionLog == null || !_sessionLog.IsEnabled || !_sessionLog.IsSnapshotLoggingEnabled)
        {
            return null;
        }

        return BoardSnapshot.FromGrid(_gridSystem).ToDebugString();
    }

    public void RecordTurn(
        int x,
        int y,
        ItemType clickedItemType,
        int matchSize,
        bool boosterCreated,
        bool boosterUsed,
        string beforeSnapshot)
    {
        string afterSnapshot = CaptureSnapshot();

        _sessionLog?.RecordTurn(
            x,
            y,
            clickedItemType,
            matchSize,
            boosterCreated,
            boosterUsed,
            _moveCounter.RemainingMoves,
            _goalTracker.Counts,
            "Pending",
            beforeSnapshot,
            afterSnapshot);
    }
}
}
