using System;
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
[Serializable]
public sealed class BoardSceneReferences
{
    public ItemFactory ItemFactory;
    public Transform BoardParent;
    public BoardSetupController BoardSetup;
    public GameObject LevelButton;
    public float CellSize = 1f;

    public void ApplyLegacy(
        ItemFactory itemFactory,
        Transform boardParent,
        float cellSize,
        BoardSetupController boardSetup,
        GameObject levelButton)
    {
        if (ItemFactory == null) ItemFactory = itemFactory;
        if (BoardParent == null) BoardParent = boardParent;
        if (BoardSetup == null) BoardSetup = boardSetup;
        if (LevelButton == null) LevelButton = levelButton;
        if (cellSize > 0f && Mathf.Approximately(CellSize, 1f))
        {
            CellSize = cellSize;
        }
    }
}

[Serializable]
public sealed class LevelLoadConfig
{
    public TextAsset LevelJson;
    public bool EnableLevelLoaderDebugLogs;

    public void ApplyLegacy(TextAsset levelJson, bool enableLevelLoaderDebugLogs)
    {
        if (LevelJson == null) LevelJson = levelJson;
        EnableLevelLoaderDebugLogs = enableLevelLoaderDebugLogs;
    }
}

[Serializable]
public sealed class GameplayRulesConfig
{
    public int MinMatchSize = 2;
    public int RocketMatchSize = 4;
    public bool EnableNoMoveShuffle = true;
    public int ShuffleMaxAttempts = 20;
    public float ShuffleVisualDuration = 0.25f;
    public BoardAnimationConfig BoardAnimation = new BoardAnimationConfig();

    public void ApplyLegacy(
        int minMatchSize,
        int rocketMatchSize,
        bool enableNoMoveShuffle,
        int shuffleMaxAttempts,
        float shuffleVisualDuration)
    {
        MinMatchSize = minMatchSize;
        RocketMatchSize = rocketMatchSize;
        EnableNoMoveShuffle = enableNoMoveShuffle;
        ShuffleMaxAttempts = shuffleMaxAttempts;
        ShuffleVisualDuration = shuffleVisualDuration;
    }
}

[Serializable]
public sealed class BoardAnimationConfig
{
    // Base fall duration for a 1-cell drop; actual duration scales as FallMoveDuration * sqrt(cells).
    [Range(0.05f, 0.4f)] public float FallMoveDuration = 0.14f;
    // Stagger delay between items in the same column (bottom item = 0, each above adds this).
    [Range(0f, 0.12f)] public float GravityStepDelay = 0.05f;
    [Range(0f, 0.25f)] public float PostFillGravityStepDelay = 0.05f;
    [Range(0f, 0.3f)] public float LandingBounceHeight = 0.08f;
    [Range(0f, 0.25f)] public float LandingBounceDuration = 0.10f;
    [Range(0f, 4f)] public float SpawnRowsAboveBoard = 1f;
    [Range(0.5f, 1.2f)] public float SpawnStartScale = 0.9f;

    public static BoardAnimationConfig Default => new BoardAnimationConfig();

    public float GetFallDuration(float cellDistance)
    {
        return FallMoveDuration * Mathf.Sqrt(Mathf.Max(1f, cellDistance));
    }

    public float GetCascadeDelay(int cascadeIndex)
    {
        return Mathf.Max(0, cascadeIndex) * GravityStepDelay;
    }

    public float GetFallTotalTime(float cellDistance, int cascadeIndex)
    {
        return GetCascadeDelay(cascadeIndex) + GetFallDuration(cellDistance) + LandingBounceDuration;
    }
}

[Serializable]
public sealed class SessionConfig
{
    public bool UseSessionSeedOverride;
    public int SessionSeedOverride;
    public bool EnableSessionLog = true;
    public bool WriteSessionLogToConsole;
    public bool EnableTurnSnapshots;

    public void ApplyLegacy(
        bool useSessionSeedOverride,
        int sessionSeedOverride,
        bool enableSessionLog,
        bool writeSessionLogToConsole,
        bool enableTurnSnapshots)
    {
        UseSessionSeedOverride = useSessionSeedOverride;
        SessionSeedOverride = sessionSeedOverride;
        EnableSessionLog = enableSessionLog;
        WriteSessionLogToConsole = writeSessionLogToConsole;
        EnableTurnSnapshots = enableTurnSnapshots;
    }
}

[Serializable]
public sealed class GameAudioConfig
{
    public AudioClip BackgroundMusic;
    public AudioClip MatchSfx;
    public AudioClip TapSfx;
    public AudioClip RocketSfx;
    public AudioClip WinSfx;
    public AudioClip LoseSfx;

    public void ApplyLegacy(
        AudioClip backgroundMusic,
        AudioClip matchSfx,
        AudioClip tapSfx,
        AudioClip rocketSfx,
        AudioClip winSfx,
        AudioClip loseSfx)
    {
        if (BackgroundMusic == null) BackgroundMusic = backgroundMusic;
        if (MatchSfx == null) MatchSfx = matchSfx;
        if (TapSfx == null) TapSfx = tapSfx;
        if (RocketSfx == null) RocketSfx = rocketSfx;
        if (WinSfx == null) WinSfx = winSfx;
        if (LoseSfx == null) LoseSfx = loseSfx;
    }
}
}
