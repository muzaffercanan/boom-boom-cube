using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages scene transitions with a fade effect.
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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (_faderCanvasGroup == null)
            {
                Debug.LogError("[SceneTransitionManager] Fader CanvasGroup is not assigned! Please assign it in the Inspector.");
            }
            else
            {
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
        yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return StartCoroutine(Fade(1f, 0f, _fadeDuration));
    }

    /// <summary>
    /// Generic fade coroutine backed by DOTween.
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (_faderCanvasGroup == null) yield break;

        if (endAlpha > 0.5f) _faderCanvasGroup.blocksRaycasts = true;

        _faderCanvasGroup.DOKill();
        _faderCanvasGroup.alpha = startAlpha;
        if (duration <= 0f)
        {
            _faderCanvasGroup.alpha = endAlpha;
        }
        else
        {
            yield return DOTween
                .To(
                    () => _faderCanvasGroup.alpha,
                    alpha => _faderCanvasGroup.alpha = alpha,
                    endAlpha,
                    duration)
                .SetEase(Ease.InOutSine)
                .SetTarget(_faderCanvasGroup)
                .WaitForCompletion();
        }

        if (endAlpha < 0.5f) _faderCanvasGroup.blocksRaycasts = false;
    }
}
