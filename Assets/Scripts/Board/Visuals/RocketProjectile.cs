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
