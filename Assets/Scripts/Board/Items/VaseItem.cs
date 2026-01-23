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

    private int _lastBlastId = -1;

    private void Start()
    {
        _health = 2; // Enforce correct health for Vase
        UpdateVisuals();
    }

    public override bool TakeDamage(DamageType type)
    {
        // For match blasts, ensure we only take damage once per blast ID
        if (type == DamageType.MatchBlast)
        {
            int currentId = BlastIdTracker.CurrentBlastId;
            if (_lastBlastId == currentId) return false;
            _lastBlastId = currentId;
        }

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
