using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour
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
    private GameObject _entityListItemPrefab;

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

        _entityListScrollView = GameObject.Find("EWP_EntityListScrollView/Viewport/Content").transform;
        _watchlistScrollView = GameObject.Find("EWP_WatchlistScrollView/Viewport/Content").transform;

        _entityListItemPrefab = Resources.Load<GameObject>("ListItemPrefab");
        SimulationController.Instance.Entities.CollectionChanged += (_, e) => UpdateEntitiesList(e);
        SimulationController.Instance.Watchlist.CollectionChanged += (_, e) => UpdateWatchlist(e);
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
    
    public void OnStopButtonClicked() {
        SimulationController.Instance.StopSimulation();
        HandleSimulationStop();
    }

    public void OnAddToWatchlistButtonClicked()
    {
        SimulationController.Instance.Watchlist.Add(SimulationController.Instance.GetSelectedEntity());
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

    public void UpdateSimulationInformationPanel(TimeSpan timeElapsed, SimualtionSpeed simulationSpeed, int entitiesCount, int foodCount)
    {   
        string formattedTimeElapsed = timeElapsed.ToString(@"hh\:mm\:ss");
        GameObject.Find("SIP_SimulationSpeed").GetComponent<TextMeshProUGUI>().text = "Simulation Speed: " + simulationSpeed;
        GameObject.Find("SIP_TimeElapsed").GetComponent<TextMeshProUGUI>().text = "Time Elapsed: " + formattedTimeElapsed;
        GameObject.Find("SIP_EntitiesCount").GetComponent<TextMeshProUGUI>().text = "Entities: " + entitiesCount;
        GameObject.Find("SIP_FoodCount").GetComponent<TextMeshProUGUI>().text = "Food: " + foodCount;
    }

    public void ShowEntityStatsPanel(EntityController entity)
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
    }

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
        foreach (GameObject entity in entities)
        {
            AddEntityToEntityList(entity);
        }
    }

    private void UpdateEntitiesList(NotifyCollectionChangedEventArgs e) 
    {
        if (e.NewItems != null)
        {
            foreach (GameObject entity in e.NewItems)
            {
                AddEntityToEntityList(entity);
            }
        }
        
        if (e.OldItems != null)
        {
            foreach (GameObject entity in e.OldItems)
            {
                // Remove the entity from the entities list
                RemoveListItem(entity, _entityListScrollView);
            }
        }
    }

    private void UpdateWatchlist(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GameObject entity in e.NewItems)
            {
                AddEntityToWatchlist(entity);
            }
        }
        
        if (e.OldItems != null)
        {
            foreach (GameObject entity in e.OldItems)
            {
                // Remove the entity from the watchlist
                RemoveListItem(entity, _watchlistScrollView);
            }
        }
    }

    private void AddEntityToEntityList(GameObject entity)
    {
        GameObject listItem = Instantiate(_entityListItemPrefab, _entityListScrollView);
        listItem.name = entity.gameObject.name;
        Transform button = listItem.transform.Find("ListItem_Button");
        button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
        AddListItemClickListener(button, entity);
    }

    private void AddEntityToWatchlist(GameObject entity)
    {
        GameObject listItem = Instantiate(_entityListItemPrefab, _watchlistScrollView);
        listItem.name = entity.gameObject.name;
        Transform button = listItem.transform.Find("ListItem_Button");
        button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
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

    public void OnListItemClicked(GameObject entity)
    {
        // Handle the click event
        Debug.Log("Clicked on entity: " + entity.gameObject.name);
        entity.GetComponent<EntityController>().SelectEntity();
    }

    private void HandleSimulationStop()
    {
        _playPauseButtonIcon.sprite = playIcon;

        StopAllCoroutines();
        _entityStatsPanelCoroutine = null;
    }

    private void ToggleSidebarPanel(GameObject panelContainer, bool isPanelOpen)
    {
        RectTransform panelRectTransform = panelContainer.GetComponent<RectTransform>();
        if (isPanelOpen)
        {
            StartCoroutine(SlidePanel(panelRectTransform, panelRectTransform.anchoredPosition, new Vector2(485.48f, panelRectTransform.anchoredPosition.y)));
        }
        else
        {
            StartCoroutine(SlidePanel(panelRectTransform, panelRectTransform.anchoredPosition, new Vector2(0, panelRectTransform.anchoredPosition.y)));
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

    private IEnumerator UpdateEntityStatsPanel(EntityController entity)
    {
        while(entity is not null && SimulationController.Instance.simulationState == SimulationState.Running)  
        {
            GameObject.Find("ESP_Name").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
            GameObject.Find("ESP_Health").GetComponent<TextMeshProUGUI>().text = "Health: " + Math.Round(entity.healthMeter, 2);
            GameObject.Find("ESP_Hunger").GetComponent<TextMeshProUGUI>().text = "Hunger: " + Math.Round(entity.hungerMeter, 2);
            GameObject.Find("ESP_Reproduction").GetComponent<TextMeshProUGUI>().text = "Reproduction: " + Math.Round(entity.reproductionMeter, 2);

            yield return new WaitForSeconds(SimulationController.SimulationStepInterval);
        }
    }
}
