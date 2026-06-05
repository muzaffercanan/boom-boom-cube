using System;
using System.Collections;
using UnityEngine;

public sealed class BoardResolver
{
    private const float DefaultFillAnimationDelay = 0.2f;

    private readonly GravitySystem _gravitySystem;
    private readonly Func<float> _fillEmptySpaces;
    private readonly Action _updateHints;

    public BoardResolver(GravitySystem gravitySystem, Action fillEmptySpaces, Action updateHints)
        : this(gravitySystem, () =>
        {
            fillEmptySpaces?.Invoke();
            return DefaultFillAnimationDelay;
        }, updateHints)
    {
    }

    public BoardResolver(GravitySystem gravitySystem, Func<float> fillEmptySpaces, Action updateHints)
    {
        _gravitySystem = gravitySystem;
        _fillEmptySpaces = fillEmptySpaces;
        _updateHints = updateHints;
    }

    public IEnumerator ApplyGravityAndFillSequence()
    {
        yield return ApplyGravityUntilStable(0.08f);

        float fillAnimationDelay = _fillEmptySpaces?.Invoke() ?? 0f;

        if (fillAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(fillAnimationDelay);
        }

        yield return ApplyGravityUntilStable(0.1f);

        _updateHints?.Invoke();
    }

    public void ResolveImmediate()
    {
        bool moved;
        do
        {
            moved = _gravitySystem.ApplyGravity();
        }
        while (moved);

        _fillEmptySpaces?.Invoke();
        _updateHints?.Invoke();
    }

    private IEnumerator ApplyGravityUntilStable(float delayAfterMove)
    {
        bool moved;
        do
        {
            moved = _gravitySystem.ApplyGravity();
            if (moved)
            {
                yield return new WaitForSeconds(delayAfterMove);
            }
        }
        while (moved);
    }
}
