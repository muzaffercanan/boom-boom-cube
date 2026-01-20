using UnityEngine;

public class StoneItem : ObstacleItem
{
    public override bool TakeDamage(DamageType type)
    {
        if (type != DamageType.RocketHit) return false;

        return base.TakeDamage(type);
    }

    protected override void UpdateVisuals()
    {
    }
}
