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

    public void OnLevelWin()
    {
        if (_gameHUD) _gameHUD.SetActive(false);
        if (_winPanel) _winPanel.SetActive(true);
        if (_celebrationParticles) _celebrationParticles.Play();
    }

    public void OnLevelLose()
    {
        if (_gameHUD) _gameHUD.SetActive(false);
        if (_losePanel) _losePanel.SetActive(true);
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
