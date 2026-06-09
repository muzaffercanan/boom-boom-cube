using UnityEngine;
using UnityEditor;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Editor
{
public class LevelTools : EditorWindow
{
    [MenuItem("DreamGames/Reset All Progress", priority = 1)]
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[LevelTools] All PlayerPrefs deleted. Game reset to Level 1.");
    }

    [MenuItem("DreamGames/Set Level/Level 1", priority = 11)]
    public static void SetLevel1() => SetLevel(1);

    [MenuItem("DreamGames/Set Level/Level 2", priority = 12)]
    public static void SetLevel2() => SetLevel(2);

    [MenuItem("DreamGames/Set Level/Level 3", priority = 13)]
    public static void SetLevel3() => SetLevel(3);

    [MenuItem("DreamGames/Set Level/Level 5", priority = 15)]
    public static void SetLevel5() => SetLevel(5);

    [MenuItem("DreamGames/Set Level/Level 10", priority = 20)]
    public static void SetLevel10() => SetLevel(10);

    private static void SetLevel(int level)
    {
        PlayerPrefs.SetInt(ProgressService.LastPlayedLevelKey, level);
        PlayerPrefs.SetInt(ProgressService.SelectedLevelForGameKey, level);
        PlayerPrefs.Save();
        Debug.Log($"[LevelTools] Progress forced to Level {level}.");
    }
}
}
