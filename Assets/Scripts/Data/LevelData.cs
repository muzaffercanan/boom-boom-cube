using System;
using System.Collections.Generic;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Data
{
[Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public List<string> grid;
}
}
