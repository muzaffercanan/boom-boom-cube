using UnityEngine;
using System.Collections;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public class GameManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BoardSceneReferences _references = new BoardSceneReferences();
    [SerializeField] private LevelLoadConfig _levelConfig = new LevelLoadConfig();
    [SerializeField] private GameplayRulesConfig _rulesConfig = new GameplayRulesConfig();
    [SerializeField] private SessionConfig _sessionConfig = new SessionConfig();
    [SerializeField] private GameAudioConfig _audioConfig = new GameAudioConfig();
    [SerializeField, HideInInspector] private bool _groupedConfigInitialized;

    [Header("References")]
    [SerializeField, HideInInspector] private ItemFactory _itemFactory;
    [SerializeField, HideInInspector] private Transform _boardParent;
    [SerializeField, HideInInspector] private float _cellSize = 1.0f;
    [SerializeField, HideInInspector] private BoardSetupController _boardSetup;
    [SerializeField, HideInInspector] private GameObject _levelButton;

    [Header("Level")]
    [SerializeField, HideInInspector] private TextAsset _levelJson;
    [SerializeField, HideInInspector] private bool _enableLevelLoaderDebugLogs;

    [Header("Game Rules")]
    [SerializeField, HideInInspector] private int _minMatchSize = 2;
    [SerializeField, HideInInspector] private int _rocketMatchSize = 4;
    [SerializeField, HideInInspector] private bool _enableNoMoveShuffle = true;
    [SerializeField, HideInInspector] private int _shuffleMaxAttempts = 20;
    [SerializeField, HideInInspector] private float _shuffleVisualDuration = 0.25f;

    [Header("Session")]
    [SerializeField, HideInInspector] private bool _useSessionSeedOverride;
    [SerializeField, HideInInspector] private int _sessionSeedOverride;
    [SerializeField, HideInInspector] private bool _enableSessionLog = true;
    [SerializeField, HideInInspector] private bool _writeSessionLogToConsole;
    [SerializeField, HideInInspector] private bool _enableTurnSnapshots;

    [Header("Audio")]
    [SerializeField, HideInInspector] private AudioClip _backgroundMusic;
    [SerializeField, HideInInspector] private AudioClip _matchSfx;
    [SerializeField, HideInInspector] private AudioClip _tapSfx;
    [SerializeField, HideInInspector] private AudioClip _rocketSfx;
    [SerializeField, HideInInspector] private AudioClip _winSfx;
    [SerializeField, HideInInspector] private AudioClip _loseSfx;

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
    private BoardInputRouter _boardInputRouter;
    private TurnEndResolver _turnEndResolver;
    private NoMoveScanner _noMoveScanner;
    private ShuffleSystem _shuffleSystem;
    private ShuffleVisualController _shuffleVisualController;
    private TurnEndFlow _turnEndFlow;
    private SessionLog _sessionLog;
    private GameStateController _gameStateController;
    private LevelSessionController _levelSessionController;
    private IAudioService _audioService;
    private IProgressService _progressService;
    private IGameplayEventBus _eventBus;

    public int CurrentSessionSeed => _levelSessionController != null ? _levelSessionController.CurrentSessionSeed : 0;
    public bool IsProcessingTurn => _turnProcessor != null && _turnProcessor.IsProcessingTurn;
    public int RemainingMoves => _turnProcessor != null ? _turnProcessor.RemainingMoves : 0;
    public SessionLog SessionLog => _sessionLog;

    public GridSystem DebugGrid => _gridSystem;
    public Transform DebugBoardParent => _references.BoardParent;
    public float DebugCellSize => _references.CellSize;
    public int DebugCurrentLevel => _levelSessionController?.CurrentLevel?.level_number ?? 0;
    public bool DebugIsGameOver => _gameStateController?.IsGameOver ?? false;

    private void OnValidate()
    {
        SyncGroupedConfigFromLegacy();
    }

    private void Start()
    {
        SyncGroupedConfigFromLegacy();
        EnsureServices();
        ParticlePool.Clear();
        _audioService.PlayMusic(_audioConfig.BackgroundMusic);
        StartCoroutine(InitNextFrame());

        var debugGo = new GameObject("[DebugPanel]");
        var panel = debugGo.AddComponent<GameDebugPanel>();
        panel.SetGameManager(this);
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        InitializeSystems();
        yield return LoadCurrentLevelRoutine();
    }

    private void InitializeSystems()
    {
        EnsureServices();
        GameplaySystems systems = new GameplaySystemFactory().Create(new GameplaySystemFactoryConfig
        {
            ItemFactory = _references.ItemFactory,
            BoardParent = _references.BoardParent,
            CellSize = _references.CellSize,
            LevelJson = _levelConfig.LevelJson,
            EnableLevelLoaderDebugLogs = _levelConfig.EnableLevelLoaderDebugLogs,
            MinMatchSize = _rulesConfig.MinMatchSize,
            RocketMatchSize = _rulesConfig.RocketMatchSize,
            BoardAnimation = _rulesConfig.BoardAnimation,
            UseSessionSeedOverride = _sessionConfig.UseSessionSeedOverride,
            SessionSeedOverride = _sessionConfig.SessionSeedOverride,
            EnableSessionLog = _sessionConfig.EnableSessionLog,
            WriteSessionLogToConsole = _sessionConfig.WriteSessionLogToConsole,
            EnableTurnSnapshots = _sessionConfig.EnableTurnSnapshots,
            MatchSfx = _audioConfig.MatchSfx,
            TapSfx = _audioConfig.TapSfx,
            RocketSfx = _audioConfig.RocketSfx,
            WinSfx = _audioConfig.WinSfx,
            LoseSfx = _audioConfig.LoseSfx,
            CoroutineRunner = this,
            OnTurnComplete = OnTurnComplete,
            PlaySound = PlaySound,
            OnBoardStableBeforeInput = OnBoardStableBeforeInput,
            ProgressService = _progressService,
            EventBus = _eventBus
        });

        _gridSystem = systems.GridSystem;
        _matchSystem = systems.MatchSystem;
        _gravitySystem = systems.GravitySystem;
        _rocketSystem = systems.RocketSystem;
        _rocketHintSystem = systems.RocketHintSystem;
        _levelLoader = systems.LevelLoader;
        _boardResolver = systems.BoardResolver;
        _goalTracker = systems.GoalTracker;
        _boardFiller = systems.BoardFiller;
        _turnProcessor = systems.TurnProcessor;
        _boardInputRouter = systems.BoardInputRouter;
        _sessionLog = systems.SessionLog;
        _gameStateController = systems.GameStateController;
        _levelSessionController = systems.LevelSessionController;
    }

    private IEnumerator LoadCurrentLevelRoutine()
    {
        PrepareBoardForLevel();
        yield return _levelSessionController.LoadSelectedLevelRoutine(ApplyLoadedLevel);
    }

    public void LoadLevel(string json)
    {
        if (_levelSessionController.TryBeginLevelFromJson(json))
        {
            ApplyLoadedLevel(_levelSessionController.CurrentLevel);
        }
    }

    public void LoadLevel(LevelData levelData)
    {
        if (_levelSessionController.TryBeginLevel(levelData))
        {
            ApplyLoadedLevel(_levelSessionController.CurrentLevel);
        }
    }

    private void ApplyLoadedLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            return;
        }

        PrepareBoardForLevel();
        CreateTurnEndFlow();

        _goalTracker.Initialize(levelData.grid);
        _gameStateController.Reset();
        _turnProcessor.ResetTurnState();
        _turnProcessor.SetRemainingMoves(levelData.move_count);

        _levelLoader.LoadLevel(_gridSystem, levelData, OnItemClicked);
        if (_references.BoardSetup != null)
        {
            _references.BoardSetup.SetupForLevel(levelData, _references.BoardParent, _references.CellSize);
        }

        StartCoroutine(UpdateHintsNextFrame());

        _eventBus.RaiseLevelLoaded(levelData, _goalTracker.Counts);
        _eventBus.RaiseMovesUpdated(_turnProcessor.RemainingMoves);
    }

    private void PrepareBoardForLevel()
    {
        if (_references.LevelButton != null)
        {
            _references.LevelButton.SetActive(false);
        }

        if (_references.BoardParent != null)
        {
            _references.BoardParent.gameObject.SetActive(true);
        }
    }

    private void OnItemClicked(int x, int y)
    {
        _boardInputRouter?.OnItemClicked(x, y);
    }

    private void OnTurnComplete()
    {
        if (_gameStateController.IsGameOver)
        {
            _references.BoardParent?.gameObject.SetActive(false);
            _references.BoardSetup?.HideBackground();
            _references.LevelButton?.SetActive(true);
        }
    }

    private IEnumerator OnBoardStableBeforeInput()
    {
        LevelData currentLevel = _levelSessionController.CurrentLevel;
        if (currentLevel == null || _turnEndFlow == null)
        {
            yield break;
        }

        yield return _turnEndFlow.RunAfterBoardStable(currentLevel.level_number);
    }

    private void CreateTurnEndFlow()
    {
        _noMoveScanner = new NoMoveScanner(_gridSystem, _rulesConfig.MinMatchSize);
        _shuffleSystem = new ShuffleSystem(
            _gridSystem,
            _rulesConfig.MinMatchSize,
            _references.CellSize,
            GameRng.Shared,
            _rulesConfig.ShuffleMaxAttempts);
        _turnEndResolver = new TurnEndResolver(_noMoveScanner, _shuffleSystem);
        _shuffleVisualController = new ShuffleVisualController(_gridSystem, _rulesConfig.ShuffleVisualDuration);
        _turnEndFlow = new TurnEndFlow(
            _gameStateController,
            _turnProcessor,
            _goalTracker,
            _sessionLog,
            _rocketHintSystem,
            _turnEndResolver,
            _noMoveScanner,
            _shuffleVisualController,
            _rulesConfig.EnableNoMoveShuffle);
    }

    public void DebugAddMoves(int amount)
    {
        _turnProcessor?.AddMoves(amount);
    }

    public void DebugForceCompleteGoals()
    {
        if (_goalTracker == null || _gameStateController == null || _gameStateController.IsGameOver) return;
        _goalTracker.DebugCompleteAll();
        _eventBus?.RaiseGoalsUpdated(_goalTracker.Counts);
        int levelNum = _levelSessionController?.CurrentLevel?.level_number ?? 0;
        _gameStateController.CheckAndResolve(RemainingMoves, levelNum);
        OnTurnComplete();
    }

    public void DebugLoadLevel(int levelNumber)
    {
        if (_progressService == null || IsProcessingTurn) return;
        _progressService.SetSelectedLevel(Mathf.Max(1, levelNumber));
        StartCoroutine(LoadCurrentLevelRoutine());
    }

    public void DebugLoadNextLevel() => DebugLoadLevel(DebugCurrentLevel + 1);

    public void DebugLoadPrevLevel() => DebugLoadLevel(Mathf.Max(1, DebugCurrentLevel - 1));

    [ContextMenu("Debug/Reload Level")]
    public void DebugReloadLevel()
    {
        if (IsProcessingTurn) return;
        StartCoroutine(LoadCurrentLevelRoutine());
    }

    public void DebugForceShuffle()
    {
        if (_shuffleSystem == null || IsProcessingTurn || DebugIsGameOver) return;
        StartCoroutine(DebugForceShuffleRoutine());
    }

    private IEnumerator DebugForceShuffleRoutine()
    {
        if (_shuffleVisualController != null)
            yield return _shuffleVisualController.PlayTransition(true);
        _shuffleSystem.TryShuffleNormalCubesUntilPlayable(out _);
        _rocketHintSystem?.UpdateHints();
        if (_shuffleVisualController != null)
            yield return _shuffleVisualController.PlayTransition(false);
    }

    public void DebugApplyCellSize(float newCellSize)
    {
        if (IsProcessingTurn) return;
        _references.CellSize = Mathf.Max(0.2f, newCellSize);
        InitializeSystems();
        StartCoroutine(LoadCurrentLevelRoutine());
    }

    public void DebugSetItemScale(float scale)
    {
        if (_gridSystem == null) return;
        scale = Mathf.Clamp(scale, 0.2f, 1.5f);
        for (int x = 0; x < _gridSystem.Width; x++)
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                GameObject go = item?.GetGameObject();
                if (go != null) go.transform.localScale = Vector3.one * scale;
            }
    }

    public void DebugReloadWithSize(int width, int height)
    {
        LevelData orig = _levelSessionController?.CurrentLevel;
        if (orig == null || IsProcessingTurn) return;

        int newW = Mathf.Clamp(width,  2, 20);
        int newH = Mathf.Clamp(height, 2, 20);

        var grid = new System.Collections.Generic.List<string>();
        for (int y = 0; y < newH; y++)
            for (int x = 0; x < newW; x++)
            {
                if (x < orig.grid_width && y < orig.grid_height)
                    grid.Add(orig.grid[y * orig.grid_width + x]);
                else
                    grid.Add(ItemIds.Random);
            }

        LoadLevel(new LevelData
        {
            level_number = orig.level_number,
            grid_width   = newW,
            grid_height  = newH,
            move_count   = orig.move_count,
            grid         = grid
        });
    }

    public string ExportSessionLog()
    {
        return _sessionLog != null ? _sessionLog.ToJsonLikeString() : "{}";
    }

    public BoardSnapshot CaptureBoardSnapshot()
    {
        return _gridSystem != null ? BoardSnapshot.FromGrid(_gridSystem) : null;
    }

    private void PlaySound(AudioClip clip)
    {
        EnsureServices();
        _audioService.PlaySfx(clip);
    }

    private IEnumerator UpdateHintsNextFrame()
    {
        yield return null;
        _rocketHintSystem?.UpdateHints();
    }

    private void SyncGroupedConfigFromLegacy()
    {
        if (_references == null) _references = new BoardSceneReferences();
        if (_levelConfig == null) _levelConfig = new LevelLoadConfig();
        if (_rulesConfig == null) _rulesConfig = new GameplayRulesConfig();
        if (_rulesConfig.BoardAnimation == null) _rulesConfig.BoardAnimation = BoardAnimationConfig.Default;
        if (_sessionConfig == null) _sessionConfig = new SessionConfig();
        if (_audioConfig == null) _audioConfig = new GameAudioConfig();

        if (_groupedConfigInitialized)
        {
            return;
        }

        _references.ApplyLegacy(_itemFactory, _boardParent, _cellSize, _boardSetup, _levelButton);
        _levelConfig.ApplyLegacy(_levelJson, _enableLevelLoaderDebugLogs);
        _rulesConfig.ApplyLegacy(
            _minMatchSize,
            _rocketMatchSize,
            _enableNoMoveShuffle,
            _shuffleMaxAttempts,
            _shuffleVisualDuration);
        _sessionConfig.ApplyLegacy(
            _useSessionSeedOverride,
            _sessionSeedOverride,
            _enableSessionLog,
            _writeSessionLogToConsole,
            _enableTurnSnapshots);
        _audioConfig.ApplyLegacy(_backgroundMusic, _matchSfx, _tapSfx, _rocketSfx, _winSfx, _loseSfx);
        _groupedConfigInitialized = true;
    }

    private void EnsureServices()
    {
        if (_audioService == null) _audioService = new UnityAudioService();
        if (_progressService == null) _progressService = new PlayerPrefsProgressService();
        if (_eventBus == null) _eventBus = new StaticGameplayEventBus();
    }
}
}
