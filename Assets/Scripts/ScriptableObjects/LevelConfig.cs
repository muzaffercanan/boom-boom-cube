using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "DreamGames/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("Board Size")]
    [Range(1, 20)]
    public int rows = 9;
    [Range(1, 20)]
    public int columns = 9;

    [Header("Match Rules")]
    [Range(2, 10)]
    public int minGroupSize = 2;
    [Range(2, 10)]
    public int rocketCreateGroupSize = 4;

    [Header("Visual Settings")]
    [Range(0.1f, 3f)]
    public float cellSize = 1.0f;
}
