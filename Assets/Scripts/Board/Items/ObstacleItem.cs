using UnityEngine;

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
