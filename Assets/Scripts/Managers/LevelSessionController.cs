using System;
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
public sealed class LevelSessionController
{
    private readonly TextAsset _fallbackLevelJson;
    private readonly bool _useSessionSeedOverride;
    private readonly int _sessionSeedOverride;
    private readonly bool _enableSessionLog;
    private readonly bool _writeSessionLogToConsole;
    private readonly bool _enableTurnSnapshots;
    private readonly SessionLog _sessionLog;
    private readonly IProgressService _progressService;

    public LevelData CurrentLevel { get; private set; }
    public int CurrentSessionSeed { get; private set; }

    public LevelSessionController(
        TextAsset fallbackLevelJson,
        bool useSessionSeedOverride,
        int sessionSeedOverride,
        bool enableSessionLog,
        bool writeSessionLogToConsole,
        bool enableTurnSnapshots,
        SessionLog sessionLog,
        IProgressService progressService)
    {
        _fallbackLevelJson = fallbackLevelJson;
        _useSessionSeedOverride = useSessionSeedOverride;
        _sessionSeedOverride = sessionSeedOverride;
        _enableSessionLog = enableSessionLog;
        _writeSessionLogToConsole = writeSessionLogToConsole;
        _enableTurnSnapshots = enableTurnSnapshots;
        _sessionLog = sessionLog;
        _progressService = progressService;
    }

    public IEnumerator LoadSelectedLevelRoutine(Action<LevelData> onLoaded)
    {
        int levelIndex = _progressService.GetSelectedLevel();
        LevelLoadResult result = null;
        yield return LevelRepository.LoadLevelAsync(levelIndex, loadResult => result = loadResult, _fallbackLevelJson);

        if (result == null || !result.Success)
        {
            Debug.LogError($"[LevelSessionController] {result?.Error ?? "Level load failed without a result."}");
            yield break;
        }

        if (TryBeginLevel(result.LevelData))
        {
            onLoaded?.Invoke(CurrentLevel);
        }
    }

    public bool TryBeginLevelFromJson(string json)
    {
        LevelLoadResult result = LevelRepository.ParseAndValidate(json, "Inline JSON");
        if (!result.Success)
        {
            Debug.LogError($"[LevelSessionController] {result.Error}");
            return false;
        }

        return TryBeginLevel(result.LevelData);
    }

    public bool TryBeginLevel(LevelData levelData)
    {
        string error = LevelRepository.Validate(levelData);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[LevelSessionController] Invalid level: {error}");
            return false;
        }

        CurrentLevel = levelData;
        CurrentSessionSeed = LevelSessionSeed.BeginSession(
            CurrentLevel.level_number,
            _useSessionSeedOverride,
            _sessionSeedOverride);

        if (_sessionLog != null)
        {
            _sessionLog.IsEnabled = _enableSessionLog;
            _sessionLog.IsSnapshotLoggingEnabled = _enableTurnSnapshots;
            _sessionLog.BeginLevel(CurrentLevel.level_number, CurrentSessionSeed);
        }

        if (_writeSessionLogToConsole)
        {
            Debug.Log($"[LevelSessionController] Level {CurrentLevel.level_number} session seed: {CurrentSessionSeed}");
        }

        return true;
    }
}
}
