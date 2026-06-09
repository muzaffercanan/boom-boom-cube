using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public interface IAudioService
{
    float SfxVolume { get; }
    void PlayMusic(AudioClip clip);
    void PlaySfx(AudioClip clip);
}

public sealed class UnityAudioService : IAudioService
{
    public float SfxVolume => AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;

    public void PlayMusic(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlayMusic(clip);
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}

public interface IProgressService
{
    int GetLastPlayedLevel(int defaultLevel = 1);
    int GetSelectedLevel(int defaultLevel = 1);
    void SetSelectedLevel(int level);
    void MarkLevelCompleted(int completedLevel);
}

public sealed class PlayerPrefsProgressService : IProgressService
{
    public int GetLastPlayedLevel(int defaultLevel = 1) => ProgressService.GetLastPlayedLevel(defaultLevel);
    public int GetSelectedLevel(int defaultLevel = 1) => ProgressService.GetSelectedLevel(defaultLevel);
    public void SetSelectedLevel(int level) => ProgressService.SetSelectedLevel(level);
    public void MarkLevelCompleted(int completedLevel) => ProgressService.MarkLevelCompleted(completedLevel);
}

public interface IGameplayEventBus
{
    event Action<LevelData, Dictionary<string, int>> LevelLoaded;
    event Action<int> MovesUpdated;
    event Action<Dictionary<string, int>> GoalsUpdated;
    event Action LevelWon;
    event Action LevelLost;

    void RaiseLevelLoaded(LevelData data, Dictionary<string, int> goals);
    void RaiseMovesUpdated(int moves);
    void RaiseGoalsUpdated(Dictionary<string, int> goals);
    void RaiseLevelWon();
    void RaiseLevelLost();
}

public sealed class StaticGameplayEventBus : IGameplayEventBus
{
    public event Action<LevelData, Dictionary<string, int>> LevelLoaded
    {
        add => GameEvents.OnLevelLoaded += value;
        remove => GameEvents.OnLevelLoaded -= value;
    }

    public event Action<int> MovesUpdated
    {
        add => GameEvents.OnMovesUpdated += value;
        remove => GameEvents.OnMovesUpdated -= value;
    }

    public event Action<Dictionary<string, int>> GoalsUpdated
    {
        add => GameEvents.OnGoalsUpdated += value;
        remove => GameEvents.OnGoalsUpdated -= value;
    }

    public event Action LevelWon
    {
        add => GameEvents.OnLevelWon += value;
        remove => GameEvents.OnLevelWon -= value;
    }

    public event Action LevelLost
    {
        add => GameEvents.OnLevelLost += value;
        remove => GameEvents.OnLevelLost -= value;
    }

    public void RaiseLevelLoaded(LevelData data, Dictionary<string, int> goals) =>
        GameEvents.RaiseLevelLoaded(data, goals);

    public void RaiseMovesUpdated(int moves) =>
        GameEvents.RaiseMovesUpdated(moves);

    public void RaiseGoalsUpdated(Dictionary<string, int> goals) =>
        GameEvents.RaiseGoalsUpdated(goals);

    public void RaiseLevelWon() =>
        GameEvents.RaiseLevelWon();

    public void RaiseLevelLost() =>
        GameEvents.RaiseLevelLost();
}

public interface ISceneLoadService
{
    string ActiveSceneName { get; }
    void LoadScene(string sceneName);
}

public sealed class UnitySceneLoadService : ISceneLoadService
{
    public string ActiveSceneName => SceneManager.GetActiveScene().name;

    public void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
}
