using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.UI
{
public class LevelButtonController : MonoBehaviour
{
    private int _targetLevel;
    private const int MAX_LEVELS = 10;

    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _buttonText; 
    [SerializeField] private AudioClip _clickSound; 
    private IAudioService _audioService;
    private IProgressService _progressService;
    private ISceneLoadService _sceneLoadService;

    private void Start()
    {
        _audioService = new UnityAudioService();
        _progressService = new PlayerPrefsProgressService();
        _sceneLoadService = new UnitySceneLoadService();

        if (_button == null)
            _button = GetComponent<Button>();

        _targetLevel = _progressService.GetLastPlayedLevel();

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
        _audioService.PlaySfx(_clickSound);

        if (_targetLevel > MAX_LEVELS) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelButtonController] Loading Level {_targetLevel}");
#endif
        
        _progressService.SetSelectedLevel(_targetLevel);
        _sceneLoadService.LoadScene(SceneNames.Level);
    }
}
}
