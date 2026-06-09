using System;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class MoveCounter
{
    private readonly Action<int> _onMovesChanged;

    public int RemainingMoves { get; private set; }

    public MoveCounter(Action<int> onMovesChanged)
    {
        _onMovesChanged = onMovesChanged;
    }

    public void SetRemainingMoves(int moves)
    {
        RemainingMoves = moves;
    }

    public void UseMove()
    {
        RemainingMoves--;
        _onMovesChanged?.Invoke(RemainingMoves);
    }

    public void AddMoves(int amount)
    {
        RemainingMoves += amount;
        if (RemainingMoves < 0) RemainingMoves = 0;
        _onMovesChanged?.Invoke(RemainingMoves);
    }
}
}
