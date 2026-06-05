using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    private GameManager CreateGameManager()
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
        SetPrivateField(manager, "_itemFactory", factory);
        SetPrivateField(manager, "_boardParent", boardParent);
        SetPrivateField(manager, "_cellSize", 1f);
        SetPrivateField(manager, "_levelButton", levelButton);
        SetPrivateField(manager, "_useSessionSeedOverride", true);
        SetPrivateField(manager, "_sessionSeedOverride", 4242);
        SetPrivateField(manager, "_enableSessionLog", true);
        SetPrivateField(manager, "_writeSessionLogToConsole", false);
        SetPrivateField(manager, "_enableTurnSnapshots", true);
        SetPrivateField(manager, "_shuffleVisualDuration", 0f);

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
