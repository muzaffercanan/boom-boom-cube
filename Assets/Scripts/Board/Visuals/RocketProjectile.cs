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
    
    private int _startX;
    private int _startY;
    private int _maxRange = -1; // -1 means unlimited, otherwise max cells to travel

    public void Init(Vector2 direction, int startX, int startY, float cellSize, GridSystem grid, Action<int, int> onCellHit, int maxRange = -1)
    {
        _direction = direction;
        _cellSize = cellSize;
        _gridSystem = grid;
        _onCellHit = onCellHit;
        _speed = 15f; 

        _startX = startX;
        _startY = startY;
        
        // Start from rocket position (don't hit it again, rocket already destroyed)
        _lastVisitedX = startX;
        _lastVisitedY = startY;
        
        _maxRange = maxRange;

        // Create fire particle effect
        CreateFireParticles();
    }

    private void CreateFireParticles()
    {
        GameObject particleObj = new GameObject("RocketFireTrail");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange fire color
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        
        // Emit particles in opposite direction of movement
        shape.rotation = GetParticleRotation();

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.8f, 0f), 0f),  // Yellow-orange
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f), // Orange
                new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f)  // Dark red
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -10;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private Vector3 GetParticleRotation()
    {
        // Rotate particle emission to be opposite of movement direction
        if (_direction == Vector2.right)
            return new Vector3(0f, 0f, 180f); // Emit left
        else if (_direction == Vector2.left)
            return new Vector3(0f, 0f, 0f);   // Emit right
        else if (_direction == Vector2.up)
            return new Vector3(0f, 0f, 90f);  // Emit down
        else if (_direction == Vector2.down)
            return new Vector3(0f, 0f, -90f); // Emit up
        
        return Vector3.zero;
    }

    private void Update()
    {
        transform.position += (Vector3)_direction * _speed * Time.deltaTime;

        Vector3 localPos = transform.localPosition;
        
        
        int currentX = Mathf.RoundToInt(localPos.x / _cellSize);
        int currentY = Mathf.RoundToInt(localPos.y / _cellSize);

        // Check if exceeded max range
        if (_maxRange > 0)
        {
            // Calculate distance from start (excluding start cell itself)
            int distanceTraveled = Mathf.Abs(currentX - _startX) + Mathf.Abs(currentY - _startY);
            
            // Destroy if we've gone beyond max range
            if (distanceTraveled > _maxRange)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (currentX != _lastVisitedX || currentY != _lastVisitedY)
        {
            // Hit all cells between last and current position (in case we skipped some)
            HitCellsInPath(_lastVisitedX, _lastVisitedY, currentX, currentY);
            
            _lastVisitedX = currentX;
            _lastVisitedY = currentY;
        }
    }

    private void HitCellsInPath(int fromX, int fromY, int toX, int toY)
    {
        // Calculate direction of movement
        int stepX = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int stepY = toY > fromY ? 1 : (toY < fromY ? -1 : 0);
        
        int x = fromX;
        int y = fromY;
        
        // Hit all cells from start to end
        while (x != toX || y != toY)
        {
            // Move one step
            if (x != toX) x += stepX;
            if (y != toY) y += stepY;
            
            if (_gridSystem.IsValid(x, y))
            {
                _onCellHit?.Invoke(x, y);
            }
            else
            {
                // Out of bounds, destroy projectile
                Destroy(gameObject);
                return;
            }
        }
    }
}
