using UnityEngine;
using System;

public interface IBoardItem
{
    int X { get; }
    int Y { get; }
    void SetPosition(int x, int y);
    void Init(Action<int, int> onClickCallback);
    ItemType GetItemType();
    GameObject GetGameObject();
    void PlayDestroyEffect(DamageType damageType);
}
