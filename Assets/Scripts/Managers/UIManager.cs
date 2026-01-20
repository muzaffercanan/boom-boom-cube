using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _losePanel;
    [SerializeField] private GameObject _gameHUD; // Moves, Grid background etc.

    [Header("Win UI")]
    [SerializeField] private Button _winMainMenuButton;
    [SerializeField] private ParticleSystem _celebrationParticles;

    [Header("Lose UI")]
    [SerializeField] private Button _loseTryAgainButton;
    [SerializeField] private Button _loseMainMenuButton;
    [SerializeField] private Button _loseCloseButton; // Acts like return to main menu or just close popup? Usually return in this context.

    private void Start()
    {
        // Auto-generate UI if missing
        if (_gameHUD == null) CreateHUD();
        if (_winPanel == null) CreateWinPanel();
        if (_losePanel == null) CreateLosePanel();

        // Ensure panels are hidden at start
        if (_winPanel) _winPanel.SetActive(false);
        if (_losePanel) _losePanel.SetActive(false);
        if (_gameHUD) _gameHUD.SetActive(true);

        // Listeners
        if (_winMainMenuButton) _winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        if (_loseTryAgainButton) _loseTryAgainButton.onClick.AddListener(OnTryAgainClicked);
        if (_loseMainMenuButton) _loseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseCloseButton) _loseCloseButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void CreateHUD()
    {
        // Try to find existing HUD or canvas
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            _gameHUD = canvas.gameObject;

            // FIX: Enforce Scale With Screen Size for 9:16 Portrait support
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
            }
        }
    }

    private void CreateWinPanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("WinPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        // Add background image
        var img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        panel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        panel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        // Add Text
        GameObject textObj = new GameObject("WinText");
        textObj.transform.SetParent(panel.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "LEVEL COMPLETED!";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 60;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.green;
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
        textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        // Add Button
        GameObject btnObj = CreateSimpleButton(panel.transform, "Main Menu", new Vector2(0, -100));
        _winMainMenuButton = btnObj.GetComponent<Button>();

        _winPanel = panel;
    }

    private void CreateLosePanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("LosePanel");
        panel.transform.SetParent(canvas.transform, false);
        
        // Add background image
        var img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        panel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        panel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        // Add Text
        GameObject textObj = new GameObject("LoseText");
        textObj.transform.SetParent(panel.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "LEVEL FAILED!";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 60;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.red;
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
        textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        // Add Buttons
        GameObject retryBtn = CreateSimpleButton(panel.transform, "Try Again", new Vector2(0, -50));
        _loseTryAgainButton = retryBtn.GetComponent<Button>();

        GameObject menuBtn = CreateSimpleButton(panel.transform, "Main Menu", new Vector2(0, -150));
        _loseMainMenuButton = menuBtn.GetComponent<Button>();
        _loseCloseButton = menuBtn.GetComponent<Button>(); // Reuse for close logic

        _losePanel = panel;
    }

    private GameObject CreateSimpleButton(Transform parent, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(parent, false);
        
        var img = btnObj.AddComponent<Image>();
        img.color = Color.white;
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        btnObj.GetComponent<RectTransform>().anchoredPosition = pos;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        textObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        textObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        textObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        textObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        return btnObj;
    }

    public void OnLevelWin()
    {
        if (_gameHUD != null) _gameHUD.SetActive(false);
        if (_winPanel != null) _winPanel.SetActive(true);
        if (_celebrationParticles != null) _celebrationParticles.Play();
        else Debug.LogWarning("[UIManager] Celebration particles not assigned!");
    }

    public void OnLevelLose()
    {
        if (_gameHUD != null) _gameHUD.SetActive(false);
        if (_losePanel != null) _losePanel.SetActive(true);
        else Debug.LogWarning("[UIManager] Lose panel not assigned!");
    }

    private void OnTryAgainClicked()
    {
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuClicked()
    {
        SceneManager.LoadScene("MainScene");
    }
}
