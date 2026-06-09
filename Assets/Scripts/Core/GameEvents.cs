using System;
using System.Collections.Generic;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
public static class GameEvents
{
    public static event Action<LevelData, Dictionary<string, int>> OnLevelLoaded;
    public static event Action<int> OnMovesUpdated;
    public static event Action<Dictionary<string, int>> OnGoalsUpdated;
    public static event Action OnLevelWon;
    public static event Action OnLevelLost;

    public static void RaiseLevelLoaded(LevelData data, Dictionary<string, int> goals) =>
        OnLevelLoaded?.Invoke(data, goals);

    public static void RaiseMovesUpdated(int moves) =>
        OnMovesUpdated?.Invoke(moves);

    public static void RaiseGoalsUpdated(Dictionary<string, int> goals) =>
        OnGoalsUpdated?.Invoke(goals);

    public static void RaiseLevelWon() =>
        OnLevelWon?.Invoke();

    public static void RaiseLevelLost() =>
        OnLevelLost?.Invoke();
}
}
