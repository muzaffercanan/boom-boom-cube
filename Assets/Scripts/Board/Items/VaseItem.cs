using UnityEngine;

public class VaseItem : ObstacleItem, IFallable
{
    public bool CanFall() => true;

    public void FallTo(int targetY, float duration)
    {
         SetPosition(X, targetY);
    }

    public override bool TakeDamage(DamageType type)
    {
        return base.TakeDamage(type);
    }

    protected override void UpdateVisuals()
    {
    }
}
