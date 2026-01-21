using UnityEngine;

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


}
