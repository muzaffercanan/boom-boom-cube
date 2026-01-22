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
            Sprite damagedSprite = null;
#if UNITY_EDITOR
            damagedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Obstacles/Vase/vase_02.png");
#endif
            if (damagedSprite != null)
            {
                spriteRenderer.sprite = damagedSprite;
            }
        }
    }
}
