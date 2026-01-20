using UnityEngine;

public class VaseItem : ObstacleItem, IFallable
{
    // Vases can fall
    public bool CanFall() => true;

    public void FallTo(int targetY, float duration)
    {
         SetPosition(X, targetY);
         // Visual tweening
    }

    public override bool TakeDamage(DamageType type)
    {
        // Vases have 2 health usually
        return base.TakeDamage(type);
    }

    protected override void UpdateVisuals()
    {
        // Change sprite to cracked vase
    }
}
