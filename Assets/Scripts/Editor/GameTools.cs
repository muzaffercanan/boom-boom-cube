using UnityEditor;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Editor
{
public class GameTools : EditorWindow
{
    [MenuItem("DreamGames/Tools/Clear PlayerPrefs")]
    public static void ClearInternalData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameTools] PlayerPrefs Cleared!");
    }

    [MenuItem("DreamGames/Tools/Set Level to 1")]
    public static void SetLevelTo1()
    {
        PlayerPrefs.SetInt(ProgressService.LastPlayedLevelKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[GameTools] Level Set to 1");
    }

    [MenuItem("DreamGames/Tools/Set Level to 10")]
    public static void SetLevelTo10()
    {
        PlayerPrefs.SetInt(ProgressService.LastPlayedLevelKey, 10);
        PlayerPrefs.Save();
        Debug.Log("[GameTools] Level Set to 10");
    }

    [MenuItem("DreamGames/Tools/Set Level to 11 (Finished)")]
    public static void SetLevelTo11()
    {
        PlayerPrefs.SetInt(ProgressService.LastPlayedLevelKey, 11);
        PlayerPrefs.Save();
        Debug.Log("[GameTools] Level Set to 11 (Finished State)");
    }
}
}
