using UnityEngine;

public class CubeItem : AbstractBoardItem, IMatchable, IFallable
{
    [SerializeField] private CubeColor _color;
    [SerializeField] private Sprite[] _sprites; 

    public override ItemType GetItemType() => ItemType.Cube;

    public void Init(CubeColor color)
    {
        _color = color;
        // Logic to set sprite based on color would go here or in a View component
    }

    public CubeColor GetColor() => _color;

    public bool CanMatch() => true;

    public bool CanFall() => true;

    public void FallTo(int targetY, float duration)
    {
        // Simple tween logic placeholder
        // Using a coroutine or a Tween library like DOTween is recommended
        SetPosition(X, targetY);
        // Visuals would handle the smooth movement
    }
}
