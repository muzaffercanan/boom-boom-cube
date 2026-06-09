using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Tests.EditMode
{
public class BoardSystemsEditModeTests
{
    [Test]
    public void GridSystem_SetItem_StoresItemAndUpdatesPosition()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);
        TestBoardItem item = new TestBoardItem();

        grid.SetItem(2, 1, item);

        Assert.AreSame(item, grid.GetItem(2, 1));
        Assert.AreEqual(2, item.X);
        Assert.AreEqual(1, item.Y);
        Assert.IsNull(grid.GetItem(-1, 0));
    }

    [Test]
    public void MatchSystem_FindMatches_ReturnsConnectedSameColorItemsOnly()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);

        TestMatchableItem redA = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem redB = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem redC = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem isolatedRed = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem blue = new TestMatchableItem(CubeColor.Blue);

        grid.SetItem(0, 0, redA);
        grid.SetItem(1, 0, redB);
        grid.SetItem(1, 1, redC);
        grid.SetItem(2, 2, isolatedRed);
        grid.SetItem(0, 1, blue);

        MatchSystem matchSystem = new MatchSystem(grid);

        List<IBoardItem> matches = matchSystem.FindMatches(0, 0);

        Assert.AreEqual(3, matches.Count);
        Assert.Contains(redA, matches);
        Assert.Contains(redB, matches);
        Assert.Contains(redC, matches);
        Assert.IsFalse(matches.Contains(isolatedRed));
        Assert.IsFalse(matches.Contains(blue));
    }

    [Test]
    public void MatchSystem_FindMatches_DoesNotCountDiagonalConnections()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 2);

        TestMatchableItem redA = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem redDiagonal = new TestMatchableItem(CubeColor.Red);

        grid.SetItem(0, 0, redA);
        grid.SetItem(1, 1, redDiagonal);

        MatchSystem matchSystem = new MatchSystem(grid);

        List<IBoardItem> matches = matchSystem.FindMatches(0, 0);

        Assert.AreEqual(1, matches.Count);
        Assert.Contains(redA, matches);
        Assert.IsFalse(matches.Contains(redDiagonal));
    }

    [Test]
    public void MatchSystem_GetAdjacentObstacles_DeduplicatesSharedObstacles()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 2);

        TestMatchableItem redA = new TestMatchableItem(CubeColor.Red);
        TestMatchableItem redB = new TestMatchableItem(CubeColor.Red);
        TestDamageableItem obstacle = new TestDamageableItem();

        grid.SetItem(0, 0, redA);
        grid.SetItem(1, 0, redB);
        grid.SetItem(0, 1, obstacle);

        MatchSystem matchSystem = new MatchSystem(grid);

        List<IBoardItem> adjacent = matchSystem.GetAdjacentObstacles(new List<IBoardItem> { redA, redB });

        Assert.AreEqual(1, adjacent.Count);
        Assert.AreSame(obstacle, adjacent[0]);
    }

    [Test]
    public void LevelRepository_Validate_RejectsGridSizeMismatch()
    {
        LevelData data = new LevelData
        {
            level_number = 1,
            grid_width = 2,
            grid_height = 2,
            move_count = 10,
            grid = new List<string> { ItemIds.Red, ItemIds.Blue, ItemIds.Green }
        };

        string error = LevelRepository.Validate(data);

        Assert.IsNotNull(error);
        Assert.IsTrue(error.Contains("Expected: 4"));
    }

    [Test]
    public void GoalTracker_TracksOnlyObstacleGoals()
    {
        GoalTracker tracker = new GoalTracker();
        tracker.Initialize(new[] { ItemIds.Box, ItemIds.Red, ItemIds.Box, ItemIds.Stone });

        Assert.AreEqual(2, tracker.Counts[ItemIds.Box]);
        Assert.AreEqual(1, tracker.Counts[ItemIds.Stone]);
        Assert.IsFalse(tracker.Counts.ContainsKey(ItemIds.Red));
        Assert.IsFalse(tracker.IsComplete);

        GameObject boxObject = new GameObject("TestBox");
        try
        {
            BoxItem box = boxObject.AddComponent<BoxItem>();

            Assert.IsTrue(tracker.TryRecordDestroyed(box));
            Assert.AreEqual(1, tracker.Counts[ItemIds.Box]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(boxObject);
        }
    }

    [Test]
    public void NoMoveScanner_ReturnsTrue_WhenConnectedPairExists()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Red));
        grid.SetItem(1, 0, new TestMatchableItem(CubeColor.Red));
        grid.SetItem(2, 0, new TestMatchableItem(CubeColor.Blue));

        NoMoveScanner scanner = new NoMoveScanner(grid, 2);

        Assert.IsTrue(scanner.HasPlayableMove());
    }

    [Test]
    public void NoMoveScanner_ReturnsFalse_ForSingleTile()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Red));

        NoMoveScanner scanner = new NoMoveScanner(grid, 2);

        Assert.IsFalse(scanner.HasPlayableMove());
    }

    [Test]
    public void NoMoveScanner_ReturnsFalse_ForCheckerboard()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);
        FillCheckerboard(grid);

        NoMoveScanner scanner = new NoMoveScanner(grid, 2);

        Assert.IsFalse(scanner.HasPlayableMove());
    }

    [Test]
    public void NoMoveScanner_DoesNotCountObstacleOrEmptyCellsAsPlayable()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 1);
        grid.SetItem(0, 0, new TestDamageableItem());
        grid.SetItem(2, 0, new TestMatchableItem(CubeColor.Red));

        NoMoveScanner scanner = new NoMoveScanner(grid, 2);

        Assert.IsFalse(scanner.HasPlayableMove());
    }

    [Test]
    public void GravitySystem_ApplyUntilStable_DoesNotLeaveGapBelowFallableItem()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 4);
        TestMatchableItem item = new TestMatchableItem(CubeColor.Blue);
        grid.SetItem(0, 3, item);

        GravitySystem gravitySystem = new GravitySystem(grid, 1f);
        int guard = 0;
        while (gravitySystem.ApplyGravity() && guard < 10)
        {
            guard++;
        }

        Assert.AreSame(item, grid.GetItem(0, 0));
        Assert.IsNull(grid.GetItem(0, 1));
        Assert.IsNull(grid.GetItem(0, 2));
        Assert.IsNull(grid.GetItem(0, 3));
        Assert.AreEqual(0, item.Y);
    }

    [Test]
    public void GravitySystem_AnimatedGravityCompactsColumnToFinalSlots()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 4);
        TestMatchableItem lower = new TestMatchableItem(CubeColor.Blue);
        TestMatchableItem upper = new TestMatchableItem(CubeColor.Red);
        grid.SetItem(0, 2, lower);
        grid.SetItem(0, 3, upper);

        GravitySystem gravitySystem = new GravitySystem(grid, 1f);

        float duration = gravitySystem.ApplyGravityAndAnimate();

        Assert.Greater(duration, 0f);
        Assert.AreSame(lower, grid.GetItem(0, 0));
        Assert.AreSame(upper, grid.GetItem(0, 1));
        Assert.IsNull(grid.GetItem(0, 2));
        Assert.IsNull(grid.GetItem(0, 3));
    }

    [Test]
    public void BoardResolver_RefreshesHintsAfterEachGravityStep()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 3);
        TestMatchableItem item = new TestMatchableItem(CubeColor.Red);
        grid.SetItem(0, 2, item);

        List<int> hintedRows = new List<int>();
        BoardResolver resolver = new BoardResolver(
            new GravitySystem(grid, 1f),
            () => 0f,
            () => hintedRows.Add(item.Y));

        RunEnumerator(resolver.ApplyGravityAndFillSequence());

        Assert.Contains(0, hintedRows);
    }

    [Test]
    public void RocketHintSystem_HidesBaseSpriteWhileHintIsActive()
    {
        GameObject redObject = CreateRuntimeCubeObject("HintRedA", CubeColor.Red, out CubeItem redCube, out SpriteRenderer redRenderer);
        GameObject secondObject = CreateRuntimeCubeObject("HintRedB", CubeColor.Red, out CubeItem secondCube, out _);

        try
        {
            GridSystem grid = new GridSystem();
            grid.Initialize(2, 1);
            grid.SetItem(0, 0, redCube);
            grid.SetItem(1, 0, secondCube);

            MatchSystem matchSystem = new MatchSystem(grid);
            Assert.AreEqual(2, matchSystem.FindMatches(0, 0).Count);

            Sprite hintSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f));
            RocketHintSystem hintSystem = new RocketHintSystem(grid, matchSystem, 2, _ => hintSprite);
            hintSystem.UpdateHints();

            Assert.IsFalse(redRenderer.enabled);
            Assert.IsNotNull(redObject.transform.Find("RocketHint"));

            secondCube.Init(CubeColor.Blue);
            hintSystem.UpdateHints();

            Assert.IsTrue(redRenderer.enabled);
            Assert.IsFalse(redObject.transform.Find("RocketHint").gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(redObject);
            UnityEngine.Object.DestroyImmediate(secondObject);
        }
    }

    [Test]
    public void BoardFiller_ReportsSpawnDurationFromAnimationConfig()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 4);

        GameObject parentObject = new GameObject("BoardFillerTestParent");
        ItemFactory factory = ScriptableObject.CreateInstance<ItemFactory>();
        GameObject redPrefab = CreateRuntimeCubePrefab("RedCubePrefab");

        try
        {
            factory.mappings = new List<ItemFactory.ItemPrefabMap>
            {
                CreateMapping(ItemId.Red, ItemIds.Red, redPrefab),
                CreateMapping(ItemId.Green, ItemIds.Green, redPrefab),
                CreateMapping(ItemId.Blue, ItemIds.Blue, redPrefab),
                CreateMapping(ItemId.Yellow, ItemIds.Yellow, redPrefab)
            };
            SetPrivateField(factory, "_cacheDirty", true);

            BoardAnimationConfig animationConfig = new BoardAnimationConfig
            {
                SpawnRowsAboveBoard = 2f,
                FallMoveDuration = 0.2f,
                GravityStepDelay = 0f,
                LandingBounceDuration = 0f,
                SpawnStartScale = 1f
            };
            BoardFiller filler = new BoardFiller(grid, factory, parentObject.transform, 1f, animationConfig);

            float duration = filler.SpawnItemAt(0, 0);

            float fallCells = grid.Height + animationConfig.SpawnRowsAboveBoard;
            Assert.AreEqual(animationConfig.GetFallTotalTime(fallCells, 0), duration, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(factory);
            UnityEngine.Object.DestroyImmediate(redPrefab);
            UnityEngine.Object.DestroyImmediate(parentObject);
        }
    }

    [Test]
    public void GameRng_ReturnsSameSequence_ForSameSeed()
    {
        GameRng first = new GameRng(12345);
        GameRng second = new GameRng(12345);

        for (int i = 0; i < 8; i++)
        {
            Assert.AreEqual(first.Range(0, 1000), second.Range(0, 1000));
        }
    }

    [Test]
    public void GameRng_ReturnsDifferentSequence_ForDifferentSeeds()
    {
        GameRng first = new GameRng(1);
        GameRng second = new GameRng(2);
        bool foundDifference = false;

        for (int i = 0; i < 8; i++)
        {
            if (first.Range(0, 1000) != second.Range(0, 1000))
            {
                foundDifference = true;
                break;
            }
        }

        Assert.IsTrue(foundDifference);
    }

    [Test]
    public void BoardSnapshot_ReadsBoardSizeAndCellState()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 2);

        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Green));
        grid.SetItem(1, 1, new TestDamageableItem(3));

        BoardSnapshot snapshot = BoardSnapshot.FromGrid(grid);

        Assert.AreEqual(2, snapshot.Width);
        Assert.AreEqual(2, snapshot.Height);

        CellSnapshot cube = snapshot.GetCell(0, 0);
        Assert.IsFalse(cube.IsEmpty);
        Assert.AreEqual(ItemType.Cube, cube.ItemType);
        Assert.IsTrue(cube.HasColor);
        Assert.AreEqual(CubeColor.Green, cube.Color);

        CellSnapshot empty = snapshot.GetCell(1, 0);
        Assert.IsTrue(empty.IsEmpty);

        CellSnapshot obstacle = snapshot.GetCell(1, 1);
        Assert.IsTrue(obstacle.IsObstacle);
        Assert.AreEqual(3, obstacle.Health);
    }

    [Test]
    public void ShuffleSystem_ShufflesNormalCubesUntilPlayable_AndPreservesObstaclesAndItemCount()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);
        FillCheckerboard(grid);
        TestDamageableItem obstacle = new TestDamageableItem();
        grid.SetItem(1, 1, obstacle);

        int itemCountBefore = CountItems(grid);
        NoMoveScanner scanner = new NoMoveScanner(grid, 2);
        Assert.IsFalse(scanner.HasPlayableMove());

        ShuffleSystem shuffleSystem = new ShuffleSystem(grid, 2, rng: new GameRng(7), maxAttempts: 1);

        bool shuffled = shuffleSystem.TryShuffleNormalCubesUntilPlayable(out _);

        Assert.IsTrue(shuffled);
        Assert.IsTrue(scanner.HasPlayableMove());
        Assert.AreSame(obstacle, grid.GetItem(1, 1));
        Assert.AreEqual(itemCountBefore, CountItems(grid));
    }

    [Test]
    public void ShuffleSystem_ReturnsFalseWithoutInfiniteLoop_WhenNoPlayableArrangementExists()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Red));
        grid.SetItem(1, 0, new TestMatchableItem(CubeColor.Blue));

        ShuffleSystem shuffleSystem = new ShuffleSystem(grid, 2, rng: new GameRng(11), maxAttempts: 2);

        bool shuffled = shuffleSystem.TryShuffleNormalCubesUntilPlayable(out int attempts);

        Assert.IsFalse(shuffled);
        Assert.AreEqual(2, attempts);
        Assert.AreEqual(2, CountItems(grid));
    }

    [Test]
    public void LevelSessionSeed_BeginSessionWithSameOverride_ProducesSameSequence()
    {
        try
        {
            LevelSessionSeed.BeginSession(1, true, 777);
            int firstA = GameRng.Shared.Range(0, 1000);
            int secondA = GameRng.Shared.Range(0, 1000);

            LevelSessionSeed.BeginSession(1, true, 777);
            int firstB = GameRng.Shared.Range(0, 1000);
            int secondB = GameRng.Shared.Range(0, 1000);

            Assert.AreEqual(firstA, firstB);
            Assert.AreEqual(secondA, secondB);
        }
        finally
        {
            GameRng.ResetShared();
        }
    }

    [Test]
    public void LevelSessionSeed_BeginSessionWithDifferentOverride_CanProduceDifferentSequence()
    {
        try
        {
            LevelSessionSeed.BeginSession(1, true, 101);
            int first = GameRng.Shared.Range(0, 1000);

            LevelSessionSeed.BeginSession(1, true, 202);
            int second = GameRng.Shared.Range(0, 1000);

            Assert.AreNotEqual(first, second);
        }
        finally
        {
            GameRng.ResetShared();
        }
    }

    [Test]
    public void LevelSessionSeed_BeginSessionWithoutOverride_ReturnsUsableSeed()
    {
        try
        {
            int seed = LevelSessionSeed.BeginSession(3, false, 0);
            int value = GameRng.Shared.Range(0, 4);

            Assert.AreNotEqual(0, seed);
            Assert.GreaterOrEqual(value, 0);
            Assert.Less(value, 4);
        }
        finally
        {
            GameRng.ResetShared();
        }
    }

    [Test]
    public void TurnEndResolver_DoesNotShuffle_WhenPlayableMoveExists()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Red));
        grid.SetItem(1, 0, new TestMatchableItem(CubeColor.Red));
        string before = BoardSnapshot.FromGrid(grid).ToDebugString();

        TurnEndResolver resolver = new TurnEndResolver(
            new NoMoveScanner(grid, 2),
            new ShuffleSystem(grid, 2, rng: new GameRng(3), maxAttempts: 1));

        TurnEndResolution resolution = resolver.ResolveAfterBoardStable(false);

        Assert.AreEqual(TurnEndResolutionStatus.PlayableMoveAvailable, resolution.Status);
        Assert.IsFalse(resolution.ShuffleTriggered);
        Assert.AreEqual(before, BoardSnapshot.FromGrid(grid).ToDebugString());
    }

    [Test]
    public void TurnEndResolver_DetectsNoMoveAndTriggersShuffle()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);
        FillCheckerboard(grid);
        grid.SetItem(1, 1, new TestDamageableItem());

        TurnEndResolver resolver = new TurnEndResolver(
            new NoMoveScanner(grid, 2),
            new ShuffleSystem(grid, 2, rng: new GameRng(5), maxAttempts: 1));

        TurnEndResolution resolution = resolver.ResolveAfterBoardStable(false);

        Assert.AreEqual(TurnEndResolutionStatus.ShuffleAttempted, resolution.Status);
        Assert.IsTrue(resolution.NoMoveDetected);
        Assert.IsTrue(resolution.ShuffleTriggered);
        Assert.IsTrue(resolution.ShuffleSucceeded);
        Assert.IsTrue(new NoMoveScanner(grid, 2).HasPlayableMove());
    }

    [Test]
    public void TurnEndResolver_SkipsShuffleAfterWinGameOver()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(3, 3);
        FillCheckerboard(grid);
        string before = BoardSnapshot.FromGrid(grid).ToDebugString();

        TurnEndResolver resolver = new TurnEndResolver(
            new NoMoveScanner(grid, 2),
            new ShuffleSystem(grid, 2, rng: new GameRng(5), maxAttempts: 1));

        TurnEndResolution resolution = resolver.ResolveAfterBoardStable(true);

        Assert.AreEqual(TurnEndResolutionStatus.SkippedGameOver, resolution.Status);
        Assert.IsFalse(resolution.ShuffleTriggered);
        Assert.AreEqual(before, BoardSnapshot.FromGrid(grid).ToDebugString());
    }

    [Test]
    public void TurnEndResolver_SkipsShuffleAfterFailGameOver()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 2);
        FillCheckerboard(grid);
        string before = BoardSnapshot.FromGrid(grid).ToDebugString();

        TurnEndResolver resolver = new TurnEndResolver(
            new NoMoveScanner(grid, 2),
            new ShuffleSystem(grid, 2, rng: new GameRng(9), maxAttempts: 1));

        TurnEndResolution resolution = resolver.ResolveAfterBoardStable(true);

        Assert.AreEqual(TurnEndResolutionStatus.SkippedGameOver, resolution.Status);
        Assert.IsFalse(resolution.ShuffleTriggered);
        Assert.AreEqual(before, BoardSnapshot.FromGrid(grid).ToDebugString());
    }

    [Test]
    public void SessionLog_BeginLevelStoresSeed()
    {
        SessionLog log = new SessionLog();

        log.BeginLevel(4, 1234);

        Assert.AreEqual(4, log.LevelId);
        Assert.AreEqual(1234, log.Seed);
        Assert.AreEqual(0, log.Turns.Count);
    }

    [Test]
    public void SessionLog_RecordTurnStoresTurnInfo()
    {
        SessionLog log = new SessionLog();
        log.BeginLevel(2, 200);

        TurnLogEntry entry = log.RecordTurn(
            1,
            2,
            ItemType.Cube,
            4,
            true,
            false,
            9,
            new Dictionary<string, int> { { ItemIds.Box, 3 } },
            "Playing");

        Assert.IsNotNull(entry);
        Assert.AreEqual(1, log.Turns.Count);
        Assert.AreEqual(1, entry.TurnIndex);
        Assert.AreEqual(4, entry.MatchSize);
        Assert.IsTrue(entry.BoosterCreated);
        Assert.AreEqual("{bo:3}", entry.GoalsRemaining);
    }

    [Test]
    public void SessionLog_MarkLastTurnShuffleStoresShuffleInfo()
    {
        SessionLog log = new SessionLog();
        log.BeginLevel(2, 200);
        log.RecordTurn(0, 0, ItemType.Cube, 2, false, false, 5, null, "Playing");

        log.MarkLastTurnShuffle(true, true, 3);

        TurnLogEntry entry = log.Turns[0];
        Assert.IsTrue(entry.ShuffleTriggered);
        Assert.IsTrue(entry.ShuffleSucceeded);
        Assert.AreEqual(3, entry.ShuffleAttempts);
    }

    [Test]
    public void SessionLog_UpdateLastTurnResultStoresWinFailState()
    {
        SessionLog log = new SessionLog();
        log.BeginLevel(2, 200);
        log.RecordTurn(0, 0, ItemType.Rocket, 0, false, true, 1, null, "Pending");

        log.UpdateLastTurnResult("Won", 1, new Dictionary<string, int> { { ItemIds.Stone, 0 } });

        TurnLogEntry entry = log.Turns[0];
        Assert.AreEqual("Won", entry.ResultState);
        Assert.AreEqual(1, entry.MovesLeft);
        Assert.AreEqual("{s:0}", entry.GoalsRemaining);
    }

    [Test]
    public void BoardSnapshot_ToDebugString_ExportsSizeAndCells()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Yellow));

        string export = BoardSnapshot.FromGrid(grid).ToDebugString();

        StringAssert.Contains("\"width\":2", export);
        StringAssert.Contains("\"height\":1", export);
        StringAssert.Contains("\"itemType\":\"Cube\"", export);
        StringAssert.Contains("\"color\":\"Yellow\"", export);
        StringAssert.Contains("\"empty\":true", export);
    }

    [Test]
    public void BoardSnapshot_ToDebugString_IsDeterministicForSameBoard()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(2, 2);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Red));
        grid.SetItem(1, 1, new TestDamageableItem(2));

        string first = BoardSnapshot.FromGrid(grid).ToDebugString();
        string second = BoardSnapshot.FromGrid(grid).ToDebugString();

        Assert.AreEqual(first, second);
    }

    [Test]
    public void RocketProjectile_CancelSetsCancelledAndSuppressesFutureHits()
    {
        GameObject projectileObject = new GameObject("TestRocketProjectile");
        try
        {
            GridSystem grid = new GridSystem();
            grid.Initialize(2, 1);
            bool hit = false;

            RocketProjectile projectile = projectileObject.AddComponent<RocketProjectile>();
            projectile.Init(Vector2.right, 0, 0, 1f, grid, (x, y) => hit = true, releaseToPool: _ => { });

            projectile.Cancel();

            Assert.IsTrue(projectile.IsCancelled);
            Assert.IsFalse(hit);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(projectileObject);
        }
    }

    [Test]
    public void SessionLog_ToJsonLikeString_IsDeterministicForSameSession()
    {
        SessionLog first = CreateSampleSessionLog();
        SessionLog second = CreateSampleSessionLog();

        Assert.AreEqual(first.ToJsonLikeString(), second.ToJsonLikeString());
    }

    [Test]
    public void SessionLog_ToJsonLikeString_ExportsSeedAndTurnsInOrder()
    {
        SessionLog log = CreateSampleSessionLog();

        string export = log.ToJsonLikeString();

        StringAssert.Contains("\"levelId\":7", export);
        StringAssert.Contains("\"seed\":7007", export);
        Assert.Less(export.IndexOf("\"turnIndex\":1"), export.IndexOf("\"turnIndex\":2"));
        StringAssert.Contains("\"clickedItemType\":\"Cube\"", export);
        StringAssert.Contains("\"shuffleTriggered\":true", export);
    }

    [Test]
    public void SessionLog_DoesNotStoreSnapshots_WhenSnapshotLoggingDisabled()
    {
        SessionLog log = new SessionLog
        {
            IsSnapshotLoggingEnabled = false
        };
        log.BeginLevel(1, 10);

        TurnLogEntry entry = log.RecordTurn(0, 0, ItemType.Cube, 2, false, false, 4, null, "Playing", "before", "after");

        Assert.IsNull(entry.BeforeSnapshot);
        Assert.IsNull(entry.AfterSnapshot);
        StringAssert.DoesNotContain("beforeSnapshot", log.ToJsonLikeString());
    }

    [Test]
    public void SessionLog_StoresSnapshots_WhenSnapshotLoggingEnabled()
    {
        SessionLog log = new SessionLog
        {
            IsSnapshotLoggingEnabled = true
        };
        log.BeginLevel(1, 10);

        TurnLogEntry entry = log.RecordTurn(0, 0, ItemType.Cube, 2, false, false, 4, null, "Playing", "before", "after");

        Assert.AreEqual("before", entry.BeforeSnapshot);
        Assert.AreEqual("after", entry.AfterSnapshot);
        StringAssert.Contains("\"beforeSnapshot\":\"before\"", log.ToJsonLikeString());
        StringAssert.Contains("\"afterSnapshot\":\"after\"", log.ToJsonLikeString());
    }

    [Test]
    public void SessionLog_ToJsonLikeString_ExportsSnapshotStrings()
    {
        GridSystem grid = new GridSystem();
        grid.Initialize(1, 1);
        grid.SetItem(0, 0, new TestMatchableItem(CubeColor.Blue));

        string snapshot = BoardSnapshot.FromGrid(grid).ToDebugString();
        SessionLog log = new SessionLog
        {
            IsSnapshotLoggingEnabled = true
        };
        log.BeginLevel(3, 333);
        log.RecordTurn(0, 0, ItemType.Cube, 2, false, false, 5, null, "Playing", snapshot, snapshot);

        string export = log.ToJsonLikeString();

        StringAssert.Contains("beforeSnapshot", export);
        StringAssert.Contains("\\\"width\\\":1", export);
        StringAssert.Contains("\\\"color\\\":\\\"Blue\\\"", export);
    }

    [Test]
    public void LevelSessionSeed_FixedSeedCanBeStoredInSessionLog()
    {
        try
        {
            int seed = LevelSessionSeed.BeginSession(5, true, 555);
            SessionLog log = new SessionLog();
            log.BeginLevel(5, seed);

            Assert.AreEqual(555, log.Seed);
            StringAssert.Contains("\"seed\":555", log.ToJsonLikeString());
        }
        finally
        {
            GameRng.ResetShared();
        }
    }

    [Test]
    public void LevelSessionSeed_RandomSeedCanBeStoredInSessionLog()
    {
        try
        {
            int seed = LevelSessionSeed.BeginSession(5, false, 0);
            SessionLog log = new SessionLog();
            log.BeginLevel(5, seed);

            Assert.AreNotEqual(0, log.Seed);
            StringAssert.Contains("\"seed\":", log.ToJsonLikeString());
        }
        finally
        {
            GameRng.ResetShared();
        }
    }

    private class TestBoardItem : IBoardItem
    {
        private readonly ItemType _itemType;

        public int X { get; private set; }
        public int Y { get; private set; }

        public TestBoardItem(ItemType itemType = ItemType.None)
        {
            _itemType = itemType;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Init(Action<int, int> onClickCallback) { }
        public virtual ItemType GetItemType() => _itemType;
        public GameObject GetGameObject() => null;
        public void PlayDestroyEffect(DamageType damageType) { }
    }

    private sealed class TestMatchableItem : TestBoardItem, IMatchable, IFallable
    {
        private readonly CubeColor _color;

        public TestMatchableItem(CubeColor color) : base(ItemType.Cube)
        {
            _color = color;
        }

        public CubeColor GetColor() => _color;
        public bool CanMatch() => true;
        public bool CanFall() => true;
        public void FallTo(int targetY, float duration)
        {
            SetPosition(X, targetY);
        }
    }

    private class TestDamageableItem : TestBoardItem, IDamageable
    {
        public int Health { get; }

        public TestDamageableItem(int health = 1) : base(ItemType.Obstacle)
        {
            Health = health;
        }

        public bool TakeDamage(DamageType type) => true;
    }

    private static void FillCheckerboard(GridSystem grid)
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                CubeColor color = (x + y) % 2 == 0 ? CubeColor.Red : CubeColor.Blue;
                grid.SetItem(x, y, new TestMatchableItem(color));
            }
        }
    }

    private static int CountItems(GridSystem grid)
    {
        int count = 0;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.GetItem(x, y) != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static SessionLog CreateSampleSessionLog()
    {
        SessionLog log = new SessionLog();
        log.BeginLevel(7, 7007);
        log.RecordTurn(
            0,
            1,
            ItemType.Cube,
            3,
            false,
            false,
            9,
            new Dictionary<string, int> { { ItemIds.Box, 2 } },
            "Playing");
        log.RecordTurn(
            2,
            3,
            ItemType.Rocket,
            0,
            false,
            true,
            8,
            new Dictionary<string, int> { { ItemIds.Box, 1 } },
            "Playing");
        log.MarkLastTurnShuffle(true, true, 1);
        return log;
    }

    private static GameObject CreateRuntimeCubeObject(
        string name,
        CubeColor color,
        out CubeItem cube,
        out SpriteRenderer renderer)
    {
        GameObject gameObject = new GameObject(name);
        renderer = gameObject.AddComponent<SpriteRenderer>();
        cube = gameObject.AddComponent<CubeItem>();
        cube.Init(color);
        return gameObject;
    }

    private static GameObject CreateRuntimeCubePrefab(string name)
    {
        GameObject prefab = new GameObject(name);
        prefab.AddComponent<SpriteRenderer>();
        prefab.AddComponent<CubeItem>();
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private static void RunEnumerator(IEnumerator enumerator)
    {
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is IEnumerator nested)
            {
                RunEnumerator(nested);
            }
        }
    }

}
}
