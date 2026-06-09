using UnityEngine;
using System;
using System.Collections;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public class TurnProcessor
{
    private readonly MoveCounter _moveCounter;
    private readonly CubeTurnHandler _cubeTurnHandler;
    private readonly RocketTurnHandler _rocketTurnHandler;
    private readonly DamageResolver _damageResolver;
    private readonly Action _onTurnComplete;
    private readonly Func<IEnumerator> _onBoardStableBeforeInput;

    public int RemainingMoves => _moveCounter.RemainingMoves;
    public bool IsProcessingTurn { get; private set; }

    public TurnProcessor(
        MoveCounter moveCounter,
        CubeTurnHandler cubeTurnHandler,
        RocketTurnHandler rocketTurnHandler,
        DamageResolver damageResolver,
        Action onTurnComplete,
        Func<IEnumerator> onBoardStableBeforeInput = null)
    {
        _moveCounter = moveCounter;
        _cubeTurnHandler = cubeTurnHandler;
        _rocketTurnHandler = rocketTurnHandler;
        _damageResolver = damageResolver;
        _onTurnComplete = onTurnComplete;
        _onBoardStableBeforeInput = onBoardStableBeforeInput;
    }

    public void SetRocketSystem(RocketSystem rocketSystem)
    {
        _damageResolver.SetRocketSystem(rocketSystem);
        _rocketTurnHandler.SetRocketSystem(rocketSystem);
    }

    public void SetRemainingMoves(int moves)
    {
        _moveCounter.SetRemainingMoves(moves);
    }

    public void AddMoves(int amount)
    {
        _moveCounter.AddMoves(amount);
    }

    public void ResetTurnState()
    {
        IsProcessingTurn = false;
    }

    public IEnumerator ProcessCubeTurn(int x, int y)
    {
        IsProcessingTurn = true;
        TurnExecutionResult result = new TurnExecutionResult();
        yield return _cubeTurnHandler.ProcessTurn(x, y, result);
        yield return CompleteTurnIfProcessed(result);
    }

    public IEnumerator ProcessRocketTurn(int x, int y, RocketItem rocket)
    {
        IsProcessingTurn = true;
        TurnExecutionResult result = new TurnExecutionResult();
        yield return _rocketTurnHandler.ProcessTurn(x, y, rocket, result);
        yield return CompleteTurnIfProcessed(result);
    }

    public void HandleDamage(Vector2Int pos)
    {
        _damageResolver.HandleRocketDamage(pos);
    }

    private IEnumerator CompleteTurnIfProcessed(TurnExecutionResult result)
    {
        if (!result.WasProcessed)
        {
            IsProcessingTurn = false;
            yield break;
        }

        if (_onBoardStableBeforeInput != null)
        {
            yield return _onBoardStableBeforeInput();
        }

        IsProcessingTurn = false;
        _onTurnComplete?.Invoke();
    }
}
}
