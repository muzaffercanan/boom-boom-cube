using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
public interface IDamageable
{
    bool TakeDamage(DamageType type);
    int Health { get; }
}
}
