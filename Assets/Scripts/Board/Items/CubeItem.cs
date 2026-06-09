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
public class CubeItem : AbstractBoardItem, IMatchable, IFallable
{
    [SerializeField] private CubeColor _color;
    [SerializeField] private Sprite[] _sprites; 

    public override ItemType GetItemType() => ItemType.Cube;

    public void Init(CubeColor color)
    {
        _color = color;
    }

    public CubeColor GetColor() => _color;

    public bool CanMatch() => true;

    public bool CanFall() => true;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _destroyParticle;

    public override void PlayDestroyEffect(DamageType damageType)
    {
        SpawnParticle(_destroyParticle, GetActualColor());
    }

    private Color GetActualColor()
    {
        switch (_color)
        {
            case CubeColor.Red: return new Color(1f, 0.2f, 0.2f);
            case CubeColor.Green: return new Color(0.2f, 0.9f, 0.3f);
            case CubeColor.Blue: return new Color(0.3f, 0.5f, 1f);
            case CubeColor.Yellow: return new Color(1f, 0.9f, 0.2f);
            default: return Color.white;
        }
    }
}
}
