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

    public BoardInputRouter(
        GridSystem gridSystem,
        TurnProcessor turnProcessor,
        GameStateController gameStateController,
        MonoBehaviour coroutineRunner,
        Action<AudioClip> playSound,
        AudioClip tapSfx)
    {
        _gridSystem = gridSystem;
        _turnProcessor = turnProcessor;
        _gameStateController = gameStateController;
        _coroutineRunner = coroutineRunner;
        _playSound = playSound;
        _tapSfx = tapSfx;
    }

    public void OnItemClicked(int x, int y)
    {
        if (_turnProcessor.IsProcessingTurn || _gameStateController.IsGameOver) return;
        if (_turnProcessor.RemainingMoves <= 0) return;

        _playSound?.Invoke(_tapSfx);

        IBoardItem item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        if (item is CubeItem)
        {
            _coroutineRunner.StartCoroutine(_turnProcessor.ProcessCubeTurn(x, y));
        }
        else if (item is RocketItem rocket)
        {
            _coroutineRunner.StartCoroutine(_turnProcessor.ProcessRocketTurn(x, y, rocket));
        }
    }
}
}
