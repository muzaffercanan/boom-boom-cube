using DG.Tweening;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private Transform _boardParent;
    [SerializeField] private float _cellSize = 1.0f;
    [SerializeField] private BoardSetupController _boardSetup;
    [SerializeField] private GameObject _levelButton;

    [Header("Level")]
    [SerializeField] private TextAsset _levelJson;
    [SerializeField] private bool _enableLevelLoaderDebugLogs;

    [Header("Game Rules")]
    [SerializeField] private int _minMatchSize = 2;
    [SerializeField] private int _rocketMatchSize = 4;
    [SerializeField] private bool _enableNoMoveShuffle = true;
    [SerializeField] private int _shuffleMaxAttempts = 20;
    [SerializeField] private float _shuffleVisualDuration = 0.25f;

    [Header("Session")]
    [SerializeField] private bool _useSessionSeedOverride;
    [SerializeField] private int _sessionSeedOverride;
    [SerializeField] private bool _enableSessionLog = true;
    [SerializeField] private bool _writeSessionLogToConsole;
    [SerializeField] private bool _enableTurnSnapshots;

    [Header("Audio")]
    [SerializeField] private AudioClip _backgroundMusic;
    [SerializeField] private AudioClip _matchSfx;
    [SerializeField] private AudioClip _tapSfx;
    [SerializeField] private AudioClip _rocketSfx;
    [SerializeField] private AudioClip _winSfx;
    [SerializeField] private AudioClip _loseSfx;

    private GridSystem _gridSystem;
    private MatchSystem _matchSystem;
    private GravitySystem _gravitySystem;
    private RocketSystem _rocketSystem;
    private RocketHintSystem _rocketHintSystem;
    private LevelLoader _levelLoader;
    private BoardResolver _boardResolver;
    private GoalTracker _goalTracker;
    private BoardFiller _boardFiller;
    private TurnProcessor _turnProcessor;
    private TurnEndResolver _turnEndResolver;
    private NoMoveScanner _noMoveScanner;
    private ShuffleSystem _shuffleSystem;
    private SessionLog _sessionLog;
    private GameStateController _gameStateController;
    private LevelData _currentLevel;
    private int _currentSessionSeed;

    public int CurrentSessionSeed => _currentSessionSeed;
    public bool IsProcessingTurn => _turnProcessor != null && _turnProcessor.IsProcessingTurn;
    public int RemainingMoves => _turnProcessor != null ? _turnProcessor.RemainingMoves : 0;
    public SessionLog SessionLog => _sessionLog;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(_backgroundMusic);
        StartCoroutine(InitNextFrame());
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        InitializeSystems();
        yield return LoadCurrentLevelRoutine();
    }

    private void InitializeSystems()
    {
        _goalTracker = new GoalTracker();
        _gridSystem = new GridSystem();
        _matchSystem = new MatchSystem(_gridSystem);
        _gravitySystem = new GravitySystem(_gridSystem, _cellSize);
        _rocketHintSystem = new RocketHintSystem(_gridSystem, _matchSystem, _rocketMatchSize);

        _boardFiller = new BoardFiller(_gridSystem, _itemFactory, _boardParent, _cellSize);
        _boardFiller.SetClickCallback(OnItemClicked);

        _boardResolver = new BoardResolver(
            _gravitySystem,
            _boardFiller.FillEmptySpaces,
            () => _rocketHintSystem?.UpdateHints()
        );

        _sessionLog = new SessionLog(_writeSessionLogToConsole)
        {
            IsEnabled = _enableSessionLog,
            IsSnapshotLoggingEnabled = _enableTurnSnapshots
        };

        _gameStateController = new GameStateController(_goalTracker, PlaySound, _winSfx, _loseSfx);

        _turnProcessor = new TurnProcessor(
            _gridSystem, _matchSystem, _boardResolver, _goalTracker, _boardFiller,
            _minMatchSize, _rocketMatchSize, _cellSize,
            onMovesChanged: GameEvents.RaiseMovesUpdated,
            onGoalsChanged: GameEvents.RaiseGoalsUpdated,
            onTurnComplete: OnTurnComplete,
            playSound: PlaySound,
            matchSfx: _matchSfx,
            onBoardStableBeforeInput: OnBoardStableBeforeInput,
            sessionLog: _sessionLog
        );

        _rocketSystem = new RocketSystem(
            _gridSystem, _itemFactory, _boardParent, _cellSize, this,
            _turnProcessor.HandleDamage, _rocketSfx
        );
        _turnProcessor.SetRocketSystem(_rocketSystem);

        _levelLoader = new LevelLoader(_itemFactory, _boardParent, _cellSize, _enableLevelLoaderDebugLogs);
    }

    private IEnumerator LoadCurrentLevelRoutine()
    {
        if (_levelButton != null) _levelButton.SetActive(false);
        if (_boardParent != null) _boardParent.gameObject.SetActive(true);

        int levelIndex = ProgressService.GetSelectedLevel();
        LevelLoadResult result = null;
        yield return LevelRepository.LoadLevelAsync(levelIndex, loadResult => result = loadResult, _levelJson);

        if (result == null || !result.Success)
        {
            Debug.LogError($"[GameManager] {result?.Error ?? "Level load failed without a result."}");
            yield break;
        }

        LoadLevel(result.LevelData);
    }

    public void LoadLevel(string json)
    {
        LevelLoadResult result = LevelRepository.ParseAndValidate(json, "Inline JSON");
        if (!result.Success)
        {
            Debug.LogError($"[GameManager] {result.Error}");
            return;
        }
        LoadLevel(result.LevelData);
    }

    public void LoadLevel(LevelData levelData)
    {
        string error = LevelRepository.Validate(levelData);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[GameManager] Invalid level: {error}");
            return;
        }

        _currentLevel = levelData;
        _currentSessionSeed = LevelSessionSeed.BeginSession(
            _currentLevel.level_number,
            _useSessionSeedOverride,
            _sessionSeedOverride);
        if (_sessionLog == null)
        {
            _sessionLog = new SessionLog(_writeSessionLogToConsole);
        }
        _sessionLog.IsEnabled = _enableSessionLog;
        _sessionLog.IsSnapshotLoggingEnabled = _enableTurnSnapshots;
        _sessionLog.BeginLevel(_currentLevel.level_number, _currentSessionSeed);
        if (_writeSessionLogToConsole)
        {
            Debug.Log($"[GameManager] Level {_currentLevel.level_number} session seed: {_currentSessionSeed}");
        }
        _turnEndResolver = CreateTurnEndResolver();

        _goalTracker.Initialize(_currentLevel.grid);
        _gameStateController.Reset();
        _turnProcessor.SetRemainingMoves(_currentLevel.move_count);

        _levelLoader.LoadLevel(_gridSystem, _currentLevel, OnItemClicked);
        if (_boardSetup != null) _boardSetup.SetupForLevel(_currentLevel, _boardParent, _cellSize);

        StartCoroutine(UpdateHintsNextFrame());

        GameEvents.RaiseLevelLoaded(_currentLevel, _goalTracker.Counts);
        GameEvents.RaiseMovesUpdated(_turnProcessor.RemainingMoves);
    }

    private void OnItemClicked(int x, int y)
    {
        if (_turnProcessor.IsProcessingTurn || _gameStateController.IsGameOver) return;
        if (_turnProcessor.RemainingMoves <= 0) return;

        PlaySound(_tapSfx);

        var item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        if (item is CubeItem)
            StartCoroutine(_turnProcessor.ProcessCubeTurn(x, y));
        else if (item is RocketItem rocket)
            StartCoroutine(_turnProcessor.ProcessRocketTurn(x, y, rocket));
    }

    private void OnTurnComplete()
    {
        if (_gameStateController.IsGameOver)
        {
            _boardParent?.gameObject.SetActive(false);
            _boardSetup?.HideBackground();
            _levelButton?.SetActive(true);
        }
    }

    private IEnumerator OnBoardStableBeforeInput()
    {
        _gameStateController.CheckAndResolve(_turnProcessor.RemainingMoves, _currentLevel.level_number);
        UpdateLastTurnResult();

        if (_gameStateController.IsGameOver)
        {
            yield break;
        }

        if (!_enableNoMoveShuffle)
        {
            yield break;
        }

        if (_turnEndResolver == null)
        {
            yield break;
        }

        bool needsShuffle = _noMoveScanner != null && !_noMoveScanner.HasPlayableMove();
        if (needsShuffle)
        {
            yield return PlayShuffleVisualTransition(true);
        }

        TurnEndResolution resolution = _turnEndResolver.ResolveAfterBoardStable(false);
        if (resolution.ShuffleTriggered)
        {
            _sessionLog.MarkLastTurnShuffle(true, resolution.ShuffleSucceeded, resolution.ShuffleAttempts);
            _rocketHintSystem?.UpdateHints();
        }

        if (needsShuffle)
        {
            yield return PlayShuffleVisualTransition(false);
        }
    }

    private TurnEndResolver CreateTurnEndResolver()
    {
        _noMoveScanner = new NoMoveScanner(_gridSystem, _minMatchSize);
        _shuffleSystem = new ShuffleSystem(_gridSystem, _minMatchSize, _cellSize, GameRng.Shared, _shuffleMaxAttempts);
        return new TurnEndResolver(_noMoveScanner, _shuffleSystem);
    }

    private void UpdateLastTurnResult()
    {
        string state = "Playing";
        if (_gameStateController.IsGameOver)
        {
            state = _goalTracker.IsComplete ? "Won" : "Lost";
        }

        _sessionLog.UpdateLastTurnResult(state, _turnProcessor.RemainingMoves, _goalTracker.Counts);
    }

    public string ExportSessionLog()
    {
        return _sessionLog != null ? _sessionLog.ToJsonLikeString() : "{}";
    }

    public BoardSnapshot CaptureBoardSnapshot()
    {
        return _gridSystem != null ? BoardSnapshot.FromGrid(_gridSystem) : null;
    }

    private IEnumerator PlayShuffleVisualTransition(bool beforeShuffle)
    {
        if (_shuffleVisualDuration <= 0f || _gridSystem == null)
        {
            yield break;
        }

        float endScale = beforeShuffle ? 0.88f : 1f;
        Sequence sequence = DOTween.Sequence();
        bool hasTween = false;

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                if (item == null || item.GetItemType() != ItemType.Cube)
                {
                    continue;
                }

                GameObject go = item.GetGameObject();
                if (go != null)
                {
                    go.transform.DOKill();
                    sequence.Join(
                        go.transform
                            .DOScale(Vector3.one * endScale, _shuffleVisualDuration)
                            .SetEase(Ease.InOutSine)
                    );
                    hasTween = true;
                }
            }
        }

        if (!hasTween)
        {
            sequence.Kill();
            yield break;
        }

        yield return sequence.WaitForCompletion();
    }

    private void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    private IEnumerator UpdateHintsNextFrame()
    {
        yield return null;
        _rocketHintSystem?.UpdateHints();
    }
}
