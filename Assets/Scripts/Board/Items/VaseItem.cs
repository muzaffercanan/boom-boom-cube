using UnityEngine;

public class VaseItem : ObstacleItem, IFallable
{
    public bool CanFall() => true;

    public override bool TakeDamage(DamageType type)
    {
        return base.TakeDamage(type);
    }

    protected override void UpdateVisuals()
    {
        // Update sprite when vase is damaged
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && _health == 1)
        {
            // Try to load the damaged vase sprite
            var damagedSprite = UnityEngine.Resources.Load<Sprite>("Obstacles/Vase/vase_02");
            if (damagedSprite != null)
            {
                spriteRenderer.sprite = damagedSprite;
            }
        }
    }
}
