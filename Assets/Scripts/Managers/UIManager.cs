using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private ItemFactory _itemFactory;
    [SerializeField] private ConfettiManager _confettiManager;
    
    [Header("Panels")]
    [SerializeField] private GameObject _gameHUD;
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _losePanel;

    [Header("HUD Elements")]
    [SerializeField] private TMP_Text _MovesNumber;
    [SerializeField] private RectTransform _goalsContainer;
    [SerializeField] private GoalItemView _goalItemPrefab;

    [Header("Win UI")]
    [SerializeField] private Button _winMainMenuButton;
    
    [Header("Lose UI")]
    [SerializeField] private Button _loseTryAgainButton;
    [SerializeField] private Button _loseMainMenuButton;
    [SerializeField] private Button _loseCloseButton;

    private Dictionary<string, GoalItemView> _goalViews = new Dictionary<string, GoalItemView>();

    private void Awake()
    {
        // Wire up buttons
        if (_winMainMenuButton) _winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseTryAgainButton) _loseTryAgainButton.onClick.AddListener(OnTryAgainClicked);
        if (_loseMainMenuButton) _loseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseCloseButton) _loseCloseButton.onClick.AddListener(OnMainMenuClicked);

        // Ensure correct start state
        ShowHUD();
    }

    private void OnEnable()
    {
        GameManager.OnLevelLoaded += OnLevelLoaded;
        GameManager.OnMovesUpdated += OnMovesUpdated;
        GameManager.OnGoalsUpdated += OnGoalsUpdated;
        GameManager.OnLevelWon += OnLevelWin;
        GameManager.OnLevelLost += OnLevelLose;
    }

    private void OnDisable()
    {
        GameManager.OnLevelLoaded -= OnLevelLoaded;
        GameManager.OnMovesUpdated -= OnMovesUpdated;
        GameManager.OnGoalsUpdated -= OnGoalsUpdated;
        GameManager.OnLevelWon -= OnLevelWin;
        GameManager.OnLevelLost -= OnLevelLose;
    }

    private void OnLevelLoaded(LevelData level, Dictionary<string, int> goals)
    {
        ShowHUD();
        BuildGoals(goals);
        
        // Reset Text
        if (_MovesNumber) _MovesNumber.text = level.move_count.ToString();
        
        // Stop confetti if running
        if (_confettiManager) _confettiManager.StopConfetti();
    }

    private void BuildGoals(Dictionary<string, int> goals)
    {
        if (_goalsContainer == null || _goalItemPrefab == null)
        {
            Debug.LogError("[UIManager] GoalsContainer or GoalItemPrefab missing!");
            return;
        }
        // Clear existing
        foreach (Transform child in _goalsContainer)
        {
            Destroy(child.gameObject);
        }
        _goalViews.Clear();

        if (goals == null) return;

        foreach (var kvp in goals)
        {
            string id = kvp.Key;
            int count = kvp.Value;

            GoalItemView view = Instantiate(_goalItemPrefab, _goalsContainer);
            Sprite icon = _itemFactory ? _itemFactory.GetSprite(id) : null;
            
            view.SetGoal(icon, count);
            _goalViews[id] = view;
        }
    }

    private void OnMovesUpdated(int moves)
    {
        if (_MovesNumber) _MovesNumber.text = moves.ToString();
    }

    private void OnGoalsUpdated(Dictionary<string, int> currentGoals)
    {
        foreach (var kvp in currentGoals)
        {
            if (_goalViews.ContainsKey(kvp.Key))
            {
                _goalViews[kvp.Key].UpdateCount(kvp.Value);
            }
        }
    }

    private void OnLevelWin()
    {
        if (_gameHUD) _gameHUD.SetActive(false);
        if (_winPanel) _winPanel.SetActive(true);
        
        // Play Confetti
        if (_confettiManager) _confettiManager.PlayConfetti();
        
        StartCoroutine(AutoLoadMainScene(3.0f));
    }

    private System.Collections.IEnumerator AutoLoadMainScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("MainScene");
    }

    private void OnLevelLose()
    {
        if (_gameHUD) _gameHUD.SetActive(false);
        if (_losePanel) _losePanel.SetActive(true);
    }

    private void ShowHUD()
    {
        if (_gameHUD) _gameHUD.SetActive(true);
        if (_winPanel) _winPanel.SetActive(false);
        if (_losePanel) _losePanel.SetActive(false);
    }

    private void OnTryAgainClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuClicked()
    {
        SceneManager.LoadScene("MainScene");
    }
}

