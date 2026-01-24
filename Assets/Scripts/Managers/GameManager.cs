using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private Transform _boardParent;
    [SerializeField] private float _cellSize = 1.0f; 
    

    
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPadding = 1.0f;


    public static event Action<LevelData, Dictionary<string, int>> OnLevelLoaded;
    public static event Action<int> OnMovesUpdated;
    public static event Action<Dictionary<string, int>> OnGoalsUpdated;
    public static event Action OnLevelWon;
    public static event Action OnLevelLost;

    [Header("Level")]
    [SerializeField] private TextAsset _levelJson;

    [Header("Game Rules")]
    [SerializeField] private int _minMatchSize = 2;
    [SerializeField] private int _rocketMatchSize = 4;

    [Header("Audio")]
    [SerializeField] private AudioClip _backgroundMusic;
    [SerializeField] private AudioClip _rocketSfx;
    [SerializeField] private AudioClip _winSfx;
    [SerializeField] private AudioClip _loseSfx;
    [SerializeField] private AudioClip _matchSfx;
    [SerializeField] private AudioClip _tapSfx;

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

    public bool IsLevelComplete
    {
        get
        {
            if (_goalCounts == null || _goalCounts.Count == 0) return false;
            foreach (var count in _goalCounts.Values)
            {
                if (count > 0) return false;
            }
            return true;
        }
    }



    private void Start()
    {
        if (AudioManager.Instance != null && _backgroundMusic != null)
        {
            AudioManager.Instance.PlayMusic(_backgroundMusic);
        }
        StartCoroutine(InitNextFrame());
    }

    private void PlaySound(AudioClip clip)
    {
         if (AudioManager.Instance != null && clip != null)
         {
             AudioManager.Instance.PlaySFX(clip);
         }
    }
    


    private IEnumerator InitNextFrame()
    {
        yield return null;
        InitializeGame();
    }

    private void InitializeGame()
    {


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
        _rocketSystem = new RocketSystem(_gridSystem, _itemFactory, _boardParent, _cellSize, this, OnDamageRequest, _rocketSfx);
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

        if (_boardParent != null)
        {
            _boardParent.gameObject.SetActive(true);
        }
    }

    private Dictionary<string, int> _goalCounts = new Dictionary<string, int>();

    public void LoadLevel(string json)
    {
        _currentLevel = JsonUtility.FromJson<LevelData>(json);
        if (_currentLevel == null)
        {
            Debug.LogError("[GameManager] Failed to parse level JSON data!");
            return;
        }

        _remainingMoves = _currentLevel.move_count;
        

        _goalCounts.Clear();
        _totalObstacles = 0;
        
        foreach (var id in _currentLevel.grid)
        {
            if (IsGoalItem(id))
            {
                if (!_goalCounts.ContainsKey(id)) _goalCounts[id] = 0;
                _goalCounts[id]++;
                _totalObstacles++;
            }
        }
        _destroyedObstacles = 0;

        _levelLoader.LoadLevel(_gridSystem, _currentLevel, OnItemClicked);
        
        UpdateGridBackground();
        UpdateCamera();
        StartCoroutine(UpdateHintsNextFrame());


        OnLevelLoaded?.Invoke(_currentLevel, _goalCounts);
        OnMovesUpdated?.Invoke(_remainingMoves);
    }

    private bool IsGoalItem(string id)
    {
        return id == "bo" || id == "s" || id == "v";
    }



    private bool _isProcessingTurn = false;
    private bool _isGameOver = false;

    private void OnValidate()
    {

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


        if (AudioManager.Instance != null && _tapSfx != null) AudioManager.Instance.PlaySFX(_tapSfx);

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
        
        if (AudioManager.Instance != null && _matchSfx != null) 
        {
            AudioManager.Instance.PlaySFX(_matchSfx);
        }
        else
        {
            Debug.LogWarning($"[GameManager] Match SFX Failed. AudioMgr: {AudioManager.Instance != null}, Clip: {_matchSfx != null}");
        }


        BlastIdTracker.NextBlast();

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
                    DestroyItemWithEffect(obstacle.X, obstacle.Y, DamageType.MatchBlast);
                }
            }
        }


        if (createdRocket)
        {
            yield return StartCoroutine(AnimateCubesToCenter(matches, x, y));
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        foreach (var it in matches)
        {
            DestroyItemWithEffect(it.X, it.Y, DamageType.MatchBlast);
        }

        if (createdRocket)
        {
            CreateRocket(x, y);
        }

        yield return StartCoroutine(ApplyGravityAndFillSequence());

        CheckGameState();
        _isProcessingTurn = false;
    }

    private IEnumerator AnimateCubesToCenter(List<IBoardItem> items, int targetX, int targetY)
    {

        Vector3 targetPos = new Vector3(targetX * _cellSize, targetY * _cellSize, 0);
        float duration = 0.2f;

        foreach (var item in items)
        {
            if (item is AbstractBoardItem abi) 
            {

                 abi.MoveToPosition(targetPos, duration);
            }
        }
        yield return new WaitForSeconds(duration);
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


        if (item is RocketItem rocket)
        {
            _rocketSystem.TriggerRocket(pos.x, pos.y, rocket);
            return;
        }

        if (item is IDamageable damageable)
        {
            destroyed = damageable.TakeDamage(DamageType.RocketHit);
            if (destroyed)
            {
                _destroyedObstacles++;
                DestroyItemWithEffect(pos.x, pos.y, DamageType.RocketHit);
            }
        }
        else
        {
            DestroyItemWithEffect(pos.x, pos.y, DamageType.RocketHit);
            destroyed = true;
        }
    }

    private void DestroyItemWithEffect(int x, int y, DamageType type)
    {
        var item = _gridSystem.GetItem(x, y);
        if (item == null)
        {
            return;
        }

        bool goalChanged = false;

        string goalId = GetGoalIdFromItem(item);
        if (goalId != null && _goalCounts.ContainsKey(goalId))
        {
            if (_goalCounts[goalId] > 0)
            {
                _goalCounts[goalId]--;
                goalChanged = true;
            }
        }

        item.PlayDestroyEffect(type);
        _gridSystem.DestroyItem(x, y);

        if (goalChanged)
        {
            OnGoalsUpdated?.Invoke(_goalCounts);
        }
    }


    private string GetGoalIdFromItem(IBoardItem item)
    {
        if (item is BoxItem) return "bo";
        if (item is StoneItem) return "s";
        if (item is VaseItem) return "v";
        return null;
    }

    private void CreateRocket(int x, int y)
    {
        bool isHorizontal = UnityEngine.Random.value < 0.5f;
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
        OnMovesUpdated?.Invoke(_remainingMoves);
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

        OnLevelWon?.Invoke();

        if (AudioManager.Instance != null && _winSfx != null) AudioManager.Instance.PlaySFX(_winSfx);

        if (_boardParent != null) _boardParent.gameObject.SetActive(false);
        
        GameObject levelBtn = GameObject.Find("LevelButton");
        if (levelBtn != null) levelBtn.SetActive(true);
    }

    private void OnLevelLose()
    {
        OnLevelLost?.Invoke();
        
        if (AudioManager.Instance != null && _loseSfx != null) AudioManager.Instance.PlaySFX(_loseSfx);

        if (_boardParent != null) _boardParent.gameObject.SetActive(false);

        GameObject levelBtn = GameObject.Find("LevelButton");
        if (levelBtn != null) levelBtn.SetActive(true);
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


        if (_gridBackgroundRenderer.transform.parent != _boardParent)
        {
            _gridBackgroundRenderer.transform.SetParent(_boardParent);
        }

        _gridBackgroundRenderer.gameObject.SetActive(true);
        _gridBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;


        float gridWidth = _currentLevel.grid_width * _cellSize;
        float gridHeight = _currentLevel.grid_height * _cellSize;


        _gridBackgroundRenderer.size = new Vector2(gridWidth + _backgroundPadding * 2, gridHeight + _backgroundPadding * 2);


        float centerX = (gridWidth - _cellSize) / 2f;
        float centerY = (gridHeight - _cellSize) / 2f;
        

        _gridBackgroundRenderer.transform.localPosition = new Vector3(centerX, centerY, 0.5f);
        
        Debug.Log($"[GameManager] Grid Background Configured: GridSize({_currentLevel.grid_width}x{_currentLevel.grid_height}) " +
                  $"CellSize({_cellSize}) Size({_gridBackgroundRenderer.size.x}x{_gridBackgroundRenderer.size.y}) " +
                  $"Center({centerX}, {centerY})");
    }

    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraPadding = 2.0f;

    private void UpdateCamera()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        if (_currentLevel == null) return;


        float gridWidth = _currentLevel.grid_width * _cellSize;
        float gridHeight = _currentLevel.grid_height * _cellSize;


        Vector3 centerPos = new Vector3(
            (gridWidth - _cellSize) / 2.0f, 
            (gridHeight - _cellSize) / 2.0f
        );
        
        Vector3 centerWorldPos = _boardParent.TransformPoint(centerPos);
        centerWorldPos.z = -10f;
        _camera.transform.position = centerWorldPos;


        
        float targetHeight = gridHeight + _cameraPadding * 2;
        float targetWidth = gridWidth + _cameraPadding * 2;

        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = targetWidth / targetHeight;

        if (screenRatio >= targetRatio)
        {

            _camera.orthographicSize = targetHeight / 2.0f;
        }
        else
        {

            float differenceInSize = targetRatio / screenRatio;
            _camera.orthographicSize = targetHeight / 2.0f * differenceInSize;
        }
        
        Debug.Log($"[GameManager] Camera Updated: Pos={centerWorldPos}, Size={_camera.orthographicSize}");
    }
}
