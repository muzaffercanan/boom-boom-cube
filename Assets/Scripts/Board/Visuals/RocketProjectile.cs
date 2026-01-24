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
    private int _maxRange = -1; 

    public void Init(Vector2 direction, int startX, int startY, float cellSize, GridSystem grid, Action<int, int> onCellHit, int maxRange = -1)
    {
        _direction = direction;
        _cellSize = cellSize;
        _gridSystem = grid;
        _onCellHit = onCellHit;
        _speed = 12f; 

        _startX = startX;
        _startY = startY;
        
        _lastVisitedX = startX;
        _lastVisitedY = startY;
        
        _maxRange = maxRange;

        CreateVisuals();
    }

    private void CreateVisuals()
    {
        CreateTrail();
        CreateFireParticles();
    }

    private void CreateTrail()
    {
        GameObject trailObj = new GameObject("RocketTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        
        Material trailMat = new Material(Shader.Find("Sprites/Default"));
        trail.material = trailMat;

        trail.startWidth = _cellSize * 0.8f; 
        trail.endWidth = _cellSize * 0.6f;
        trail.time = 0.35f; 
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 1f, 0.8f), 0f), 
                new GradientColorKey(new Color(1f, 0.6f, 0f), 0.5f),
                new GradientColorKey(new Color(1f, 0.4f, 0f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        trail.colorGradient = gradient;
        
        trail.minVertexDistance = 0.1f;
    }

    private void CreateFireParticles()
    {
        GameObject particleObj = new GameObject("RocketFireParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        particleObj.transform.localRotation = Quaternion.Euler(GetParticleRotation());

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1; 

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.2f), new Color(1f, 0.5f, 0f));
        main.gravityModifier = 0.1f; 
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 80f; 

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0f), 
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.6f),
                new GradientColorKey(Color.red, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGrad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 1f;
        noise.damping = true;
    }

    private Vector3 GetParticleRotation()
    {
        if (_direction == Vector2.right)
            return new Vector3(0f, 0f, 180f); 
        else if (_direction == Vector2.left)
            return new Vector3(0f, 0f, 0f);   
        else if (_direction == Vector2.up)
            return new Vector3(0f, 0f, 90f);  
        else if (_direction == Vector2.down)
            return new Vector3(0f, 0f, -90f); 
        
        return Vector3.zero;
    }

    private void Update()
    {
        transform.position += (Vector3)_direction * _speed * Time.deltaTime;

        Vector3 localPos = transform.localPosition;
        
        
        int currentX = Mathf.RoundToInt(localPos.x / _cellSize);
        int currentY = Mathf.RoundToInt(localPos.y / _cellSize);

        if (_maxRange > 0)
        {
            int distanceTraveled = Mathf.Abs(currentX - _startX) + Mathf.Abs(currentY - _startY);
            
            if (distanceTraveled > _maxRange)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (currentX != _lastVisitedX || currentY != _lastVisitedY)
        {
            HitCellsInPath(_lastVisitedX, _lastVisitedY, currentX, currentY);
            
            _lastVisitedX = currentX;
            _lastVisitedY = currentY;
        }
    }

    private void HitCellsInPath(int fromX, int fromY, int toX, int toY)
    {
        int stepX = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int stepY = toY > fromY ? 1 : (toY < fromY ? -1 : 0);
        
        int x = fromX;
        int y = fromY;
        
        while (x != toX || y != toY)
        {
            if (x != toX) x += stepX;
            if (y != toY) y += stepY;
            
            if (_gridSystem.IsValid(x, y))
            {
                _onCellHit?.Invoke(x, y);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
