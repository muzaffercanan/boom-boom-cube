using UnityEngine;
using DreamGames.Core;

namespace DreamGames.Board.Systems
{
public interface IBoardItemViewLifecycle
{
    void DestroyView(IBoardItem item);
    void DestroyGameObject(GameObject gameObject);
}

public sealed class UnityBoardItemViewLifecycle : IBoardItemViewLifecycle
{
    public void DestroyView(IBoardItem item)
    {
        if (item == null) return;
        DestroyGameObject(item.GetGameObject());
    }

    public void DestroyGameObject(GameObject gameObject)
    {
        if (gameObject == null) return;

        if (Application.isPlaying)
        {
            Object.Destroy(gameObject);
        }
        else
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
}
