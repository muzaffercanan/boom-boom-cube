using System;
using System.Collections;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class GameplaySystemFactory
{
    public GameplaySystems Create(GameplaySystemFactoryConfig config)
    {
        GoalTracker goalTracker = new GoalTracker();
        GridSystem gridSystem = new GridSystem();
        MatchSystem matchSystem = new MatchSystem(gridSystem);
        BoardAnimationConfig animationConfig = config.BoardAnimation ?? BoardAnimationConfig.Default;
        BoardGeometry geometry = new BoardGeometry(config.BoardParent, config.CellSize);
        if (config.ItemFactory != null && config.VisualConfig != null)
        {
            config.ItemFactory.SetVisualConfig(config.VisualConfig);
        }

        GravitySystem gravitySystem = new GravitySystem(gridSystem, geometry, animationConfig);
        RocketHintSystem rocketHintSystem = new RocketHintSystem(gridSystem, matchSystem, config.RocketMatchSize);

        BoardFiller boardFiller = new BoardFiller(
            gridSystem,
            config.ItemFactory,
            config.BoardParent,
            geometry,
            animationConfig);

        BoardResolver boardResolver = new BoardResolver(
            gravitySystem,
            boardFiller.FillEmptySpaces,
            () => rocketHintSystem?.UpdateHints(),
            animationConfig
        );

        SessionLog sessionLog = new SessionLog(config.WriteSessionLogToConsole)
        {
            IsEnabled = config.EnableSessionLog,
            IsSnapshotLoggingEnabled = config.EnableTurnSnapshots
        };
        MoveCounter moveCounter = new MoveCounter(config.EventBus.RaiseMovesUpdated);
        DamageResolver damageResolver = new DamageResolver(gridSystem, goalTracker, config.EventBus.RaiseGoalsUpdated);
        TurnLogger turnLogger = new TurnLogger(sessionLog, gridSystem, goalTracker, moveCounter);

        GameStateController gameStateController = new GameStateController(
            goalTracker,
            config.PlaySound,
            config.WinSfx,
            config.LoseSfx,
            config.ProgressService,
            config.EventBus);

        LevelSessionController levelSessionController = new LevelSessionController(
            config.LevelJson,
            config.UseSessionSeedOverride,
            config.SessionSeedOverride,
            config.EnableSessionLog,
            config.WriteSessionLogToConsole,
            config.EnableTurnSnapshots,
            sessionLog,
            config.ProgressService);

        CubeTurnHandler cubeTurnHandler = new CubeTurnHandler(
            matchSystem,
            boardResolver,
            boardFiller,
            damageResolver,
            moveCounter,
            turnLogger,
            config.MinMatchSize,
            config.RocketMatchSize,
            geometry,
            config.PlaySound,
            config.MatchSfx
        );
        RocketTurnHandler rocketTurnHandler = new RocketTurnHandler(boardResolver, moveCounter, turnLogger);

        TurnProcessor turnProcessor = new TurnProcessor(
            moveCounter,
            cubeTurnHandler,
            rocketTurnHandler,
            damageResolver,
            config.OnTurnComplete,
            config.OnBoardStableBeforeInput);

        RocketSystem rocketSystem = new RocketSystem(
            gridSystem,
            config.ItemFactory,
            config.BoardParent,
            geometry,
            config.CoroutineRunner,
            turnProcessor.HandleDamage,
            config.RocketSfx
        );
        turnProcessor.SetRocketSystem(rocketSystem);
        BoardInputRouter boardInputRouter = new BoardInputRouter(
            gridSystem,
            turnProcessor,
            gameStateController,
            config.CoroutineRunner,
            config.PlaySound,
            config.TapSfx,
            geometry);
        boardFiller.SetClickCallback(boardInputRouter.OnItemClicked);

        LevelLoader levelLoader = new LevelLoader(
            config.ItemFactory,
            config.BoardParent,
            geometry,
            config.EnableLevelLoaderDebugLogs);

        return new GameplaySystems(
            gridSystem,
            matchSystem,
            gravitySystem,
            rocketSystem,
            rocketHintSystem,
            levelLoader,
            boardResolver,
            goalTracker,
            boardFiller,
            turnProcessor,
            boardInputRouter,
            sessionLog,
            gameStateController,
            levelSessionController);
    }
}

public sealed class GameplaySystemFactoryConfig
{
    public ItemFactory ItemFactory { get; set; }
    public BoardVisualConfig VisualConfig { get; set; }
    public Transform BoardParent { get; set; }
    public float CellSize { get; set; }
    public TextAsset LevelJson { get; set; }
    public bool EnableLevelLoaderDebugLogs { get; set; }
    public int MinMatchSize { get; set; }
    public int RocketMatchSize { get; set; }
    public BoardAnimationConfig BoardAnimation { get; set; }
    public bool UseSessionSeedOverride { get; set; }
    public int SessionSeedOverride { get; set; }
    public bool EnableSessionLog { get; set; }
    public bool WriteSessionLogToConsole { get; set; }
    public bool EnableTurnSnapshots { get; set; }
    public AudioClip MatchSfx { get; set; }
    public AudioClip TapSfx { get; set; }
    public AudioClip RocketSfx { get; set; }
    public AudioClip WinSfx { get; set; }
    public AudioClip LoseSfx { get; set; }
    public MonoBehaviour CoroutineRunner { get; set; }
    public Action OnTurnComplete { get; set; }
    public Action<AudioClip> PlaySound { get; set; }
    public Func<IEnumerator> OnBoardStableBeforeInput { get; set; }
    public IProgressService ProgressService { get; set; }
    public IGameplayEventBus EventBus { get; set; }
}
}
