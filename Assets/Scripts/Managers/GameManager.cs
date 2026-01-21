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
    
    [Tooltip("Assign the Background SpriteRenderer from the scene. Must NOT be inside Canvas.")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;

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

        // Background is now set up in the scene directly (not via runtime component swap)
        // Ensure _backgroundRenderer is assigned in Inspector

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
        
        // Create Grid Background fit to level size
        //CreateGridBackground();

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

    private bool _isProcessingTurn = false;

    private void OnItemClicked(int x, int y)
    {
        if (_isProcessingTurn) return;
        
        // Manual game over check if property unavailable, or trust existing
        if (_remainingMoves <= 0) return; 

        var item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        if (item is CubeItem)
        {
            StartCoroutine(ProcessTurn(x, y));
        }
        else if (item is RocketItem rocket)
        {
             StartCoroutine(ProcessTurnRocket(x, y, rocket));
        }
    }

    private IEnumerator ProcessTurn(int x, int y)
    {
        _isProcessingTurn = true;

        var matches = _matchSystem.FindMatches(x, y);
        if (matches.Count < _minMatchSize)
        {
            _isProcessingTurn = false;
            yield break;
        }

        UseMove();

        // 1. Check Rocket Creation
        bool createdRocket = (matches.Count >= _rocketMatchSize);

        // 2. Damage Obstacles
        var adjacentObstacles = _matchSystem.GetAdjacentObstacles(matches);
        HashSet<IBoardItem> damagedObstacles = new HashSet<IBoardItem>();
        
        foreach (var obstacle in adjacentObstacles)
        {
            if (damagedObstacles.Contains(obstacle)) continue;
            
            if (obstacle is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.MatchBlast);
                damagedObstacles.Add(obstacle);
                
                if (destroyed)
                {
                    _destroyedObstacles++;
                    _gridSystem.DestroyItem(obstacle.X, obstacle.Y);
                }
            }
        }

        // 3. Destroy Matches visual delay
        yield return new WaitForSeconds(0.1f);

        foreach (var it in matches)
        {
            _gridSystem.SetItem(it.X, it.Y, null); 
            if (it.GetGameObject() != null)
                Destroy(it.GetGameObject());
        }

        yield return new WaitForSeconds(0.1f);

        // 4. Create Rocket
        if (createdRocket)
        {
            CreateRocket(x, y);
        }

        // 5. Gravity Sequence
        yield return StartCoroutine(ApplyGravityAndFillSequence());

        CheckGameState();
        _isProcessingTurn = false;
    }

    private IEnumerator ProcessTurnRocket(int x, int y, RocketItem rocket)
    {
        _isProcessingTurn = true;
        UseMove();

        bool isCombo;
        _rocketSystem.TryProcessRocketClick(x, y, rocket, out isCombo);
        
        yield return new WaitForSeconds(0.3f); // Wait for rocket launch/travel

        yield return StartCoroutine(ApplyGravityAndFillSequence());

        CheckGameState();
        _isProcessingTurn = false;
    }

    private IEnumerator ApplyGravityAndFillSequence()
    {
        bool moved;
        do
        {
            moved = _gravitySystem.ApplyGravity();
            // Wait less than animation time (0.15s) to allow cascading "flow"
            if (moved) yield return new WaitForSeconds(0.08f); 
        }
        while (moved);

        FillEmptySpaces();
        
        yield return new WaitForSeconds(0.2f); // Wait for spawn
        
        // Re-check gravity after spawn just in case
        do
        {
            moved = _gravitySystem.ApplyGravity();
            if (moved) yield return new WaitForSeconds(0.1f);
        } while (moved);
        
        _rocketHintSystem?.UpdateHints();
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

            // Apply bottom-left origin conversion (Standard Cartesian)
            float worldY = y * _cellSize;
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

            // Apply bottom-left origin conversion (Standard Cartesian)
            float worldY = y * _cellSize;
            go.transform.localPosition = new Vector3(x * _cellSize, worldY, 0);

            // Animation
            Vector3 targetScale = go.transform.localScale; // Capture prefab scale
            go.transform.localScale = Vector3.zero;
            StartCoroutine(ScaleUp(go.transform, 0.2f, targetScale));
        }
    }

    private IEnumerator ScaleUp(Transform target, float duration, Vector3 targetScale)
    {
        float t = 0;
        while (t < 1f)
        {
            if (target == null) yield break;
            t += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        if (target != null) target.localScale = targetScale;
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

    private void CreateGridBackground()
    {
        var gridBgSprite = Resources.Load<Sprite>("UI/Gameplay/grid_background");
        if (gridBgSprite == null)
        {
            Debug.LogWarning("[GameManager] Could not load grid_background sprite from Resources/UI/Gameplay/");
            return;
        }

        GameObject frameObj = new GameObject("GridFrame");
        var sr = frameObj.AddComponent<SpriteRenderer>();
        sr.sprite = gridBgSprite;
        
        // Try to enable sliced mode if sprite supports it, otherwise plain
        try { sr.drawMode = SpriteDrawMode.Sliced; } catch {}

        sr.sortingOrder = -50; 

        // Size: Grid Width/Height + padding
        // User wants "thin" border. 0.15f padding is much thinner than 0.5f.
        float padding = 0.15f; 
        float width = _currentLevel.grid_width * _cellSize + padding;
        float height = _currentLevel.grid_height * _cellSize + padding;
        sr.size = new Vector2(width, height);

        // Center
        float centerX = (_currentLevel.grid_width - 1) * _cellSize / 2f;
        float centerY = (_currentLevel.grid_height - 1) * _cellSize / 2f;
        
        // Adjust Y based on origin fix (Height relative)
        // Since we draw from 0,0 to W,H in world space now (thanks to bottom-left fix)
        // Center is just W/2, H/2
        
        frameObj.transform.position = new Vector3(centerX, centerY, 5f);
    }
}
