using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
public sealed class SessionLog
{
    private readonly List<TurnLogEntry> _turns = new List<TurnLogEntry>();
    private readonly bool _writeToConsole;

    public bool IsEnabled { get; set; } = true;
    public bool IsSnapshotLoggingEnabled { get; set; }
    public int LevelId { get; private set; }
    public int Seed { get; private set; }
    public IReadOnlyList<TurnLogEntry> Turns => _turns;

    public SessionLog(bool writeToConsole = false)
    {
        _writeToConsole = writeToConsole;
    }

    public void BeginLevel(int levelId, int seed)
    {
        LevelId = levelId;
        Seed = seed;
        _turns.Clear();

        if (IsEnabled && _writeToConsole)
        {
            Debug.Log($"[SessionLog] level_start level={LevelId} seed={Seed}");
        }
    }

    public TurnLogEntry RecordTurn(
        int clickedColumn,
        int clickedRow,
        ItemType clickedItemType,
        int matchSize,
        bool boosterCreated,
        bool boosterUsed,
        int movesLeft,
        IReadOnlyDictionary<string, int> goalsRemaining,
        string resultState,
        string beforeSnapshot = null,
        string afterSnapshot = null)
    {
        if (!IsEnabled)
        {
            return null;
        }

        TurnLogEntry entry = new TurnLogEntry(
            _turns.Count + 1,
            clickedColumn,
            clickedRow,
            clickedItemType,
            matchSize,
            boosterCreated,
            boosterUsed,
            movesLeft,
            FormatGoals(goalsRemaining),
            resultState,
            IsSnapshotLoggingEnabled ? beforeSnapshot : null,
            IsSnapshotLoggingEnabled ? afterSnapshot : null);

        _turns.Add(entry);

        if (_writeToConsole)
        {
            Debug.Log($"[SessionLog] {entry.ToDebugString()}");
        }

        return entry;
    }

    public void MarkLastTurnShuffle(bool triggered, bool succeeded, int attempts)
    {
        if (!IsEnabled || _turns.Count == 0)
        {
            return;
        }

        TurnLogEntry entry = _turns[_turns.Count - 1];
        entry.SetShuffle(triggered, succeeded, attempts);

        if (_writeToConsole && triggered)
        {
            Debug.Log($"[SessionLog] shuffle turn={entry.TurnIndex} success={succeeded} attempts={attempts}");
        }
    }

    public void UpdateLastTurnResult(string resultState, int movesLeft, IReadOnlyDictionary<string, int> goalsRemaining)
    {
        if (!IsEnabled || _turns.Count == 0)
        {
            return;
        }

        _turns[_turns.Count - 1].SetResult(resultState, movesLeft, FormatGoals(goalsRemaining));
    }

    public string ToJsonLikeString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"levelId\":").Append(LevelId)
            .Append(",\"seed\":").Append(Seed)
            .Append(",\"turns\":[");

        for (int i = 0; i < _turns.Count; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(_turns[i].ToJsonLikeString());
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private static string FormatGoals(IReadOnlyDictionary<string, int> goals)
    {
        if (goals == null || goals.Count == 0)
        {
            return "{}";
        }

        List<string> keys = new List<string>(goals.Keys);
        keys.Sort();

        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0) builder.Append(',');
            string key = keys[i];
            builder.Append(key).Append(':').Append(goals[key]);
        }
        builder.Append('}');
        return builder.ToString();
    }

    internal static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

public sealed class TurnLogEntry
{
    public int TurnIndex { get; }
    public int ClickedColumn { get; }
    public int ClickedRow { get; }
    public ItemType ClickedItemType { get; }
    public int MatchSize { get; }
    public bool BoosterCreated { get; }
    public bool BoosterUsed { get; }
    public bool ShuffleTriggered { get; private set; }
    public bool ShuffleSucceeded { get; private set; }
    public int ShuffleAttempts { get; private set; }
    public int MovesLeft { get; private set; }
    public string GoalsRemaining { get; private set; }
    public string ResultState { get; private set; }
    public string BeforeSnapshot { get; private set; }
    public string AfterSnapshot { get; private set; }

    public TurnLogEntry(
        int turnIndex,
        int clickedColumn,
        int clickedRow,
        ItemType clickedItemType,
        int matchSize,
        bool boosterCreated,
        bool boosterUsed,
        int movesLeft,
        string goalsRemaining,
        string resultState,
        string beforeSnapshot = null,
        string afterSnapshot = null)
    {
        TurnIndex = turnIndex;
        ClickedColumn = clickedColumn;
        ClickedRow = clickedRow;
        ClickedItemType = clickedItemType;
        MatchSize = matchSize;
        BoosterCreated = boosterCreated;
        BoosterUsed = boosterUsed;
        MovesLeft = movesLeft;
        GoalsRemaining = goalsRemaining;
        ResultState = resultState;
        BeforeSnapshot = beforeSnapshot;
        AfterSnapshot = afterSnapshot;
    }

    public void SetShuffle(bool triggered, bool succeeded, int attempts)
    {
        ShuffleTriggered = triggered;
        ShuffleSucceeded = succeeded;
        ShuffleAttempts = attempts;
    }

    public void SetResult(string resultState, int movesLeft, string goalsRemaining)
    {
        ResultState = resultState;
        MovesLeft = movesLeft;
        GoalsRemaining = goalsRemaining;
    }

    public string ToDebugString()
    {
        return $"turn={TurnIndex} click=({ClickedColumn},{ClickedRow}) item={ClickedItemType} match={MatchSize} " +
            $"boosterCreated={BoosterCreated} boosterUsed={BoosterUsed} shuffle={ShuffleTriggered}/{ShuffleSucceeded} " +
            $"attempts={ShuffleAttempts} movesLeft={MovesLeft} goals={GoalsRemaining} result={ResultState}";
    }

    public string ToJsonLikeString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"turnIndex\":").Append(TurnIndex)
            .Append(",\"clickedColumn\":").Append(ClickedColumn)
            .Append(",\"clickedRow\":").Append(ClickedRow)
            .Append(",\"clickedItemType\":\"").Append(ClickedItemType).Append('"')
            .Append(",\"matchSize\":").Append(MatchSize)
            .Append(",\"boosterCreated\":").Append(BoosterCreated ? "true" : "false")
            .Append(",\"boosterUsed\":").Append(BoosterUsed ? "true" : "false")
            .Append(",\"shuffleTriggered\":").Append(ShuffleTriggered ? "true" : "false")
            .Append(",\"shuffleSucceeded\":").Append(ShuffleSucceeded ? "true" : "false")
            .Append(",\"shuffleAttempts\":").Append(ShuffleAttempts)
            .Append(",\"movesLeft\":").Append(MovesLeft)
            .Append(",\"goalsRemaining\":\"").Append(SessionLog.Escape(GoalsRemaining)).Append('"')
            .Append(",\"resultState\":\"").Append(SessionLog.Escape(ResultState)).Append('"');

        if (BeforeSnapshot != null)
        {
            builder.Append(",\"beforeSnapshot\":\"").Append(SessionLog.Escape(BeforeSnapshot)).Append('"');
        }

        if (AfterSnapshot != null)
        {
            builder.Append(",\"afterSnapshot\":\"").Append(SessionLog.Escape(AfterSnapshot)).Append('"');
        }

        builder.Append('}');
        return builder.ToString();
    }
}
}
