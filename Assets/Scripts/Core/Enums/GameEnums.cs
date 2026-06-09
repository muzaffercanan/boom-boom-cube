using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Core
{
public enum ItemType
{
    None,
    Cube,
    Rocket,
    Obstacle
}

public enum ItemId
{
    Unknown,
    Red,
    Green,
    Blue,
    Yellow,
    Box,
    Stone,
    Vase,
    Random,
    HorizontalRocket,
    VerticalRocket,
    RocketHorizontalPartLeft,
    RocketHorizontalPartRight,
    RocketVerticalPartBottom,
    RocketVerticalPartTop,
    RocketProjectile
}

public enum CubeColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Random
}

public enum DamageType
{
    MatchBlast,
    RocketHit
}
}
