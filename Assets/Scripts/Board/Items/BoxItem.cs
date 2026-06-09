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
}
