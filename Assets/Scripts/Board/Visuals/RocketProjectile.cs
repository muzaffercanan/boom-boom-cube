using System;
using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private GridSystem _gridSystem;
    private Action<int, int> _onCellHit;
    private float _cellSize;
    
    private int _lastVisitedX = -1;
    private int _lastVisitedY = -1;

    public void Init(Vector2 direction, int startX, int startY, float cellSize, GridSystem grid, Action<int, int> onCellHit)
    {
        _direction = direction;
        _cellSize = cellSize;
        _gridSystem = grid;
        _onCellHit = onCellHit;
        _speed = 15f; 

        _lastVisitedX = startX;
        _lastVisitedY = startY;

    }

    private void Update()
    {
        transform.position += (Vector3)_direction * _speed * Time.deltaTime;

        Vector3 localPos = transform.localPosition;
        
        
        int currentX = Mathf.RoundToInt(localPos.x / _cellSize);
        int currentY = Mathf.RoundToInt(localPos.y / _cellSize);

        if (currentX != _lastVisitedX || currentY != _lastVisitedY)
        {
            if (_gridSystem.IsValid(currentX, currentY))
            {
                _lastVisitedX = currentX;
                _lastVisitedY = currentY;
                _onCellHit?.Invoke(currentX, currentY);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
