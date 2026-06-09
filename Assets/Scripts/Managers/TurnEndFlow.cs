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
public sealed class TurnEndFlow
{
    private readonly GameStateController _gameStateController;
    private readonly TurnProcessor _turnProcessor;
    private readonly GoalTracker _goalTracker;
    private readonly SessionLog _sessionLog;
    private readonly RocketHintSystem _rocketHintSystem;
    private readonly TurnEndResolver _turnEndResolver;
    private readonly NoMoveScanner _noMoveScanner;
    private readonly ShuffleVisualController _shuffleVisualController;
    private readonly bool _enableNoMoveShuffle;

    public TurnEndFlow(
        GameStateController gameStateController,
        TurnProcessor turnProcessor,
        GoalTracker goalTracker,
        SessionLog sessionLog,
        RocketHintSystem rocketHintSystem,
        TurnEndResolver turnEndResolver,
        NoMoveScanner noMoveScanner,
        ShuffleVisualController shuffleVisualController,
        bool enableNoMoveShuffle)
    {
        _gameStateController = gameStateController;
        _turnProcessor = turnProcessor;
        _goalTracker = goalTracker;
        _sessionLog = sessionLog;
        _rocketHintSystem = rocketHintSystem;
        _turnEndResolver = turnEndResolver;
        _noMoveScanner = noMoveScanner;
        _shuffleVisualController = shuffleVisualController;
        _enableNoMoveShuffle = enableNoMoveShuffle;
    }

    public IEnumerator RunAfterBoardStable(int levelNumber)
    {
        _gameStateController.CheckAndResolve(_turnProcessor.RemainingMoves, levelNumber);
        UpdateLastTurnResult();

        if (_gameStateController.IsGameOver)
        {
            yield break;
        }

        if (!_enableNoMoveShuffle)
        {
            yield break;
        }

        if (_turnEndResolver == null)
        {
            yield break;
        }

        bool needsShuffle = _noMoveScanner != null && !_noMoveScanner.HasPlayableMove();
        if (needsShuffle && _shuffleVisualController != null)
        {
            yield return _shuffleVisualController.PlayTransition(true);
        }

        TurnEndResolution resolution = _turnEndResolver.ResolveAfterBoardStable(false);
        if (resolution.ShuffleTriggered)
        {
            _sessionLog?.MarkLastTurnShuffle(true, resolution.ShuffleSucceeded, resolution.ShuffleAttempts);
            _rocketHintSystem?.UpdateHints();
        }

        if (needsShuffle && _shuffleVisualController != null)
        {
            yield return _shuffleVisualController.PlayTransition(false);
        }
    }

    private void UpdateLastTurnResult()
    {
        string state = "Playing";
        if (_gameStateController.IsGameOver)
        {
            state = _goalTracker.IsComplete ? "Won" : "Lost";
        }

        _sessionLog?.UpdateLastTurnResult(state, _turnProcessor.RemainingMoves, _goalTracker.Counts);
    }
}
}
