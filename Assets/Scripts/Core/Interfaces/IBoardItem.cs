using UnityEngine;
using System;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
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
}
