using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Tests.PlayMode
{
public class GameManagerPlayModeSmokeTests
{
    private const int TestLevel = 1;

    private readonly List<Object> _createdObjects = new List<Object>();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (Object createdObject in _createdObjects)
        {
            if (createdObject != null)
            {
                Object.Destroy(createdObject);
            }
        }

        _createdObjects.Clear();
        GameRng.ResetShared();
        ProgressService.SetSelectedLevel(TestLevel);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameManager_LoadsLevelAndCreatesGrid()
    {
        GameManager manager = CreateGameManager();

        yield return WaitForLoadedBoard(manager);

        BoardSnapshot snapshot = manager.CaptureBoardSnapshot();
        Assert.IsNotNull(snapshot);
        Assert.Greater(snapshot.Width, 0);
        Assert.Greater(snapshot.Height, 0);
        Assert.AreNotEqual(0, manager.CurrentSessionSeed);
        Assert.IsNotNull(manager.SessionLog);
    }

    [UnityTest]
    public IEnumerator GameManager_ValidTapResolvesTurnAndDecreasesMoves()
    {
        GameManager manager = CreateGameManager();
        yield return WaitForLoadedBoard(manager);

        GridSystem grid = GetPrivateField<GridSystem>(manager, "_gridSystem");
        NoMoveScanner scanner = new NoMoveScanner(grid, 2);
        Assert.IsTrue(scanner.TryFindPlayableMove(out PlayableMove move));

        int movesBefore = manager.RemainingMoves;
        InvokePrivate(manager, "OnItemClicked", move.X, move.Y);

        Assert.IsTrue(manager.IsProcessingTurn);
        yield return new WaitUntil(() => !manager.IsProcessingTurn);

        Assert.AreEqual(movesBefore - 1, manager.RemainingMoves);
        Assert.GreaterOrEqual(manager.SessionLog.Turns.Count, 1);
    }

    [UnityTest]
    public IEnumerator GameManager_InputLockClosesAndReopensAroundTurn()
    {
        GameManager manager = CreateGameManager();
        yield return WaitForLoadedBoard(manager);

        GridSystem grid = GetPrivateField<GridSystem>(manager, "_gridSystem");
        NoMoveScanner scanner = new NoMoveScanner(grid, 2);
        Assert.IsTrue(scanner.TryFindPlayableMove(out PlayableMove move));

        InvokePrivate(manager, "OnItemClicked", move.X, move.Y);
        bool lockedDuringTurn = manager.IsProcessingTurn;

        yield return new WaitUntil(() => !manager.IsProcessingTurn);

        Assert.IsTrue(lockedDuringTurn);
        Assert.IsFalse(manager.IsProcessingTurn);
    }

    [UnityTest]
    public IEnumerator BoardAnimationProfile_ControlsSpawnDurationInPlayMode()
    {
        BoardAnimationConfig animationConfig = new BoardAnimationConfig
        {
            SpawnRowsAboveBoard = 2f,
            FallMoveDuration = 0.2f,
            GravityStepDelay = 0f,
            LandingBounceDuration = 0f,
            SpawnStartScale = 1f
        };
        GameManager manager = CreateGameManager(animationConfig);
        yield return WaitForLoadedBoard(manager);

        GridSystem grid = GetPrivateField<GridSystem>(manager, "_gridSystem");
        BoardFiller boardFiller = GetPrivateField<BoardFiller>(manager, "_boardFiller");

        grid.DestroyItem(0, 0);
        yield return null;

        float duration = boardFiller.FillEmptySpaces();
        float fallCells = grid.Height + animationConfig.SpawnRowsAboveBoard;
        float expectedDuration = animationConfig.GetFallTotalTime(fallCells, 0);

        Assert.AreEqual(expectedDuration, duration, 0.0001f);
    }

    [UnityTest]
    public IEnumerator GameManager_LoadLevelAfterGameOverReactivatesBoardAndAcceptsInput()
    {
        GameManager manager = CreateGameManager();
        yield return WaitForLoadedBoard(manager);

        BoardSceneReferences references = GetPrivateField<BoardSceneReferences>(manager, "_references");
        Transform boardParent = references.BoardParent;
        GameStateController gameStateController = GetPrivateField<GameStateController>(manager, "_gameStateController");

        gameStateController.CheckAndResolve(0, TestLevel);
        InvokePrivate(manager, "OnTurnComplete");

        Assert.IsTrue(gameStateController.IsGameOver);
        Assert.IsFalse(boardParent.gameObject.activeSelf);

        manager.LoadLevel(CreateInlineLevel(2));
        yield return null;

        Assert.IsTrue(boardParent.gameObject.activeSelf);
        Assert.IsFalse(gameStateController.IsGameOver);

        GridSystem grid = GetPrivateField<GridSystem>(manager, "_gridSystem");
        NoMoveScanner scanner = new NoMoveScanner(grid, 2);
        Assert.IsTrue(scanner.TryFindPlayableMove(out PlayableMove move));

        int movesBefore = manager.RemainingMoves;
        InvokePrivate(manager, "OnItemClicked", move.X, move.Y);

        Assert.IsTrue(manager.IsProcessingTurn);
        yield return new WaitUntil(() => !manager.IsProcessingTurn);

        Assert.AreEqual(movesBefore - 1, manager.RemainingMoves);
    }

    private GameManager CreateGameManager(BoardAnimationConfig animationConfig = null)
    {
        ProgressService.SetSelectedLevel(TestLevel);

        GameObject root = new GameObject("PlayModeSmokeRoot");
        _createdObjects.Add(root);

        Transform boardParent = new GameObject("BoardParent").transform;
        boardParent.SetParent(root.transform);

        GameObject levelButton = new GameObject("LevelButton");
        levelButton.transform.SetParent(root.transform);

        ItemFactory factory = CreateRuntimeFactory(root.transform);

        GameManager manager = root.AddComponent<GameManager>();
        SetPrivateField(manager, "_references", new BoardSceneReferences
        {
            ItemFactory = factory,
            BoardParent = boardParent,
            LevelButton = levelButton,
            CellSize = 1f
        });
        SetPrivateField(manager, "_levelConfig", new LevelLoadConfig());
        SetPrivateField(manager, "_rulesConfig", new GameplayRulesConfig
        {
            MinMatchSize = 2,
            RocketMatchSize = 4,
            EnableNoMoveShuffle = true,
            ShuffleMaxAttempts = 20,
            ShuffleVisualDuration = 0f,
            BoardAnimation = animationConfig ?? BoardAnimationConfig.Default
        });
        SetPrivateField(manager, "_sessionConfig", new SessionConfig
        {
            UseSessionSeedOverride = true,
            SessionSeedOverride = 4242,
            EnableSessionLog = true,
            WriteSessionLogToConsole = false,
            EnableTurnSnapshots = true
        });
        SetPrivateField(manager, "_audioConfig", new GameAudioConfig());
        SetPrivateField(manager, "_groupedConfigInitialized", true);

        return manager;
    }

    private ItemFactory CreateRuntimeFactory(Transform parent)
    {
        ItemFactory factory = ScriptableObject.CreateInstance<ItemFactory>();
        _createdObjects.Add(factory);

        factory.mappings = new List<ItemFactory.ItemPrefabMap>
        {
            CreateMapping(ItemId.Red, ItemIds.Red, CreateBoardItemPrefab<CubeItem>("RedCube", parent)),
            CreateMapping(ItemId.Green, ItemIds.Green, CreateBoardItemPrefab<CubeItem>("GreenCube", parent)),
            CreateMapping(ItemId.Blue, ItemIds.Blue, CreateBoardItemPrefab<CubeItem>("BlueCube", parent)),
            CreateMapping(ItemId.Yellow, ItemIds.Yellow, CreateBoardItemPrefab<CubeItem>("YellowCube", parent)),
            CreateMapping(ItemId.Box, ItemIds.Box, CreateBoardItemPrefab<BoxItem>("Box", parent)),
            CreateMapping(ItemId.HorizontalRocket, ItemIds.HorizontalRocket, CreateBoardItemPrefab<RocketItem>("HorizontalRocket", parent)),
            CreateMapping(ItemId.VerticalRocket, ItemIds.VerticalRocket, CreateBoardItemPrefab<RocketItem>("VerticalRocket", parent))
        };

        SetPrivateField(factory, "_cacheDirty", true);
        return factory;
    }

    private GameObject CreateBoardItemPrefab<T>(string name, Transform parent) where T : Component
    {
        GameObject prefab = new GameObject(name);
        prefab.transform.SetParent(parent);
        prefab.AddComponent<SpriteRenderer>();
        prefab.AddComponent<T>();
        return prefab;
    }

    private static ItemFactory.ItemPrefabMap CreateMapping(ItemId itemId, string id, GameObject prefab)
    {
        return new ItemFactory.ItemPrefabMap
        {
            itemId = itemId,
            id = id,
            prefab = prefab
        };
    }

    private static IEnumerator WaitForLoadedBoard(GameManager manager)
    {
        yield return new WaitUntil(() =>
        {
            BoardSnapshot snapshot = manager.CaptureBoardSnapshot();
            return snapshot != null && snapshot.Width > 0 && snapshot.Height > 0;
        });

        yield return null;
    }

    private static LevelData CreateInlineLevel(int levelNumber)
    {
        return new LevelData
        {
            level_number = levelNumber,
            grid_width = 3,
            grid_height = 1,
            move_count = 5,
            grid = new List<string>
            {
                ItemIds.Red,
                ItemIds.Red,
                ItemIds.Blue
            }
        };
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field: {fieldName}");
        return (T)field.GetValue(target);
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing method: {methodName}");
        method.Invoke(target, args);
    }
}
}
