using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class UiController : MonoBehaviour, IUiInteractionInterface
{
    public static UiController Instance { get; private set; }
    public Sprite playIcon; // Assign the Play icon sprite in the Inspector
    public Sprite pauseIcon; // Assign the Pause icon sprite in the Inspector
    public Button playPauseButton; // Assign the Button in the Inspector

    //private Image playPauseButtonBackground;
    private Image _playPauseButtonIcon;
    public GameObject entityStatsPanel;

    private bool _isEwpPanelOpen;
    private bool _isGipPanelOpen;
    private Coroutine _entityStatsPanelCoroutine;

    private Transform _entityListScrollView;
    private Transform _watchlistScrollView;
    private Transform _entitiesScrollViewContent;
    private Transform _watchlistScrollViewContent;

    private Transform _currentlySelectedEntityListItem;
    private Transform _currentlySelectedWatchlistListItem;
    private GameObject _entityListItemPrefab;
    private readonly Color32 _lightListItemColor = new(255, 255, 255, 171);
    private readonly Color32 _darkListItemColor = new(0, 0, 0, 171);
    private readonly Color32 _lightListItemTextColor = new(255, 255, 255, 255);
    private readonly Color32 _darkListItemTextColor = new(0, 0, 0, 255);


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // playPauseButtonBackground = playPauseButton.GetComponent<Image>();
        _playPauseButtonIcon = playPauseButton.transform.GetChild(0).GetComponent<Image>();
        _playPauseButtonIcon.sprite = playIcon;
        entityStatsPanel = GameObject.Find("ESP");
        entityStatsPanel.SetActive(false);

        _entityListScrollView = GameObject.Find("EWP_EntityListScrollView").transform;
        _watchlistScrollView = GameObject.Find("EWP_WatchlistScrollView").transform;
        _entitiesScrollViewContent = GameObject.Find("EWP_EntityListScrollView/Viewport/Content").transform;
        _watchlistScrollViewContent = GameObject.Find("EWP_WatchlistScrollView/Viewport/Content").transform;

        _entityListItemPrefab = Resources.Load<GameObject>("ListItemPrefab");
        SimulationController.Instance.Entities.CollectionChanged +=
            (_, e) => UpdateEntitiesList(e, EntityListType.EntityList);
        SimulationController.Instance.Watchlist.CollectionChanged +=
            (_, e) => UpdateEntitiesList(e, EntityListType.Watchlist);
    }

    public void OnResumePauseButtonClicked()
    {
        switch (SimulationController.Instance.simulationState)
        {
            case SimulationState.Running:
                SimulationController.Instance.PauseSimulation();
                _playPauseButtonIcon.sprite = playIcon;
                break;
            case SimulationState.Paused:
                SimulationController.Instance.ResumeSimulation();
                _playPauseButtonIcon.sprite = pauseIcon;
                break;
            case SimulationState.Stopped:
                SimulationController.Instance.StartSimulation();
                _playPauseButtonIcon.sprite = pauseIcon;
                break;
        }
    }

    public void OnStopButtonClicked()
    {
        SimulationController.Instance.StopSimulation();
        HandleSimulationStop();
    }

    public void OnEspAddToWatchlistButtonClicked()
    {
        var selectedEntity = SimulationController.Instance.GetSelectedEntity();
        if (SimulationController.Instance.Watchlist.Contains(selectedEntity)) return;
        SimulationController.Instance.Watchlist.Add(selectedEntity);
    }

    // Called when the simulation ends naturally 
    public void OnSimulationEnded()
    {
        HandleSimulationStop();
    }

    public void OnEntitiesWatchlistPanelClicked()
    {
        GameObject ewpContainer = GameObject.Find("EWP_container");
        ToggleSidebarPanel(ewpContainer, _isEwpPanelOpen);
        _isEwpPanelOpen = !_isEwpPanelOpen;
        InitiateEntitiesList(SimulationController.Instance.Entities);
    }

    public void OnGeneticInformationPanelClicked()
    {
        GameObject gipContainer = GameObject.Find("GIP_container");
        ToggleSidebarPanel(gipContainer, _isGipPanelOpen);
        _isGipPanelOpen = !_isGipPanelOpen;
    }

    private void OnListItemClicked(GameObject entity)
    {
        // Handle the click event
        Debug.Log("Clicked on entity: " + entity.gameObject.name);
        entity.GetComponent<EntityController>().SelectEntity(true);
    }

    public void OnEntitiesMultipleSelectionButtonClicked()
    {
        foreach (Transform listItem in _entitiesScrollViewContent)
        {
            var button = listItem.transform.Find("ListItem_Button");
            // Reduce button's size
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(80, 100);
        }
    }

    public void OnEwpAddToWathclistButtonClicked()
    {
        
    }

    public void OnWatchlistMultipleSelectionButtonClicked()
    {
        
    }

    public void OnDeleteFromWatchlistButtonClicked()
    {
        
    }

    public void UpdateSimulationInformationPanel(TimeSpan timeElapsed, SimualtionSpeed simulationSpeed,
        int entitiesCount, int foodCount)
    {
        var formattedTimeElapsed = timeElapsed.ToString(@"hh\:mm\:ss");
        GameObject.Find("SIP_SimulationSpeed").GetComponent<TextMeshProUGUI>().text =
            "Simulation Speed: " + simulationSpeed;
        GameObject.Find("SIP_TimeElapsed").GetComponent<TextMeshProUGUI>().text =
            "Time Elapsed: " + formattedTimeElapsed;
        GameObject.Find("SIP_EntitiesCount").GetComponent<TextMeshProUGUI>().text = "Entities: " + entitiesCount;
        GameObject.Find("SIP_FoodCount").GetComponent<TextMeshProUGUI>().text = "Food: " + foodCount;
    }

    private IEnumerator UpdateEntityStatsPanel(EntityController entity)
    {
        while (entity is not null && SimulationController.Instance.simulationState == SimulationState.Running)
        {
            GameObject.Find("ESP_Name").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
            GameObject.Find("ESP_Health").GetComponent<TextMeshProUGUI>().text =
                "Health: " + Math.Round(entity.healthMeter, 2);
            GameObject.Find("ESP_Hunger").GetComponent<TextMeshProUGUI>().text =
                "Hunger: " + Math.Round(entity.hungerMeter, 2);
            GameObject.Find("ESP_Reproduction").GetComponent<TextMeshProUGUI>().text =
                "Reproduction: " + Math.Round(entity.reproductionMeter, 2);

            yield return new WaitForSeconds(SimulationController.SimulationStepInterval);
        }
    }

    public void OnEntitySelected(EntityController entity, bool isSelectedFromUi)
    {
        entityStatsPanel.SetActive(true);

        // Stop the previous coroutine if it is running to avoid updating the wrong entity stats
        if (_entityStatsPanelCoroutine != null)
        {
            StopCoroutine(_entityStatsPanelCoroutine);
            _entityStatsPanelCoroutine = null;
        }

        // Start the coroutine to update the entity stats panel
        _entityStatsPanelCoroutine = StartCoroutine(UpdateEntityStatsPanel(entity));

        // Select the entity in the entities list
        SelectListItem(entity, EntityListType.EntityList, !isSelectedFromUi);

        // Select the entity in the watchlist list
        SelectListItem(entity, EntityListType.Watchlist, !isSelectedFromUi);
    }

    /// <summary>
    /// Hides the entity stats panel and stops the coroutine that updates it.
    /// </summary>
    public void HideEntityStatsPanel()
    {
        entityStatsPanel.SetActive(false);

        if (_entityStatsPanelCoroutine != null)
        {
            StopCoroutine(_entityStatsPanelCoroutine);
            _entityStatsPanelCoroutine = null;
        }
    }

    private void InitiateEntitiesList(ObservableCollection<GameObject> entities)
    {
        Debug.Log("UpdateEntitiesWatchlistPanel called. Number of entities: " + entities.Count);

        // Populate the list with new items
        foreach (var entity in entities)
        {
            AddEntityToList(entity, _entitiesScrollViewContent);
        }
    }

    private void UpdateEntitiesList(NotifyCollectionChangedEventArgs e, EntityListType listType)
    {
        var scrollViewContent = listType == EntityListType.EntityList
            ? _entitiesScrollViewContent
            : _watchlistScrollViewContent;
        
        if (e.NewItems != null)
        {
            foreach (GameObject entity in e.NewItems)
            {
                AddEntityToList(entity, scrollViewContent);
            }
        }

        if (e.OldItems != null)
        {
            foreach (GameObject entity in e.OldItems)
            {
                // Remove the entity from the entities list
                RemoveListItem(entity, scrollViewContent);
            }
        }
    }

    private void AddEntityToList(GameObject entity, Transform scrollViewContent)
    {
        var listItem = Instantiate(_entityListItemPrefab, scrollViewContent);
        listItem.name = entity.gameObject.name;
        var button = listItem.transform.Find("ListItem_Button");
        button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
        AddListItemClickListener(button, entity);
        
        
        if (scrollViewContent == _watchlistScrollViewContent)
        {
            // Select entity added to the Watchlist
            SelectListItem(entity.GetComponent<EntityController>(), EntityListType.Watchlist, true);
        }
    }

    private void AddListItemClickListener(Transform button, GameObject entity)
    {
        Button listItemButton = button.GetComponent<Button>();
        listItemButton.onClick.AddListener(() => OnListItemClicked(entity));
    }

    private void RemoveListItem(GameObject entity, Transform scrollView)
    {
        foreach (Transform child in scrollView)
        {
            if (child.name == entity.name)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SelectListItem(EntityController entity, EntityListType listType, bool shouldScrollToItem)
    {
        var scrollViewContent = listType == EntityListType.EntityList
            ? _entitiesScrollViewContent
            : _watchlistScrollViewContent;
        var scrollView = listType == EntityListType.EntityList ? _entityListScrollView : _watchlistScrollView;
        var currentlySelectedItem = listType == EntityListType.EntityList
            ? _currentlySelectedEntityListItem
            : _currentlySelectedWatchlistListItem;

        // Find the item in the list that matches the entity game object
        foreach (Transform listItem in scrollViewContent)
        {
            if (listItem.name == entity.name)
            {
                if (currentlySelectedItem != null)
                {
                    ChangeListItemUi(currentlySelectedItem, false);
                }

                // Set the color of the item to the selected color
                ChangeListItemUi(listItem, true);

                // Scroll to the selected item
                if (shouldScrollToItem) ScrollToSelectedItem(scrollView, listItem);

                if (listType == EntityListType.EntityList)
                {
                    _currentlySelectedEntityListItem = listItem;
                }
                else
                {
                    _currentlySelectedWatchlistListItem = listItem;
                }

                break;
            }
        }

        // If selected item doesn't exist in watchlist, unselect previously selected watchlist item
        UnselectSelectedWatchlistItem(entity, listType);
    }

    private void UnselectSelectedWatchlistItem(EntityController entity, EntityListType listType)
    {
        var watchlist = SimulationController.Instance.Watchlist;
        if (listType == EntityListType.EntityList && _currentlySelectedWatchlistListItem != null)
        {
            var watchListContainsSelectedEntity = watchlist.Contains(entity.gameObject);
            if (!watchListContainsSelectedEntity)
            {
                ChangeListItemUi(_currentlySelectedWatchlistListItem, false);
                _currentlySelectedWatchlistListItem = null;
            }
        }
    }

    private static void ScrollToSelectedItem(Transform scrollView, Transform listItem)
    {
        Canvas.ForceUpdateCanvases();
        var itemRect = listItem.GetComponent<RectTransform>();
        var scrollRect = scrollView.GetComponent<ScrollRect>();
        var scrollValue = 1 + itemRect.anchoredPosition.y / scrollRect.content.rect.height;
        scrollRect.verticalNormalizedPosition = scrollValue;
    }

    private void ChangeListItemUi(Transform listItem, bool isSelected)
    {
        var backgroundColor = isSelected ? _lightListItemColor : _darkListItemColor;
        var textColor = isSelected ? _darkListItemTextColor : _lightListItemTextColor;

        var button = listItem.transform.Find("ListItem_Button");
        var buttonImage = button.GetComponent<Image>();
        var text = button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>();
        buttonImage.color = backgroundColor;
        text.color = textColor;
        text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
    }

    private void HandleSimulationStop()
    {
        StopAllCoroutines();
        _playPauseButtonIcon.sprite = playIcon;
        _entityStatsPanelCoroutine = null;
        foreach (Transform child in _entitiesScrollViewContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in _watchlistScrollViewContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void ToggleSidebarPanel(GameObject panelContainer, bool isPanelOpen)
    {
        var panelRectTransform = panelContainer.GetComponent<RectTransform>();
        if (isPanelOpen)
        {
            StartCoroutine(SlidePanel(panelRectTransform, panelRectTransform.anchoredPosition,
                new Vector2(485.48f, panelRectTransform.anchoredPosition.y)));
        }
        else
        {
            StartCoroutine(SlidePanel(panelRectTransform, panelRectTransform.anchoredPosition,
                new Vector2(0, panelRectTransform.anchoredPosition.y)));
        }
    }

    private static IEnumerator SlidePanel(RectTransform panel, Vector2 start, Vector2 end)
    {
        float elapsedTime = 0;
        const float duration = 0.1f;

        while (elapsedTime < duration)
        {
            panel.anchoredPosition = Vector2.Lerp(start, end, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panel.anchoredPosition = end;
    }

    enum EntityListType
    {
        EntityList,
        Watchlist
    }
}