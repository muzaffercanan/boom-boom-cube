using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Items
{
public abstract class ObstacleItem : AbstractBoardItem, IDamageable
{
    [SerializeField] protected int _health;

    public int Health => _health;

    public override ItemType GetItemType() => ItemType.Obstacle;

    public virtual bool TakeDamage(DamageType type)
    {
        _health--;
        if (_health <= 0)
        {
            return true;
        }
        UpdateVisuals();
        return false;
    }

    protected abstract void UpdateVisuals();
}
}
