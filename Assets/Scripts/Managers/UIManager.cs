using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
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
    private IGameplayEventBus _eventBus;
    private ISceneLoadService _sceneLoadService;

    private void Awake()
    {
        EnsureServices();

        if (_winMainMenuButton) _winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseTryAgainButton) _loseTryAgainButton.onClick.AddListener(OnTryAgainClicked);
        if (_loseMainMenuButton) _loseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_loseCloseButton) _loseCloseButton.onClick.AddListener(OnMainMenuClicked);

        EnsureGoalLayoutComponents();
        ShowHUD();
    }

    private void OnEnable()
    {
        EnsureServices();

        _eventBus.LevelLoaded += OnLevelLoaded;
        _eventBus.MovesUpdated += OnMovesUpdated;
        _eventBus.GoalsUpdated += OnGoalsUpdated;
        _eventBus.LevelWon += OnLevelWin;
        _eventBus.LevelLost += OnLevelLose;
    }

    private void OnDisable()
    {
        if (_eventBus == null) return;

        _eventBus.LevelLoaded -= OnLevelLoaded;
        _eventBus.MovesUpdated -= OnMovesUpdated;
        _eventBus.GoalsUpdated -= OnGoalsUpdated;
        _eventBus.LevelWon -= OnLevelWin;
        _eventBus.LevelLost -= OnLevelLose;
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

        StartCoroutine(UpdateLayoutRoutine());
    }

    private System.Collections.IEnumerator UpdateLayoutRoutine()
    {
        if (_goalsContainer == null) yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_goalsContainer);
        yield return new WaitForEndOfFrame();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_goalsContainer);
    }

    private void EnsureGoalLayoutComponents()
    {
        if (_goalsContainer == null) return;

        if (_goalsContainer.GetComponent<HorizontalOrVerticalLayoutGroup>() == null &&
            _goalsContainer.GetComponent<GridLayoutGroup>() == null)
        {
            HorizontalLayoutGroup layout = _goalsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
        }

        ContentSizeFitter fitter = _goalsContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = _goalsContainer.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        LoadSceneSafe(SceneNames.Main);
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
        EnsureServices();
        LoadSceneSafe(_sceneLoadService.ActiveSceneName);
    }

    private void OnMainMenuClicked()
    {
        LoadSceneSafe(SceneNames.Main);
    }

    private void LoadSceneSafe(string sceneName)
    {
        EnsureServices();
        _sceneLoadService.LoadScene(sceneName);
    }

    private void EnsureServices()
    {
        if (_eventBus == null) _eventBus = new StaticGameplayEventBus();
        if (_sceneLoadService == null) _sceneLoadService = new UnitySceneLoadService();
    }
}
}
