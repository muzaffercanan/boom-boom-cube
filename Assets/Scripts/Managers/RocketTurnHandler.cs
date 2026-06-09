using System.Collections;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class RocketTurnHandler
{
    private readonly BoardResolver _boardResolver;
    private readonly MoveCounter _moveCounter;
    private readonly TurnLogger _turnLogger;

    private RocketSystem _rocketSystem;

    public RocketTurnHandler(
        BoardResolver boardResolver,
        MoveCounter moveCounter,
        TurnLogger turnLogger)
    {
        _boardResolver = boardResolver;
        _moveCounter = moveCounter;
        _turnLogger = turnLogger;
    }

    public void SetRocketSystem(RocketSystem rocketSystem)
    {
        _rocketSystem = rocketSystem;
    }

    public IEnumerator ProcessTurn(int x, int y, RocketItem rocket, TurnExecutionResult result)
    {
        string beforeSnapshot = _turnLogger.CaptureSnapshot();

        result.MarkProcessed();
        _moveCounter.UseMove();

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
        _turnLogger.RecordTurn(x, y, ItemType.Rocket, 0, false, true, beforeSnapshot);
    }
}
}
