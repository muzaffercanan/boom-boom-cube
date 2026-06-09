using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class TurnExecutionResult
{
    public bool WasProcessed { get; private set; }

    public void MarkProcessed()
    {
        WasProcessed = true;
    }
}
}
