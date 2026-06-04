using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelButtonController : MonoBehaviour
{
    private int _targetLevel;
    private const int MAX_LEVELS = 10;

    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _buttonText; 
    [SerializeField] private AudioClip _clickSound; 

    private void Start()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        _targetLevel = ProgressService.GetLastPlayedLevel();

        UpdateVisuals();

        if (_button != null)
            _button.onClick.AddListener(LoadLevel);
    }

    private void UpdateVisuals()
    {
        if (_buttonText == null)
            _buttonText = GetComponentInChildren<TMP_Text>();

        if (_buttonText == null)
        {
             var fallbackText = GetComponentInChildren<Text>();
             if (fallbackText != null) 
             {
                 if (_targetLevel > MAX_LEVELS)
                    fallbackText.text = "Finished";
                 else
                    fallbackText.text = $"Level {_targetLevel}";
             }
             return;
        }

        if (_targetLevel > MAX_LEVELS)
        {
            _buttonText.text = "Finished";
            if (_button != null) _button.interactable = false;
        }
        else
        {
            _buttonText.text = $"Level {_targetLevel}";
            if (_button != null) _button.interactable = true;
        }
    }

    void LoadLevel()
    {
        if (AudioManager.Instance != null && _clickSound != null) 
            AudioManager.Instance.PlaySFX(_clickSound);

        if (_targetLevel > MAX_LEVELS) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelButtonController] Loading Level {_targetLevel}");
#endif
        
        ProgressService.SetSelectedLevel(_targetLevel);
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(SceneNames.Level);
        }
        else
        {
            SceneManager.LoadScene(SceneNames.Level);
        }
    }
}
