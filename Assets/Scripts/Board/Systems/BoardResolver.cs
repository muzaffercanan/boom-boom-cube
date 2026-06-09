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

namespace DreamGames.Board.Systems
{
public sealed class BoardResolver
{
    private const float DefaultFillAnimationDelay = 0.2f;

    private readonly GravitySystem _gravitySystem;
    private readonly Func<float> _fillEmptySpaces;
    private readonly Action _updateHints;
    private readonly BoardAnimationConfig _animationConfig;

    public BoardResolver(GravitySystem gravitySystem, Action fillEmptySpaces, Action updateHints)
        : this(gravitySystem, () =>
        {
            fillEmptySpaces?.Invoke();
            return DefaultFillAnimationDelay;
        }, updateHints)
    {
    }

    public BoardResolver(
        GravitySystem gravitySystem,
        Func<float> fillEmptySpaces,
        Action updateHints,
        BoardAnimationConfig animationConfig = null)
    {
        _gravitySystem = gravitySystem;
        _fillEmptySpaces = fillEmptySpaces;
        _updateHints = updateHints;
        _animationConfig = animationConfig ?? BoardAnimationConfig.Default;
    }

    public IEnumerator ApplyGravityAndFillSequence()
    {
        float invSpeed = 1f / GameDebug.SpeedMultiplier;
        yield return ApplyGravityUntilStable(_animationConfig.GravityStepDelay * invSpeed);

        float fillAnimationDelay = _fillEmptySpaces?.Invoke() ?? 0f;
        _updateHints?.Invoke();

        if (fillAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(fillAnimationDelay);
        }

        yield return ApplyGravityUntilStable(_animationConfig.PostFillGravityStepDelay * invSpeed);

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
        // One pass computes all final positions and starts staggered animations.
        float animationTime = _gravitySystem.ApplyGravityAndAnimate();
        if (animationTime > 0f)
        {
            _updateHints?.Invoke();
            yield return new WaitForSeconds(animationTime + delayAfterMove);
        }
    }
}
}
