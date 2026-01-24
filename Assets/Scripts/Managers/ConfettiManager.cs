using UnityEngine;
using System.Collections;

public class ConfettiManager : MonoBehaviour
{
    [Header("Confetti Assets")]
    [SerializeField] private Texture2D _starTexture;
    [SerializeField] private Texture2D _sparkleTexture;

    private ParticleSystem _starParticles;
    private ParticleSystem _sparkleParticles;
    
    // Fireworks pool or simple instantiation
    private GameObject _confettiRoot;
    private bool _isPlaying;

    private void Awake()
    {
        InitializeConfetti();
    }

    private void InitializeConfetti()
    {
        _confettiRoot = new GameObject("ConfettiSystem_Runtime");
        _confettiRoot.transform.SetParent(transform);
        // Move camera-relative or just high up. 
        // Assuming Camera is at Z=-10, looking at Z=0. 
        // We put confetti at Z=5 to be behind UI but visible? Or in front?
        // Let's keep it at Z=5 based on previous success.
        _confettiRoot.transform.localPosition = new Vector3(0, 8, 5); 
        
        // Create Rain Systems (Confetti)
        _starParticles = CreateRainSystem("StarConfetti", _starTexture, false);
        _sparkleParticles = CreateRainSystem("Sparkles", _sparkleTexture, true);
        
        StopConfetti();
    }

    public void PlayConfetti()
    {
        _isPlaying = true;

        // 1. Start Rain
        if (_starParticles)
        {
            _starParticles.gameObject.SetActive(true);
            _starParticles.Play();
        }
        if (_sparkleParticles)
        {
            _sparkleParticles.gameObject.SetActive(true);
            _sparkleParticles.Play();
        }

        // 2. Start Fireworks Routine
        StartCoroutine(FireworkRoutine());
    }

    public void StopConfetti()
    {
        _isPlaying = false;
        StopAllCoroutines();

        if (_starParticles)
        {
            _starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _starParticles.gameObject.SetActive(false);
        }
        if (_sparkleParticles)
        {
            _sparkleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _sparkleParticles.gameObject.SetActive(false);
        }
    }

    private IEnumerator FireworkRoutine()
    {
        // Simple object pool could be better, but for low frequency instantiation is fine
        while (_isPlaying)
        {
            SpawnFirework();
            // Random delay between fireworks
            yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
        }
    }

    private void SpawnFirework()
    {
        if (!_sparkleTexture) return;

        GameObject firework = new GameObject("FireworkExplosion");
        firework.transform.SetParent(_confettiRoot.transform);
        
        // Random position within screen bounds roughly
        // Assuming orthographic camera width ~5-6
        float randomX = Random.Range(-3f, 3f);
        float randomY = Random.Range(-2f, 3f); // Higher up
        firework.transform.localPosition = new Vector3(randomX, randomY, 0);

        ParticleSystem ps = firework.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop immediately to allow property modification
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        
        Material mat = new Material(Shader.Find("Mobile/Particles/Additive"));
        mat.mainTexture = _sparkleTexture;
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 101; // In front of rain

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f); // Explosive speed
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.gravityModifier = 0.8f; // Fall down
        
        // Random Color for this explosion
        Color randomColor = new Color(Random.value, Random.value, Random.value, 1f);
        // Make sure it's bright
        float maxColor = Mathf.Max(randomColor.r, randomColor.g, randomColor.b);
        if (maxColor < 0.5f) randomColor = Color.white; 
        
        main.startColor = randomColor;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30, 50) }); // One big burst

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Add size fade out
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.2f); // Start small (explosive growth?) or start big? Usually start big then shrink.
        curve.AddKey(0.1f, 1f);
        curve.AddKey(1f, 0f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        ps.Play(); // Start the system!

        // Auto destory script equivalent
        Destroy(firework, 2.0f);
    }

    private ParticleSystem CreateRainSystem(string name, Texture2D texture, bool isAdditive)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(_confettiRoot.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(90, 0, 0); // Point down
        go.SetActive(false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        Material mat;
        if (isAdditive)
            mat = new Material(Shader.Find("Mobile/Particles/Additive"));
        else
            mat = new Material(Shader.Find("Sprites/Default"));

        if (texture != null) mat.mainTexture = texture;

        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 100;

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f); // Faster fall
        main.startSize = isAdditive ? new ParticleSystem.MinMaxCurve(0.2f, 0.4f) : new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = 0.4f;
        
        if (!isAdditive)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.red, 0f), 
                    new GradientColorKey(Color.green, 0.33f),
                    new GradientColorKey(Color.blue, 0.66f),
                    new GradientColorKey(Color.yellow, 1f)
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }
        else
        {
            main.startColor = Color.white;
        }

        var emission = ps.emission;
        emission.rateOverTime = 60f; // Increased Density (was 20)

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12, 1, 1); // Wider coverage
        
        var rotOL = ps.rotationOverLifetime;
        rotOL.enabled = true;
        rotOL.z = new ParticleSystem.MinMaxCurve(90f, 180f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f; // More turbulence
        noise.frequency = 0.5f;

        return ps;
    }
}
