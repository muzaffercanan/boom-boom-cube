using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class AbstractBoardItem : MonoBehaviour, IBoardItem, IPointerClickHandler
{
    private const float DefaultCellSize = 1.0f;

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
            MoveToPosition(new Vector3(X * DefaultCellSize, targetY * DefaultCellSize, 0f), duration);
        }
    }
    
    public void MoveToPosition(Vector3 targetLocalPos, float duration)
    {
        transform.DOKill();
        if (duration <= 0f)
        {
            transform.localPosition = targetLocalPos;
            return;
        }

        transform
            .DOLocalMove(targetLocalPos, duration)
            .SetEase(Ease.OutQuad)
            .SetTarget(transform);
    }
    
    public GameObject GetGameObject() => gameObject;

    protected virtual void OnDestroy()
    {
        transform.DOKill();
    }

    public virtual void OnItemCreated() { }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        _onClickCallback?.Invoke(X, Y);
    }
    
    public virtual void PlayDestroyEffect(DamageType damageType)
    {

    }

    protected void SpawnParticle(ParticleSystem prefab, Color? overrideColor = null)
    {
        if (prefab == null) return;
        
        ParticleSystem instance = ParticlePool.Get(prefab, transform.parent);
        if (instance == null) return;

        instance.transform.position = transform.position;
        
        var main = instance.main;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = 0.25f;
        main.gravityModifier = 0.6f;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f * Mathf.Deg2Rad);
        
        var emission = instance.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 10) }); 

        var shape = instance.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var sizeOverLifetime = instance.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        Color particleColor = overrideColor ?? Color.white;
        main.startColor = particleColor;

        var colorOverLifetime = instance.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = instance.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetFloat("_Mode", 0); 
            mat.SetColor("_Color", particleColor);
            mat.SetTexture("_MainTex", CreateCircleTexture(32, particleColor));
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        instance.Play();
    }

    private static Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = size / 2f;
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius)
                {
                    float alpha = 1f - (dist / radius) * 0.3f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
