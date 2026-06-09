using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Items
{
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
}
