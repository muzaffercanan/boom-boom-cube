using System.Collections;
using DG.Tweening;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Visuals
{
public sealed class ShuffleVisualController
{
    private readonly GridSystem _gridSystem;
    private readonly float _duration;

    public ShuffleVisualController(GridSystem gridSystem, float duration)
    {
        _gridSystem = gridSystem;
        _duration = duration;
    }

    public IEnumerator PlayTransition(bool beforeShuffle)
    {
        if (_duration <= 0f || _gridSystem == null)
        {
            yield break;
        }

        float endScale = beforeShuffle ? 0.88f : 1f;
        Sequence sequence = DOTween.Sequence();
        bool hasTween = false;

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                if (item == null || item.GetItemType() != ItemType.Cube)
                {
                    continue;
                }

                GameObject go = item.GetGameObject();
                if (go == null)
                {
                    continue;
                }

                go.transform.DOKill();
                sequence.Join(
                    go.transform
                        .DOScale(Vector3.one * endScale, _duration)
                        .SetEase(Ease.InOutSine)
                );
                hasTween = true;
            }
        }

        if (!hasTween)
        {
            sequence.Kill();
            yield break;
        }

        yield return sequence.WaitForCompletion();
    }
}
}
