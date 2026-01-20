using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class AbstractBoardItem : MonoBehaviour, IBoardItem, IPointerClickHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }

    protected Action<int, int> _onClickCallback;

    public void Init(Action<int, int> onClickCallback)
    {
        _onClickCallback = onClickCallback;
    }

    public void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
        gameObject.name = $"{GetItemType()}_{X}_{Y}";
    }

    public abstract ItemType GetItemType();
    
    public GameObject GetGameObject() => gameObject;

    public virtual void OnItemCreated() { }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        _onClickCallback?.Invoke(X, Y);
    }
}
