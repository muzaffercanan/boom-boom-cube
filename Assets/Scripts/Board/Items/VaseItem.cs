using UnityEngine;

public class VaseItem : ObstacleItem, IFallable
{
    public bool CanFall() => true;

    [Header("Visuals")]
    [SerializeField] private Sprite _healthySprite;
    [SerializeField] private Sprite _damagedSprite;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _destroyParticle01; // Phase 2
    [SerializeField] private ParticleSystem _destroyParticle02;
    [SerializeField] private ParticleSystem _destroyParticle03;

    public override bool TakeDamage(DamageType type)
    {
        return base.TakeDamage(type);
    }

    public override void PlayDestroyEffect(DamageType damageType)
    {
        // Phase 2 Destroy Effect
        SpawnParticle(_destroyParticle01);
        SpawnParticle(_destroyParticle02);
        SpawnParticle(_destroyParticle03);
    }

    protected override void UpdateVisuals()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = (_health == 1) ? _damagedSprite : _healthySprite;
        }
    }
}
