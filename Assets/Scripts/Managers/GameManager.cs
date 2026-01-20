using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private Transform _boardParent;
    [SerializeField] private float _cellSize = 1.0f;

    [Header("Level")]
    [SerializeField] private TextAsset _levelJson;

    [Header("Game Rules")]
    [SerializeField] private int _minMatchSize = 2;
    [SerializeField] private int _rocketMatchSize = 4;

    private GridSystem _gridSystem;
    private MatchSystem _matchSystem;
    private GravitySystem _gravitySystem;
    private LevelLoader _levelLoader;
    private LevelData _currentLevel;

    private int _remainingMoves;
    private int _totalObstacles;
    private int _destroyedObstacles;

    public int RemainingMoves => _remainingMoves;
    public bool IsGameOver => _remainingMoves <= 0 || IsLevelComplete;
    public bool IsLevelComplete => _destroyedObstacles >= _totalObstacles && _totalObstacles > 0;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        _gridSystem = new GridSystem();
        _matchSystem = new MatchSystem(_gridSystem);
        _gravitySystem = new GravitySystem(_gridSystem);
        _levelLoader = new LevelLoader(_itemFactory, _boardParent, _cellSize);

        if (_levelJson != null)
        {
            LoadLevel(_levelJson.text);
        }
        else
        {
            Debug.LogError("[GameManager] No level JSON assigned!");
        }
    }

    public void LoadLevel(string json)
    {
        _currentLevel = JsonUtility.FromJson<LevelData>(json);
        if (_currentLevel == null)
        {
            Debug.LogError("[GameManager] Failed to parse level JSON!");
            return;
        }

        _remainingMoves = _currentLevel.move_count;
        _totalObstacles = CountObstaclesInLevel(_currentLevel);
        _destroyedObstacles = 0;

        _levelLoader.LoadLevel(_gridSystem, _currentLevel, OnItemClicked);

        Debug.Log($"[GameManager] Level {_currentLevel.level_number} loaded. Moves: {_remainingMoves}, Obstacles: {_totalObstacles}");
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
            HandleRocketClick(x, y, rocket);
            return;
        }

        if (item is IMatchable matchable)
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

        var adjacentObstacles = _matchSystem.GetAdjacentObstacles(matches);
        foreach (var obstacle in adjacentObstacles)
        {
            if (obstacle is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.MatchBlast);
                if (destroyed)
                {
                    _destroyedObstacles++;
                    _gridSystem.DestroyItem(obstacle.X, obstacle.Y);
                }
            }
        }

        foreach (var item in matches)
        {
            _gridSystem.DestroyItem(item.X, item.Y);
        }

        if (matches.Count >= _rocketMatchSize)
        {
            CreateRocket(x, y);
        }

        _gravitySystem.ApplyGravity();
        FillEmptySpaces();

        CheckGameState();
    }

    private void HandleRocketClick(int x, int y, RocketItem rocket)
    {
        UseMove();

        List<Vector2Int> cellsToClear = new List<Vector2Int>();

        if (rocket.IsHorizontal)
        {
            for (int i = 0; i < _gridSystem.Width; i++)
            {
                cellsToClear.Add(new Vector2Int(i, y));
            }
        }
        else
        {
            for (int i = 0; i < _gridSystem.Height; i++)
            {
                cellsToClear.Add(new Vector2Int(x, i));
            }
        }

        foreach (var pos in cellsToClear)
        {
            var item = _gridSystem.GetItem(pos.x, pos.y);
            if (item == null) continue;

            if (item is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.RocketHit);
                if (destroyed)
                {
                    _destroyedObstacles++;
                    _gridSystem.DestroyItem(pos.x, pos.y);
                }
            }
            else
            {
                _gridSystem.DestroyItem(pos.x, pos.y);
            }
        }

        _gravitySystem.ApplyGravity();
        FillEmptySpaces();

        CheckGameState();
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
            rocket.GetGameObject().transform.localPosition = new Vector3(x, y, 0) * _cellSize;
        }
    }

    private void FillEmptySpaces()
    {
        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                if (_gridSystem.GetItem(x, y) == null)
                {
                    var newItem = _itemFactory.CreateItem("rand", _boardParent);
                    if (newItem != null)
                    {
                        newItem.Init(OnItemClicked);
                        _gridSystem.SetItem(x, y, newItem);
                        newItem.GetGameObject().transform.localPosition = new Vector3(x, y, 0) * _cellSize;
                    }
                }
            }
        }
    }

    private void UseMove()
    {
        _remainingMoves--;
        Debug.Log($"[GameManager] Move used. Remaining: {_remainingMoves}");
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
        // TODO: Show win popup, particles, etc.
    }

    private void OnLevelLose()
    {
        // TODO: Show lose popup
    }
}
