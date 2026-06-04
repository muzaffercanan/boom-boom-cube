using System;
using System.Collections;
using UnityEngine;

public sealed class BoardResolver
{
    private readonly GravitySystem _gravitySystem;
    private readonly Action _fillEmptySpaces;
    private readonly Action _updateHints;

    public BoardResolver(GravitySystem gravitySystem, Action fillEmptySpaces, Action updateHints)
    {
        _gravitySystem = gravitySystem;
        _fillEmptySpaces = fillEmptySpaces;
        _updateHints = updateHints;
    }

    public IEnumerator ApplyGravityAndFillSequence()
    {
        yield return ApplyGravityUntilStable(0.08f);

        _fillEmptySpaces?.Invoke();

        yield return new WaitForSeconds(0.2f);
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
