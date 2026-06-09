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
public class GameStateController
{
    private readonly GoalTracker _goalTracker;
    private readonly System.Action<AudioClip> _playSound;
    private readonly AudioClip _winSfx;
    private readonly AudioClip _loseSfx;
    private readonly IProgressService _progressService;
    private readonly IGameplayEventBus _eventBus;

    public bool IsGameOver { get; private set; }

    public GameStateController(
        GoalTracker goalTracker,
        System.Action<AudioClip> playSound,
        AudioClip winSfx,
        AudioClip loseSfx,
        IProgressService progressService,
        IGameplayEventBus eventBus)
    {
        _goalTracker = goalTracker;
        _playSound = playSound;
        _winSfx = winSfx;
        _loseSfx = loseSfx;
        _progressService = progressService;
        _eventBus = eventBus;
    }

    public void Reset()
    {
        IsGameOver = false;
    }

    public void CheckAndResolve(int remainingMoves, int levelNumber)
    {
        if (IsGameOver) return;

        if (_goalTracker.IsComplete)
        {
            IsGameOver = true;
            _progressService.MarkLevelCompleted(levelNumber);
            _playSound?.Invoke(_winSfx);
            _eventBus.RaiseLevelWon();
        }
        else if (remainingMoves <= 0)
        {
            IsGameOver = true;
            _playSound?.Invoke(_loseSfx);
            _eventBus.RaiseLevelLost();
        }
    }
}
}
