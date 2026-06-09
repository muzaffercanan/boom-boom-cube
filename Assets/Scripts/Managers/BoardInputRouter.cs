using System;
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
public sealed class BoardInputRouter
{
    private readonly GridSystem _gridSystem;
    private readonly TurnProcessor _turnProcessor;
    private readonly GameStateController _gameStateController;
    private readonly MonoBehaviour _coroutineRunner;
    private readonly Action<AudioClip> _playSound;
    private readonly AudioClip _tapSfx;
    private readonly BoardGeometry _geometry;

    public BoardInputRouter(
        GridSystem gridSystem,
        TurnProcessor turnProcessor,
        GameStateController gameStateController,
        MonoBehaviour coroutineRunner,
        Action<AudioClip> playSound,
        AudioClip tapSfx,
        BoardGeometry geometry = null)
    {
        _gridSystem = gridSystem;
        _turnProcessor = turnProcessor;
        _gameStateController = gameStateController;
        _coroutineRunner = coroutineRunner;
        _playSound = playSound;
        _tapSfx = tapSfx;
        _geometry = geometry ?? new BoardGeometry(null, 1f);
    }

    public bool TryHandleScreenPosition(Vector2 screenPosition, Camera camera = null)
    {
        Camera inputCamera = camera != null ? camera : Camera.main;
        if (!TryResolveScreenPosition(screenPosition, inputCamera, out Vector2Int cell))
        {
            return false;
        }

        return TryHandleCell(cell.x, cell.y);
    }

    public bool TryResolveScreenPosition(Vector2 screenPosition, Camera camera, out Vector2Int cell)
    {
        cell = default;
        if (_gridSystem == null || _gridSystem.Width <= 0 || _gridSystem.Height <= 0)
        {
            return false;
        }

        return _geometry.TryScreenPositionToCell(camera, screenPosition, _gridSystem.Width, _gridSystem.Height, out cell);
    }

    public bool TryResolveWorldPosition(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = default;
        if (_gridSystem == null || _gridSystem.Width <= 0 || _gridSystem.Height <= 0)
        {
            return false;
        }

        return _geometry.TryWorldPositionToCell(worldPosition, _gridSystem.Width, _gridSystem.Height, out cell);
    }

    public bool TryResolveLocalPosition(Vector2 localPosition, out Vector2Int cell)
    {
        cell = default;
        if (_gridSystem == null || _gridSystem.Width <= 0 || _gridSystem.Height <= 0)
        {
            return false;
        }

        return _geometry.TryLocalPositionToCell(localPosition, _gridSystem.Width, _gridSystem.Height, out cell);
    }

    public void OnItemClicked(int x, int y)
    {
        TryHandleCell(x, y);
    }

    private bool TryHandleCell(int x, int y)
    {
        if (_turnProcessor == null || _gameStateController == null || _coroutineRunner == null)
        {
            return false;
        }

        if (_turnProcessor.IsProcessingTurn || _gameStateController.IsGameOver) return false;
        if (_turnProcessor.RemainingMoves <= 0) return false;
        if (!_gridSystem.IsValid(x, y)) return false;


        IBoardItem item = _gridSystem.GetItem(x, y);
        if (item == null) return false;

        _playSound?.Invoke(_tapSfx);

        if (item is CubeItem)
        {
            _coroutineRunner.StartCoroutine(_turnProcessor.ProcessCubeTurn(x, y));
            return true;
        }

        if (item is RocketItem rocket)
        {
            _coroutineRunner.StartCoroutine(_turnProcessor.ProcessRocketTurn(x, y, rocket));
            return true;
        }

        return false;
    }
}
}
