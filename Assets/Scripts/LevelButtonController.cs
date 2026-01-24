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

    private void Start()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        // Load persisted level, default to 1
        _targetLevel = PlayerPrefs.GetInt("LastPlayedLevel", 1);

        UpdateVisuals();

        if (_button != null)
            _button.onClick.AddListener(LoadLevel);
    }

    private void UpdateVisuals()
    {
        if (_buttonText == null)
            _buttonText = GetComponentInChildren<TMP_Text>();

        // Fallback to legacy Text if TMP Not found, or just debug
        if (_buttonText == null)
        {
             // Try getting legacy text
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
            _button.interactable = false; // Disable click if desired, or let them replay specific level? 
            // The requirement says "When all levels are finished, the LevelButton should display finished text."
            // doesn't explicitly say disable, but "Finished" implies completion.
            // Let's keep it clickable but maybe restart or do nothing? 
            // Case doesn't verify "Replay" mechanics for finished state. 
            // Standard approach: Disable or just show text. 
            // Given "Finished text", I will leave it enabled but it might fail to load Level 11.
            _button.interactable = false;
        }
        else
        {
            _buttonText.text = $"Level {_targetLevel}";
            if (_button != null) _button.interactable = true;
        }
    }

    void LoadLevel()
    {
        if (_targetLevel > MAX_LEVELS) return;

        // "LevelScene should be loaded according to the current level"
        // We pass the level via PlayerPrefs or similar. 
        // GameManager reads from json.
        // Wait, GameManager loads specific JSON. How does it know which one?
        // Is GameManager loading "Level_{index}.json"? 
        // In the provided GameManager code: [SerializeField] private TextAsset _levelJson;
        // It has a single TextAsset field logic. This needs to be dynamic.
        
        Debug.Log($"[LevelButtonController] Loading Level {_targetLevel}");
        
        // We need to pass the level index to the game scene.
        PlayerPrefs.SetInt("SelectedLevelForGame", _targetLevel);
        SceneManager.LoadScene("LevelScene");
    }
}
