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
        // Update logical coordinate
        SetPosition(X, targetY);
        
        // Use the MoveToPosition system for visuals if active
        if (gameObject.activeInHierarchy)
        {
            // We need to calculate target position here if FallTo is called directly
            // However, usually GravitySystem calls MoveToPosition directly now.
            // But to satisfy IFallable interface robustly:
            MoveToPosition(new Vector3(X * 1.0f, targetY * 1.0f, 0f), duration); 
            // Note: 1.0f is assumed cell size if not provided. 
            // Better to rely on GravitySystem's new logic, but this prevents errors.
        }
    }
    
    // REVISING STRATEGY: 
    // I will implement "MoveToPosition(Vector3 targetPos, float duration)" in AbstractBoardItem
    // GravitySystem will call this.
    
    public void MoveToPosition(Vector3 targetLocalPos, float duration)
    {
        StopAllCoroutines(); // Stop any previous fall
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
