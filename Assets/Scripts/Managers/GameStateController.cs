using UnityEngine;

public class GameStateController
{
    private readonly GoalTracker _goalTracker;
    private readonly System.Action<AudioClip> _playSound;
    private readonly AudioClip _winSfx;
    private readonly AudioClip _loseSfx;

    public bool IsGameOver { get; private set; }

    public GameStateController(
        GoalTracker goalTracker,
        System.Action<AudioClip> playSound,
        AudioClip winSfx,
        AudioClip loseSfx)
    {
        _goalTracker = goalTracker;
        _playSound = playSound;
        _winSfx = winSfx;
        _loseSfx = loseSfx;
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
            ProgressService.MarkLevelCompleted(levelNumber);
            _playSound?.Invoke(_winSfx);
            GameEvents.RaiseLevelWon();
        }
        else if (remainingMoves <= 0)
        {
            IsGameOver = true;
            _playSound?.Invoke(_loseSfx);
            GameEvents.RaiseLevelLost();
        }
    }
}
