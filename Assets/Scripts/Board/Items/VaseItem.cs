using UnityEngine;

public class VaseItem : ObstacleItem, IFallable
{
    public bool CanFall() => true;

    [Header("Visuals")]
    [SerializeField] private Sprite _healthySprite;
    [SerializeField] private Sprite _damagedSprite;

    [Header("Particles")]

    [SerializeField] private ParticleSystem _destroyParticle01;
    [SerializeField] private ParticleSystem _destroyParticle02;
    [SerializeField] private ParticleSystem _destroyParticle03;

    private void Start()
    {
        _health = 2;
        UpdateVisuals();
    }

    public override void PlayDestroyEffect(DamageType damageType)
    {

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
