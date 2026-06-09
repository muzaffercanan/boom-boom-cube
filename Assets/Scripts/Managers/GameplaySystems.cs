using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public sealed class GameplaySystems
{
    public GridSystem GridSystem { get; }
    public MatchSystem MatchSystem { get; }
    public GravitySystem GravitySystem { get; }
    public RocketSystem RocketSystem { get; }
    public RocketHintSystem RocketHintSystem { get; }
    public LevelLoader LevelLoader { get; }
    public BoardResolver BoardResolver { get; }
    public GoalTracker GoalTracker { get; }
    public BoardFiller BoardFiller { get; }
    public TurnProcessor TurnProcessor { get; }
    public BoardInputRouter BoardInputRouter { get; }
    public SessionLog SessionLog { get; }
    public GameStateController GameStateController { get; }
    public LevelSessionController LevelSessionController { get; }

    public GameplaySystems(
        GridSystem gridSystem,
        MatchSystem matchSystem,
        GravitySystem gravitySystem,
        RocketSystem rocketSystem,
        RocketHintSystem rocketHintSystem,
        LevelLoader levelLoader,
        BoardResolver boardResolver,
        GoalTracker goalTracker,
        BoardFiller boardFiller,
        TurnProcessor turnProcessor,
        BoardInputRouter boardInputRouter,
        SessionLog sessionLog,
        GameStateController gameStateController,
        LevelSessionController levelSessionController)
    {
        GridSystem = gridSystem;
        MatchSystem = matchSystem;
        GravitySystem = gravitySystem;
        RocketSystem = rocketSystem;
        RocketHintSystem = rocketHintSystem;
        LevelLoader = levelLoader;
        BoardResolver = boardResolver;
        GoalTracker = goalTracker;
        BoardFiller = boardFiller;
        TurnProcessor = turnProcessor;
        BoardInputRouter = boardInputRouter;
        SessionLog = sessionLog;
        GameStateController = gameStateController;
        LevelSessionController = levelSessionController;
    }
}
}
