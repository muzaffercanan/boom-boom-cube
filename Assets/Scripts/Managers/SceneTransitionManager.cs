using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages scene transitions with a fade effect using native Unity Coroutines.
/// No external dependencies required.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("CanvasGroup controlling the fade overlay (should be a black panel blocking raycasts)")]
    [SerializeField] private CanvasGroup _faderCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float _fadeDuration = 0.5f;

    private void Awake()
    {
        // Singleton Implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Validate references
            if (_faderCanvasGroup == null)
            {
                Debug.LogError("[SceneTransitionManager] Fader CanvasGroup is not assigned! Please assign it in the Inspector.");
            }
            else
            {
                // Ensure starts transparent and non-blocking
                _faderCanvasGroup.alpha = 0f;
                _faderCanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads the specified scene with a fade-out/fade-in transition.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionToScene(sceneName));
    }

    /// <summary>
    /// Coroutine handling the full transition lifecycle.
    /// </summary>
    private IEnumerator TransitionToScene(string sceneName)
    {
        // 1. Block input and Fade Out (Darken screen)
        yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

        // 2. Load Scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // Unity loads up to 0.9 then waits for activation
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }

        // 3. Fade In (Lighten screen)
        yield return StartCoroutine(Fade(1f, 0f, _fadeDuration));
    }

    /// <summary>
    /// Generic Fade Coroutine using Mathf.Lerp
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (_faderCanvasGroup == null) yield break;

        // Block Raycasts if we are fading to opaque (Fade Out)
        if (endAlpha > 0.5f) _faderCanvasGroup.blocksRaycasts = true;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // Optional: Add SmoothStep for better feel
            t = t * t * (3f - 2f * t); 
            
            _faderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        // Ensure final value is set
        _faderCanvasGroup.alpha = endAlpha;

        // Unblock Raycasts if we are fading to transparent (Fade In)
        if (endAlpha < 0.5f) _faderCanvasGroup.blocksRaycasts = false;
    }
}
