using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TurnProcessor
{
    private readonly GridSystem _gridSystem;
    private readonly MatchSystem _matchSystem;
    private readonly BoardResolver _boardResolver;
    private readonly GoalTracker _goalTracker;
    private readonly BoardFiller _boardFiller;
    private readonly int _minMatchSize;
    private readonly int _rocketMatchSize;
    private readonly float _cellSize;
    private readonly Action<int> _onMovesChanged;
    private readonly Action<Dictionary<string, int>> _onGoalsChanged;
    private readonly Action _onTurnComplete;
    private readonly Action<AudioClip> _playSound;
    private readonly AudioClip _matchSfx;
    private readonly Func<IEnumerator> _onBoardStableBeforeInput;
    private readonly SessionLog _sessionLog;

    private RocketSystem _rocketSystem;

    public int RemainingMoves { get; private set; }
    public bool IsProcessingTurn { get; private set; }

    public TurnProcessor(
        GridSystem gridSystem,
        MatchSystem matchSystem,
        BoardResolver boardResolver,
        GoalTracker goalTracker,
        BoardFiller boardFiller,
        int minMatchSize,
        int rocketMatchSize,
        float cellSize,
        Action<int> onMovesChanged,
        Action<Dictionary<string, int>> onGoalsChanged,
        Action onTurnComplete,
        Action<AudioClip> playSound,
        AudioClip matchSfx,
        Func<IEnumerator> onBoardStableBeforeInput = null,
        SessionLog sessionLog = null)
    {
        _gridSystem = gridSystem;
        _matchSystem = matchSystem;
        _boardResolver = boardResolver;
        _goalTracker = goalTracker;
        _boardFiller = boardFiller;
        _minMatchSize = minMatchSize;
        _rocketMatchSize = rocketMatchSize;
        _cellSize = cellSize;
        _onMovesChanged = onMovesChanged;
        _onGoalsChanged = onGoalsChanged;
        _onTurnComplete = onTurnComplete;
        _playSound = playSound;
        _matchSfx = matchSfx;
        _onBoardStableBeforeInput = onBoardStableBeforeInput;
        _sessionLog = sessionLog;
    }

    public void SetRocketSystem(RocketSystem rocketSystem)
    {
        _rocketSystem = rocketSystem;
    }

    public void SetRemainingMoves(int moves)
    {
        RemainingMoves = moves;
    }

    public IEnumerator ProcessCubeTurn(int x, int y)
    {
        IsProcessingTurn = true;
        string beforeSnapshot = CaptureSnapshotForLog();

        var matches = _matchSystem.FindMatches(x, y);
        if (matches.Count < _minMatchSize)
        {
            IsProcessingTurn = false;
            yield break;
        }

        UseMove();
        _playSound?.Invoke(_matchSfx);

        bool createdRocket = matches.Count >= _rocketMatchSize;

        DamageAdjacentObstacles(_matchSystem.GetAdjacentObstacles(matches));

        if (createdRocket)
            yield return AnimateCubesToCenter(matches, x, y);
        else
            yield return new WaitForSeconds(0.1f);

        foreach (var item in matches)
            DestroyItemWithEffect(item.X, item.Y, DamageType.MatchBlast);

        if (createdRocket)
            _boardFiller.CreateRocket(x, y);

        yield return _boardResolver.ApplyGravityAndFillSequence();
        RecordTurnLog(x, y, ItemType.Cube, matches.Count, createdRocket, false, beforeSnapshot);
        yield return RunBoardStableHook();

        IsProcessingTurn = false;
        _onTurnComplete?.Invoke();
    }

    public IEnumerator ProcessRocketTurn(int x, int y, RocketItem rocket)
    {
        IsProcessingTurn = true;
        string beforeSnapshot = CaptureSnapshotForLog();

        UseMove();
        if (_rocketSystem != null)
        {
            _rocketSystem.TryProcessRocketClick(x, y, rocket, out _);
            yield return _rocketSystem.WaitForProjectilesToComplete();
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
        }

        yield return _boardResolver.ApplyGravityAndFillSequence();
        RecordTurnLog(x, y, ItemType.Rocket, 0, false, true, beforeSnapshot);
        yield return RunBoardStableHook();

        IsProcessingTurn = false;
        _onTurnComplete?.Invoke();
    }

    public void HandleDamage(Vector2Int pos)
    {
        var item = _gridSystem.GetItem(pos.x, pos.y);
        if (item == null) return;

        if (item is RocketItem rocket)
        {
            _rocketSystem.TriggerRocket(pos.x, pos.y, rocket);
            return;
        }

        if (item is IDamageable damageable)
        {
            bool destroyed = damageable.TakeDamage(DamageType.RocketHit);
            if (destroyed)
                DestroyItemWithEffect(pos.x, pos.y, DamageType.RocketHit);
        }
        else
        {
            DestroyItemWithEffect(pos.x, pos.y, DamageType.RocketHit);
        }
    }

    private void UseMove()
    {
        RemainingMoves--;
        _onMovesChanged?.Invoke(RemainingMoves);
    }

    private void DamageAdjacentObstacles(IEnumerable<IBoardItem> obstacles)
    {
        var damaged = new HashSet<IBoardItem>();
        foreach (var obstacle in obstacles)
        {
            if (damaged.Contains(obstacle)) continue;
            if (obstacle is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.MatchBlast);
                damaged.Add(obstacle);
                if (destroyed)
                    DestroyItemWithEffect(obstacle.X, obstacle.Y, DamageType.MatchBlast);
            }
        }
    }

    private void DestroyItemWithEffect(int x, int y, DamageType type)
    {
        var item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        bool goalChanged = _goalTracker.TryRecordDestroyed(item);
        item.PlayDestroyEffect(type);
        _gridSystem.DestroyItem(x, y);

        if (goalChanged)
            _onGoalsChanged?.Invoke(_goalTracker.Counts);
    }

    private IEnumerator AnimateCubesToCenter(List<IBoardItem> items, int targetX, int targetY)
    {
        Vector3 targetPos = new Vector3(targetX * _cellSize, targetY * _cellSize, 0);
        float duration = 0.2f;

        foreach (var item in items)
        {
            if (item is AbstractBoardItem abi)
                abi.MoveToPosition(targetPos, duration);
        }

        yield return new WaitForSeconds(duration);
    }

    private IEnumerator RunBoardStableHook()
    {
        if (_onBoardStableBeforeInput == null)
        {
            yield break;
        }

        yield return _onBoardStableBeforeInput();
    }

    private void RecordTurnLog(
        int x,
        int y,
        ItemType clickedItemType,
        int matchSize,
        bool boosterCreated,
        bool boosterUsed,
        string beforeSnapshot)
    {
        string afterSnapshot = CaptureSnapshotForLog();

        _sessionLog?.RecordTurn(
            x,
            y,
            clickedItemType,
            matchSize,
            boosterCreated,
            boosterUsed,
            RemainingMoves,
            _goalTracker.Counts,
            "Pending",
            beforeSnapshot,
            afterSnapshot);
    }

    private string CaptureSnapshotForLog()
    {
        if (_sessionLog == null || !_sessionLog.IsEnabled || !_sessionLog.IsSnapshotLoggingEnabled)
        {
            return null;
        }

        return BoardSnapshot.FromGrid(_gridSystem).ToDebugString();
    }
}
