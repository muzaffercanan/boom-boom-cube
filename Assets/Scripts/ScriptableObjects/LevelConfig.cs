using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "DreamGames/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("Board Size")]
    public int rows = 9;
    public int columns = 9;

    [Header("Match Rules")]
    public int minGroupSize = 2;
    public int rocketCreateGroupSize = 4;

    [Header("Visual Settings")]
    public float cellSize = 1.0f;
}
