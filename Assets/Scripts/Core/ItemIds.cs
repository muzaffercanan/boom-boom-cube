public static class ItemIds
{
    public const string Red = "r";
    public const string Green = "g";
    public const string Blue = "b";
    public const string Yellow = "y";
    public const string Box = "bo";
    public const string Stone = "s";
    public const string Vase = "v";
    public const string Random = "rand";
    public const string HorizontalRocket = "hro";
    public const string VerticalRocket = "vro";
    public const string RocketHorizontalPartLeft = "rocket_h_part_left";
    public const string RocketHorizontalPartRight = "rocket_h_part_right";
    public const string RocketVerticalPartBottom = "rocket_v_part_bottom";
    public const string RocketVerticalPartTop = "rocket_v_part_top";
    public const string RocketProjectile = "rocket_projectile";

    public static ItemId ToItemId(string id)
    {
        switch (id)
        {
            case Red: return ItemId.Red;
            case Green: return ItemId.Green;
            case Blue: return ItemId.Blue;
            case Yellow: return ItemId.Yellow;
            case Box: return ItemId.Box;
            case Stone: return ItemId.Stone;
            case Vase: return ItemId.Vase;
            case Random: return ItemId.Random;
            case HorizontalRocket: return ItemId.HorizontalRocket;
            case VerticalRocket: return ItemId.VerticalRocket;
            case RocketHorizontalPartLeft: return ItemId.RocketHorizontalPartLeft;
            case RocketHorizontalPartRight: return ItemId.RocketHorizontalPartRight;
            case RocketVerticalPartBottom: return ItemId.RocketVerticalPartBottom;
            case RocketVerticalPartTop: return ItemId.RocketVerticalPartTop;
            case RocketProjectile: return ItemId.RocketProjectile;
            default: return ItemId.Unknown;
        }
    }

    public static string ToStringId(ItemId itemId)
    {
        switch (itemId)
        {
            case ItemId.Red: return Red;
            case ItemId.Green: return Green;
            case ItemId.Blue: return Blue;
            case ItemId.Yellow: return Yellow;
            case ItemId.Box: return Box;
            case ItemId.Stone: return Stone;
            case ItemId.Vase: return Vase;
            case ItemId.Random: return Random;
            case ItemId.HorizontalRocket: return HorizontalRocket;
            case ItemId.VerticalRocket: return VerticalRocket;
            case ItemId.RocketHorizontalPartLeft: return RocketHorizontalPartLeft;
            case ItemId.RocketHorizontalPartRight: return RocketHorizontalPartRight;
            case ItemId.RocketVerticalPartBottom: return RocketVerticalPartBottom;
            case ItemId.RocketVerticalPartTop: return RocketVerticalPartTop;
            case ItemId.RocketProjectile: return RocketProjectile;
            default: return null;
        }
    }
}
