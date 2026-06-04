using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

    private class TestBoardItem : IBoardItem
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Init(Action<int, int> onClickCallback) { }
        public ItemType GetItemType() => ItemType.None;
        public GameObject GetGameObject() => null;
        public void PlayDestroyEffect(DamageType damageType) { }
    }

    private sealed class TestMatchableItem : TestBoardItem, IMatchable
    {
        private readonly CubeColor _color;

        public TestMatchableItem(CubeColor color)
        {
            _color = color;
        }

        public CubeColor GetColor() => _color;
        public bool CanMatch() => true;
    }

    private class TestDamageableItem : TestBoardItem, IDamageable
    {
        public int Health => 1;

        public bool TakeDamage(DamageType type) => true;
    }

}
