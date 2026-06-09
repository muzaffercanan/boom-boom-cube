using System;
using System.Collections.Generic;
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
public sealed class DamageResolver
{
    private readonly GridSystem _gridSystem;
    private readonly GoalTracker _goalTracker;
    private readonly Action<Dictionary<string, int>> _onGoalsChanged;

    private RocketSystem _rocketSystem;

    public DamageResolver(
        GridSystem gridSystem,
        GoalTracker goalTracker,
        Action<Dictionary<string, int>> onGoalsChanged)
    {
        _gridSystem = gridSystem;
        _goalTracker = goalTracker;
        _onGoalsChanged = onGoalsChanged;
    }

    public void SetRocketSystem(RocketSystem rocketSystem)
    {
        _rocketSystem = rocketSystem;
    }

    public void HandleRocketDamage(Vector2Int position)
    {
        IBoardItem item = _gridSystem.GetItem(position.x, position.y);
        if (item == null) return;

        if (item is RocketItem rocket)
        {
            _rocketSystem?.TriggerRocket(position.x, position.y, rocket);
            return;
        }

        if (item is IDamageable damageable)
        {
            bool destroyed = damageable.TakeDamage(DamageType.RocketHit);
            if (destroyed)
            {
                DestroyItemWithEffect(position.x, position.y, DamageType.RocketHit);
            }
        }
        else
        {
            DestroyItemWithEffect(position.x, position.y, DamageType.RocketHit);
        }
    }

    public void DamageAdjacentObstacles(IEnumerable<IBoardItem> obstacles)
    {
        HashSet<IBoardItem> damaged = new HashSet<IBoardItem>();
        foreach (IBoardItem obstacle in obstacles)
        {
            if (damaged.Contains(obstacle)) continue;
            if (obstacle is IDamageable damageable)
            {
                bool destroyed = damageable.TakeDamage(DamageType.MatchBlast);
                damaged.Add(obstacle);
                if (destroyed)
                {
                    DestroyItemWithEffect(obstacle.X, obstacle.Y, DamageType.MatchBlast);
                }
            }
        }
    }

    public void DestroyItemWithEffect(int x, int y, DamageType type)
    {
        IBoardItem item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        bool goalChanged = _goalTracker.TryRecordDestroyed(item);
        item.PlayDestroyEffect(type);
        _gridSystem.DestroyItem(x, y);

        if (goalChanged)
        {
            _onGoalsChanged?.Invoke(_goalTracker.Counts);
        }
    }
}
}
