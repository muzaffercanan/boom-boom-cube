using UnityEngine;

public class StoneItem : ObstacleItem
{
    public override bool TakeDamage(DamageType type)
    {
        // Stone only takes damage from Rockets
        if (type != DamageType.RocketHit) return false;

        return base.TakeDamage(type);
    }

    protected override void UpdateVisuals()
    {
        // Stone usually has 1 HP, so maybe just particle effect here
    }
}
