using UnityEngine;

public class BoxItem : ObstacleItem
{
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
