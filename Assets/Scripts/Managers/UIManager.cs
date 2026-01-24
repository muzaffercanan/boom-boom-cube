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

        if (_winMainMenuButton) _winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseTryAgainButton) _loseTryAgainButton.onClick.AddListener(OnTryAgainClicked);
        if (_loseMainMenuButton) _loseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseCloseButton) _loseCloseButton.onClick.AddListener(OnMainMenuClicked);


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
        
        if (_MovesNumber) _MovesNumber.text = level.move_count.ToString();
        
        if (_confettiManager) _confettiManager.StopConfetti();
    }

    private void BuildGoals(Dictionary<string, int> goals)
    {
        if (_goalsContainer == null || _goalItemPrefab == null)
        {
            Debug.LogError("[UIManager] GoalsContainer or GoalItemPrefab missing!");
            return;
        }

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

        StartCoroutine(UpdateLayoutRoutine(goals.Count));
    }

    private System.Collections.IEnumerator UpdateLayoutRoutine(int itemCount)
    {
        if (_goalsContainer == null) yield break;

        // Once Unity'nin kendi layout hesaplamalarini yapmasini bekleyelim (Layout Rebuild)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_goalsContainer);
        yield return new WaitForEndOfFrame();

        GridLayoutGroup grid = _goalsContainer.GetComponent<GridLayoutGroup>();
        ContentSizeFitter fitter = _goalsContainer.GetComponent<ContentSizeFitter>();
        
        if (grid != null && fitter != null)
        {
            float maxAvailableWidth = 500f; 
            if (_goalsContainer.parent is RectTransform parentRect)
            {
                maxAvailableWidth = parentRect.rect.width; 
            }

            float itemWidth = grid.cellSize.x + grid.spacing.x;
            float totalWidthNeeded = itemCount * itemWidth;
            totalWidthNeeded += grid.padding.left + grid.padding.right;

            if (totalWidthNeeded <= maxAvailableWidth)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                _goalsContainer.sizeDelta = new Vector2(maxAvailableWidth, _goalsContainer.sizeDelta.y);
            }
        }
        
        // Layout degisikliklerinin oturmasi icin tekrar rebuild yapabiliriz veya direkt pozisyonu verebiliriz.
        LayoutRebuilder.ForceRebuildLayoutImmediate(_goalsContainer);
        yield return new WaitForEndOfFrame(); // Bir frame daha bekleyip pozisyonu cakiyoruz.

        // -- Pozisyon Ayarlamasi --
        Vector2 targetPos = _goalsContainer.anchoredPosition;

        if (itemCount == 1)
        {
            targetPos = new Vector2(-371f, 654f);
        }
        else if (itemCount == 2)
        {
            targetPos = new Vector2(-435f, 642f);
        }
        else
        {
            targetPos = new Vector2(-433f, 708f);
        }

        _goalsContainer.anchoredPosition = targetPos;
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
        

        if (_confettiManager) _confettiManager.PlayConfetti();
        
        StartCoroutine(AutoLoadMainScene(3.0f));
    }

    private System.Collections.IEnumerator AutoLoadMainScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadSceneSafe("MainScene");
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
        LoadSceneSafe(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuClicked()
    {
        LoadSceneSafe("MainScene");
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}

