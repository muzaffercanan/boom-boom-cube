using UnityEngine;

public static class ProgressService
{
    public const string LastPlayedLevelKey = "LastPlayedLevel";
    public const string SelectedLevelForGameKey = "SelectedLevelForGame";

    public static int GetLastPlayedLevel(int defaultLevel = 1)
    {
        return PlayerPrefs.GetInt(LastPlayedLevelKey, defaultLevel);
    }

    public static int GetSelectedLevel(int defaultLevel = 1)
    {
        return PlayerPrefs.GetInt(SelectedLevelForGameKey, defaultLevel);
    }

    public static void SetSelectedLevel(int level)
    {
        PlayerPrefs.SetInt(SelectedLevelForGameKey, level);
        PlayerPrefs.Save();
    }

    public static void MarkLevelCompleted(int completedLevel)
    {
        int savedLevel = GetLastPlayedLevel();
        if (completedLevel < savedLevel)
        {
            return;
        }

        PlayerPrefs.SetInt(LastPlayedLevelKey, completedLevel + 1);
        PlayerPrefs.Save();
    }
}
