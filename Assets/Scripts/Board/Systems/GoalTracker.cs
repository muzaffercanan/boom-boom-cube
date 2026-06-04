using System.Collections.Generic;

public sealed class GoalTracker
{
    private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

    public Dictionary<string, int> Counts => _counts;

    public bool IsComplete
    {
        get
        {
            if (_counts.Count == 0) return false;

            foreach (int count in _counts.Values)
            {
                if (count > 0) return false;
            }

            return true;
        }
    }

    public void Initialize(IEnumerable<string> itemIds)
    {
        _counts.Clear();

        foreach (string id in itemIds)
        {
            if (!IsGoalItem(id)) continue;

            if (!_counts.ContainsKey(id))
            {
                _counts[id] = 0;
            }

            _counts[id]++;
        }
    }

    public bool TryRecordDestroyed(IBoardItem item)
    {
        string goalId = GetGoalIdFromItem(item);
        if (goalId == null || !_counts.ContainsKey(goalId) || _counts[goalId] <= 0)
        {
            return false;
        }

        _counts[goalId]--;
        return true;
    }

    private static bool IsGoalItem(string id)
    {
        return id == ItemIds.Box || id == ItemIds.Stone || id == ItemIds.Vase;
    }

    private static string GetGoalIdFromItem(IBoardItem item)
    {
        if (item is BoxItem) return ItemIds.Box;
        if (item is StoneItem) return ItemIds.Stone;
        if (item is VaseItem) return ItemIds.Vase;
        return null;
    }
}
