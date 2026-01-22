using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private Transform _boardParent;
    [SerializeField] private float _cellSize = 1.0f; 
    [SerializeField] private UIManager _uiManager;
    
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPadding = 1.0f;

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

    public int RemainingMoves => _remainingMoves;

    public bool IsGameOver => _remainingMoves <= 0 || IsLevelComplete;

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
        if (_uiManager == null)
        {
            _uiManager = FindObjectOfType<UIManager>();
            if (_uiManager == null)
            {
                GameObject go = new GameObject("UIManager");
                _uiManager = go.AddComponent<UIManager>();
            }
        }

        GameObject levelBtn = GameObject.Find("LevelButton");
        if (levelBtn != null)
        {
            levelBtn.SetActive(false);
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

        int levelToLoad = PlayerPrefs.GetInt("SelectedLevelForGame", 1);
        Debug.Log($"[GameManager] Requesting Level {levelToLoad}...");

        string levelJsonPath = System.IO.Path.Combine(Application.dataPath, "Levels", $"level_{levelToLoad.ToString("D2")}.json");
        string jsonContent = null;

        if (System.IO.File.Exists(levelJsonPath))
        {
            jsonContent = System.IO.File.ReadAllText(levelJsonPath);
            LoadLevel(jsonContent);
        }
        else if (_levelJson != null)
        {
            LoadLevel(_levelJson.text);
        }
        else
        {
            Debug.LogError($"[GameManager] NO LEVEL FILE FOUND! Path '{levelJsonPath}' is missing and no Inspector fallback assigned.");
        }
    }

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
        
        if (_uiManager != null)
        {
            _uiManager.UpdateHUD(_remainingMoves, _totalObstacles - _destroyedObstacles);
        }

        UpdateGridBackground();
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
    private bool _isGameOver = false;

    private void OnValidate()
    {
        // Allow live background updates in the editor when values change
        if (_gridBackgroundRenderer != null && _currentLevel != null)
        {
            UpdateGridBackground();
        }
    }

    private void OnItemClicked(int x, int y)
    {
        if (_isProcessingTurn || _isGameOver) return;
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

        bool createdRocket = (matches.Count >= _rocketMatchSize);

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

        yield return new WaitForSeconds(0.1f);

        foreach (var it in matches)
        {
            _gridSystem.SetItem(it.X, it.Y, null); 
            if (it.GetGameObject() != null)
                Destroy(it.GetGameObject());
        }

        yield return new WaitForSeconds(0.1f);

        if (createdRocket)
        {
            CreateRocket(x, y);
        }

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
        
        yield return new WaitForSeconds(0.8f); 

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
            if (moved) yield return new WaitForSeconds(0.08f); 
        }
        while (moved);

        FillEmptySpaces();
        
        yield return new WaitForSeconds(0.2f); 
        
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
            if (_uiManager != null)
            {
                _uiManager.UpdateHUD(_remainingMoves, _totalObstacles - _destroyedObstacles);
            }
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

            float worldY = y * _cellSize;
            go.transform.localPosition = new Vector3(x * _cellSize, worldY, 0);

            Vector3 targetScale = go.transform.localScale; 
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
        if (_uiManager != null)
        {
            _uiManager.UpdateHUD(_remainingMoves, _totalObstacles - _destroyedObstacles);
        }
    }

    private void CheckGameState()
    {
        if (_isGameOver) return;

        if (IsLevelComplete)
        {
            _isGameOver = true;
            Debug.Log("[GameManager] LEVEL COMPLETE! You Win!");
            OnLevelWin();
        }
        else if (_remainingMoves <= 0)
        {
            _isGameOver = true;
            Debug.Log("[GameManager] OUT OF MOVES! You Lose!");
            OnLevelLose();
        }
    }

    private void OnLevelWin()
    {
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
        yield return null; 
        _rocketHintSystem?.UpdateHints();
    }

    private void UpdateGridBackground()
    {
        if (_gridBackgroundRenderer == null)
        {
            Debug.LogWarning("[GameManager] Grid Background Renderer is not assigned!");
            return;
        }

        if (_currentLevel == null) return;

        // Ensure the background is parented to the board for consistent local coordinates
        if (_gridBackgroundRenderer.transform.parent != _boardParent)
        {
            _gridBackgroundRenderer.transform.SetParent(_boardParent);
        }

        _gridBackgroundRenderer.gameObject.SetActive(true);
        _gridBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;

        // Total grid dimensions based on counts and cell size
        float gridWidth = _currentLevel.grid_width * _cellSize;
        float gridHeight = _currentLevel.grid_height * _cellSize;

        // Set size with symmetric padding
        _gridBackgroundRenderer.size = new Vector2(gridWidth + _backgroundPadding * 2, gridHeight + _backgroundPadding * 2);

        // The items are at (0..W-1)*cellSize, (0..H-1)*cellSize
        // Their geometric center is (W-1)*cellSize/2
        float centerX = (gridWidth - _cellSize) / 2f;
        float centerY = (gridHeight - _cellSize) / 2f;
        
        // Position at center, slightly behind items
        _gridBackgroundRenderer.transform.localPosition = new Vector3(centerX, centerY, 0.5f);
        
        Debug.Log($"[GameManager] Grid Background Configured: GridSize({_currentLevel.grid_width}x{_currentLevel.grid_height}) " +
                  $"CellSize({_cellSize}) Size({_gridBackgroundRenderer.size.x}x{_gridBackgroundRenderer.size.y}) " +
                  $"Center({centerX}, {centerY})");
    }
}
