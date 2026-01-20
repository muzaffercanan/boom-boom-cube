using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central manager that controls the game flow, level loading, and board resolution.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private Transform _boardParent;
    [SerializeField] private float _cellSize = 1.0f;
    [SerializeField] private UIManager _uiManager;

    [Header("Level")]
    [SerializeField] private TextAsset _levelJson;

    [Header("Game Rules")]
    [SerializeField] private int _minMatchSize = 2;
    [SerializeField] private int _rocketMatchSize = 4;

    private GridSystem _gridSystem;
    private MatchSystem _matchSystem;
    private GravitySystem _gravitySystem;
    private RocketSystem _rocketSystem;
    private RocketHintSystem _rocketHintSystem;
    private LevelLoader _levelLoader;
    private LevelData _currentLevel;

    private bool _isResolvingGravity;

    private int _remainingMoves;
    private int _totalObstacles;
    private int _destroyedObstacles;

    /// <summary>
    /// Current remaining moves for the level.
    /// </summary>
    public int RemainingMoves => _remainingMoves;

    /// <summary>
    /// Checks if the game has ended (either won or lost).
    /// </summary>
    public bool IsGameOver => _remainingMoves <= 0 || IsLevelComplete;

    /// <summary>
    /// Checks if all level objectives (obstacles) are cleared.
    /// </summary>
    public bool IsLevelComplete => _destroyedObstacles >= _totalObstacles && _totalObstacles > 0;

    private void Start()
    {
        StartCoroutine(InitNextFrame());
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Find UIManager if not assigned
        if (_uiManager == null)
        {
            _uiManager = FindObjectOfType<UIManager>();
            if (_uiManager == null)
            {
                // Create UIManager if doesn't exist
                GameObject go = new GameObject("UIManager");
                _uiManager = go.AddComponent<UIManager>();
            }
        }

        // FIX: Hide leftover LevelButton from MainScene if present in LevelScene
        GameObject levelBtn = GameObject.Find("LevelButton");
        if (levelBtn != null)
        {
            levelBtn.SetActive(false);
        }

        // FIX: Move Background behind the board (Convert UI Image to World Space Sprite)
        GameObject bgObj = GameObject.Find("Background");
        if (bgObj != null && bgObj.GetComponent<UnityEngine.UI.Image>() != null)
        {
            // Move out of Canvas to World Space
            bgObj.transform.SetParent(null);
            
            var img = bgObj.GetComponent<UnityEngine.UI.Image>();
            var sprite = img.sprite;
            Object.Destroy(img); // Remove Image component
            
            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100; // Render behind everything
            
            // Center background behind the grid (assuming approx grid size)
            // Grid is roughly 0 to Width (e.g., 9x9), so center is ~4.5, 4.5
            bgObj.transform.position = new Vector3(4.5f, 6f, 10f); 
            bgObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f); // Adjust scale for world space
        }

        if (_boardParent == null)
        {
            Debug.LogError("[GameManager] BoardParent reference is missing!");
            return;
        }

        _gridSystem = new GridSystem();
        _matchSystem = new MatchSystem(_gridSystem);
        _gravitySystem = new GravitySystem(_gridSystem, _cellSize);
        _rocketSystem = new RocketSystem(_gridSystem, _itemFactory, _boardParent, _cellSize, this, OnDamageRequest);
        _rocketHintSystem = new RocketHintSystem(_gridSystem, _matchSystem, _rocketMatchSize);
        _levelLoader = new LevelLoader(_itemFactory, _boardParent, _cellSize);

        // Dynamic Level Loading
        int levelToLoad = PlayerPrefs.GetInt("SelectedLevelForGame", 1);
        Debug.Log($"[GameManager] Requesting Level {levelToLoad}...");

        // Format level number to "01", "02", ..., "10"
        string formattedLevelNumber = levelToLoad.ToString("D2"); // D2 ensures 01, 05, 10 format

        // Try Loading from Resources first (Standard for multiple levels)
        // Adjust path to coincide with user file structure (level_01, level_02...)
        TextAsset levelAsset = Resources.Load<TextAsset>($"Levels/level_{formattedLevelNumber}"); 
        
        // Fallback checks
        if (levelAsset == null)
        {
             // Try searching without leading zero just in case (e.g. level_1 fallback)
             levelAsset = Resources.Load<TextAsset>($"Levels/level_{levelToLoad}");
        }

        if (levelAsset == null)
        {
             // Try root resources with formatted name
             levelAsset = Resources.Load<TextAsset>($"level_{formattedLevelNumber}");
        }

        if (levelAsset != null)
        {
            LoadLevel(levelAsset.text);
        }
        else if (_levelJson != null)
        {
            Debug.LogWarning($"[GameManager] Dynamic level file not found for level {levelToLoad}. Using Inspector assigned test level.");
            LoadLevel(_levelJson.text);
        }
        else
        {
            Debug.LogError($"[GameManager] NO LEVEL FILE FOUND! Resources path 'Levels/level_{levelToLoad}' is missing and no Inspector fallback assigned.");
        }
    }

    /// <summary>
    /// Loads a level from a JSON string representation.
    /// </summary>
    /// <param name="json">The JSON data string of the level.</param>
    public void LoadLevel(string json)
    {
        _currentLevel = JsonUtility.FromJson<LevelData>(json);
        if (_currentLevel == null)
        {
            Debug.LogError("[GameManager] Failed to parse level JSON data!");
            return;
        }

        _remainingMoves = _currentLevel.move_count;
        _totalObstacles = CountObstaclesInLevel(_currentLevel);
        _destroyedObstacles = 0;

        _levelLoader.LoadLevel(_gridSystem, _currentLevel, OnItemClicked);
        
        // Show initial rocket hints after level loads
        StartCoroutine(UpdateHintsNextFrame());
    }

    private int CountObstaclesInLevel(LevelData data)
    {
        int count = 0;
        foreach (var id in data.grid)
        {
            if (id == "bo" || id == "s" || id == "v")
                count++;
        }
        return count;
    }

    private void OnItemClicked(int x, int y)
    {
        if (IsGameOver)
        {
            Debug.Log("[GameManager] Game is over!");
            return;
        }

        var item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        if (item is RocketItem rocket)
        {
            UseMove();
            _rocketSystem.TryProcessRocketClick(x, y, rocket, out bool isCombo);
            return;
        }

        if (item is IMatchable)
        {
            HandleCubeClick(x, y);
        }
    }

    private void HandleCubeClick(int x, int y)
    {
        var matches = _matchSystem.FindMatches(x, y);

        if (matches.Count < _minMatchSize)
        {
            Debug.Log($"[GameManager] Not enough matches: {matches.Count}");
            return;
        }

        UseMove();

        // FIX: Ensure each obstacle takes max 1 damage per blast group
        // Track which obstacles have been damaged to prevent multi-hit from same blast
        var adjacentObstacles = _matchSystem.GetAdjacentObstacles(matches);
        HashSet<IBoardItem> damagedObstacles = new HashSet<IBoardItem>();
        
        foreach (var obstacle in adjacentObstacles)
        {
            // Skip if already damaged in this blast event
            if (damagedObstacles.Contains(obstacle)) continue;
            
            if (obstacle is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.MatchBlast);
                damagedObstacles.Add(obstacle); // Mark as damaged
                
                if (destroyed)
                {
                    _destroyedObstacles++;
                    _gridSystem.DestroyItem(obstacle.X, obstacle.Y);
                }
            }
        }

        foreach (var it in matches)
        {
            _gridSystem.DestroyItem(it.X, it.Y);
        }

        if (matches.Count >= _rocketMatchSize)
        {
            CreateRocket(x, y);
        }

        ResolveBoard();
        CheckGameState();
    }

    private void OnDamageRequest(Vector2Int pos)
    {
        var item = _gridSystem.GetItem(pos.x, pos.y);
        if (item == null) return;

        bool destroyed = false;

        if (item is IDamageable damageable)
        {
            destroyed = damageable.TakeDamage(DamageType.RocketHit);
            if (destroyed)
            {
                _destroyedObstacles++;
                _gridSystem.DestroyItem(pos.x, pos.y);
            }
        }
        else
        {
            _gridSystem.DestroyItem(pos.x, pos.y);
            destroyed = true;
        }

        if (destroyed)
        {
            ResolveBoard();
            CheckGameState();
        }
    }

    private void CreateRocket(int x, int y)
    {
        bool isHorizontal = Random.value < 0.5f;
        string rocketId = isHorizontal ? "hro" : "vro";

        var rocket = _itemFactory.CreateItem(rocketId, _boardParent);
        if (rocket != null)
        {
            rocket.Init(OnItemClicked);
            _gridSystem.SetItem(x, y, rocket);

            var go = rocket.GetGameObject();
            if (go == null)
            {
                Debug.LogError("[GameManager] Rocket GetGameObject() is NULL");
                return;
            }

            // Apply bottom-left origin conversion
            float worldY = (_currentLevel.grid_height - 1 - y) * _cellSize;
            go.transform.localPosition = new Vector3(x * _cellSize, worldY, 0);
        }
    }

    private void ResolveBoard()
    {
        if (_isResolvingGravity) return;

        _isResolvingGravity = true;

        bool moved;
        do
        {
            moved = _gravitySystem.ApplyGravity();
        }
        while (moved);

        _isResolvingGravity = false;

        FillEmptySpaces();
        
        // Update rocket hints after board settles
        _rocketHintSystem?.UpdateHints();
    }

    private void FillEmptySpaces()
    {
        if (_isResolvingGravity) return;

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                if (_gridSystem.GetItem(x, y) == null)
                {
                    SpawnItemAt(x, y);
                }
            }
        }
    }

    private void SpawnItemAt(int x, int y)
    {
        var newItem = _itemFactory.CreateItem("rand", _boardParent);
        if (newItem != null)
        {
            newItem.Init(OnItemClicked);
            _gridSystem.SetItem(x, y, newItem);

            var go = newItem.GetGameObject();
            if (go == null)
            {
                Debug.LogError($"[GameManager] Spawn GetGameObject() is NULL at ({x},{y})");
                return;
            }

            // Apply bottom-left origin conversion
            float worldY = (_currentLevel.grid_height - 1 - y) * _cellSize;
            go.transform.localPosition = new Vector3(x * _cellSize, worldY, 0);
        }
    }

    private void UseMove()
    {
        _remainingMoves--;
    }

    private void CheckGameState()
    {
        if (IsLevelComplete)
        {
            Debug.Log("[GameManager] LEVEL COMPLETE! You Win!");
            OnLevelWin();
        }
        else if (_remainingMoves <= 0)
        {
            Debug.Log("[GameManager] OUT OF MOVES! You Lose!");
            OnLevelLose();
        }
    }

    private void OnLevelWin()
    {
        // Save progress (Unlock next level)
        int completedLevel = _currentLevel.level_number;
        int savedLevel = PlayerPrefs.GetInt("LastPlayedLevel", 1);
        
        if (completedLevel >= savedLevel)
        {
            PlayerPrefs.SetInt("LastPlayedLevel", completedLevel + 1);
            PlayerPrefs.Save();
        }

        if (_uiManager != null)
        {
            _uiManager.OnLevelWin();
        }
    }

    private void OnLevelLose()
    {
        if (_uiManager != null)
        {
            _uiManager.OnLevelLose();
        }
    }
    
    private IEnumerator UpdateHintsNextFrame()
    {
        yield return null; // Wait one frame for items to be fully instantiated
        _rocketHintSystem?.UpdateHints();
    }
}
