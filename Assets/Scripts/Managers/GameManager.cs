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
    private GameStateController _gameStateController;
    private LevelData _currentLevel;

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

        _boardFiller = new BoardFiller(_gridSystem, _itemFactory, _boardParent, _cellSize, this);
        _boardFiller.SetClickCallback(OnItemClicked);

        _boardResolver = new BoardResolver(
            _gravitySystem,
            _boardFiller.FillEmptySpaces,
            () => _rocketHintSystem?.UpdateHints()
        );

        _gameStateController = new GameStateController(_goalTracker, PlaySound, _winSfx, _loseSfx);

        _turnProcessor = new TurnProcessor(
            _gridSystem, _matchSystem, _boardResolver, _goalTracker, _boardFiller,
            _minMatchSize, _rocketMatchSize, _cellSize,
            onMovesChanged: GameEvents.RaiseMovesUpdated,
            onGoalsChanged: GameEvents.RaiseGoalsUpdated,
            onTurnComplete: OnTurnComplete,
            playSound: PlaySound,
            matchSfx: _matchSfx
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
        _gameStateController.CheckAndResolve(_turnProcessor.RemainingMoves, _currentLevel.level_number);

        if (_gameStateController.IsGameOver)
        {
            _boardParent?.gameObject.SetActive(false);
            _boardSetup?.HideBackground();
            _levelButton?.SetActive(true);
        }
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
