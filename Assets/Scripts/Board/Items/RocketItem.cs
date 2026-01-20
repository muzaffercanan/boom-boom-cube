using UnityEngine;

public class RocketItem : AbstractBoardItem, IFallable
{
    public bool IsHorizontal { get; private set; }

    public override ItemType GetItemType() => ItemType.Rocket;

    public void Init(bool isHorizontal)
    {
        IsHorizontal = isHorizontal;
    }

    public bool CanFall() => true;

    public void FallTo(int targetY, float duration)
    {
        SetPosition(X, targetY);
    }
}
