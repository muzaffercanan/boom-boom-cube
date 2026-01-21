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

    public virtual void FallTo(int targetY, float duration)
    {
        SetPosition(X, targetY);
        
        if (gameObject.activeInHierarchy)
        {
            MoveToPosition(new Vector3(X * 1.0f, targetY * 1.0f, 0f), duration); 
        }
    }
    
    public void MoveToPosition(Vector3 targetLocalPos, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateMove(targetLocalPos, duration));
    }

    private System.Collections.IEnumerator AnimateMove(Vector3 targetPos, float duration)
    {
        float t = 0;
        Vector3 startPos = transform.localPosition;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.localPosition = targetPos;
    }
    
    public GameObject GetGameObject() => gameObject;

    public virtual void OnItemCreated() { }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        _onClickCallback?.Invoke(X, Y);
    }
}
