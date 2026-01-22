using UnityEngine;

public class StoneItem : ObstacleItem
{
    public override bool TakeDamage(DamageType type)
    {
        if (type != DamageType.RocketHit) return false;

        return base.TakeDamage(type);
    }

    [Header("Particles")]
    [SerializeField] private ParticleSystem _particle01;
    [SerializeField] private ParticleSystem _particle02;
    [SerializeField] private ParticleSystem _particle03;

    public override void PlayDestroyEffect(DamageType damageType)
    {
        SpawnParticle(_particle01);
        SpawnParticle(_particle02);
        SpawnParticle(_particle03);
    }

    protected override void UpdateVisuals()
    {
    }
}
