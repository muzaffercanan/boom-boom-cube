using System;
using System.Collections;
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
public sealed class CubeTurnHandler
{
    private readonly MatchSystem _matchSystem;
    private readonly BoardResolver _boardResolver;
    private readonly BoardFiller _boardFiller;
    private readonly DamageResolver _damageResolver;
    private readonly MoveCounter _moveCounter;
    private readonly TurnLogger _turnLogger;
    private readonly int _minMatchSize;
    private readonly int _rocketMatchSize;
    private readonly BoardGeometry _geometry;
    private readonly Action<AudioClip> _playSound;
    private readonly AudioClip _matchSfx;

    public CubeTurnHandler(
        MatchSystem matchSystem,
        BoardResolver boardResolver,
        BoardFiller boardFiller,
        DamageResolver damageResolver,
        MoveCounter moveCounter,
        TurnLogger turnLogger,
        int minMatchSize,
        int rocketMatchSize,
        float cellSize,
        Action<AudioClip> playSound,
        AudioClip matchSfx)
        : this(
            matchSystem,
            boardResolver,
            boardFiller,
            damageResolver,
            moveCounter,
            turnLogger,
            minMatchSize,
            rocketMatchSize,
            new BoardGeometry(null, cellSize),
            playSound,
            matchSfx)
    {
    }

    public CubeTurnHandler(
        MatchSystem matchSystem,
        BoardResolver boardResolver,
        BoardFiller boardFiller,
        DamageResolver damageResolver,
        MoveCounter moveCounter,
        TurnLogger turnLogger,
        int minMatchSize,
        int rocketMatchSize,
        BoardGeometry geometry,
        Action<AudioClip> playSound,
        AudioClip matchSfx)
    {
        _matchSystem = matchSystem;
        _boardResolver = boardResolver;
        _boardFiller = boardFiller;
        _damageResolver = damageResolver;
        _moveCounter = moveCounter;
        _turnLogger = turnLogger;
        _minMatchSize = minMatchSize;
        _rocketMatchSize = rocketMatchSize;
        _geometry = geometry ?? new BoardGeometry(null, 1f);
        _playSound = playSound;
        _matchSfx = matchSfx;
    }

    public IEnumerator ProcessTurn(int x, int y, TurnExecutionResult result)
    {
        string beforeSnapshot = _turnLogger.CaptureSnapshot();
        List<IBoardItem> matches = _matchSystem.FindMatches(x, y);
        if (matches.Count < _minMatchSize)
        {
            yield break;
        }

        result.MarkProcessed();
        _moveCounter.UseMove();
        _playSound?.Invoke(_matchSfx);

        bool createdRocket = matches.Count >= _rocketMatchSize;

        _damageResolver.DamageAdjacentObstacles(_matchSystem.GetAdjacentObstacles(matches));

        if (createdRocket)
        {
            yield return AnimateCubesToCenter(matches, x, y);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        foreach (IBoardItem item in matches)
        {
            _damageResolver.DestroyItemWithEffect(item.X, item.Y, DamageType.MatchBlast);
        }

        if (createdRocket)
        {
            _boardFiller.CreateRocket(x, y);
        }

        yield return _boardResolver.ApplyGravityAndFillSequence();
        _turnLogger.RecordTurn(x, y, ItemType.Cube, matches.Count, createdRocket, false, beforeSnapshot);
    }

    private IEnumerator AnimateCubesToCenter(List<IBoardItem> items, int targetX, int targetY)
    {
        Vector3 targetPosition = _geometry.CellToLocalPosition(targetX, targetY);
        float duration = 0.2f;

        foreach (IBoardItem item in items)
        {
            if (item is AbstractBoardItem boardItem)
            {
                boardItem.MoveToPosition(targetPosition, duration);
            }
        }

        yield return new WaitForSeconds(duration);
    }
}
}
