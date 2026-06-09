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
public class RocketItem : AbstractBoardItem, IFallable
{
    public bool IsHorizontal { get; private set; }

    public override ItemType GetItemType() => ItemType.Rocket;

    public void Init(bool isHorizontal)
    {
        IsHorizontal = isHorizontal;
    }

    public bool CanFall() => true;


}
}
