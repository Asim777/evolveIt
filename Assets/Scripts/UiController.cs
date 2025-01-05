using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UiController : MonoBehaviour
{
    public static UiController Instance { get; private set; }
    public Sprite playIcon; // Assign the Play icon sprite in the Inspector
    public Sprite pauseIcon; // Assign the Pause icon sprite in the Inspector
    public Button playPauseButton; // Assign the Button in the Inspector

    //private Image playPauseButtonBackground;
    private Image playPauseButtonIcon;
    public GameObject entityStatsPanel; 

    private bool isEwpPanelOpen = false;
    private bool isGipPanelOpen = false;
    private Coroutine entityStatsPanelCoroutine;
    //private Coroutine updateEntityListCoroutine;
    //private Coroutine updateGeneListCoroutine;

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
        playPauseButtonIcon = playPauseButton.transform.GetChild(0).GetComponent<Image>();
        playPauseButtonIcon.sprite = playIcon;
        entityStatsPanel = GameObject.Find("ESP");
        entityStatsPanel.SetActive(false);
    }

    public void OnResumePauseButtonClicked()
    {
        switch (SimulationController.Instance.simulationState)
        {
            case SimulationState.Running:
                SimulationController.Instance.PauseSimulation();
                playPauseButtonIcon.sprite = playIcon;
                break;
            case SimulationState.Paused:
                SimulationController.Instance.ResumeSimulation();
                playPauseButtonIcon.sprite = pauseIcon;
                break;
            case SimulationState.Stopped:
                SimulationController.Instance.StartSimulation();
                playPauseButtonIcon.sprite = pauseIcon;
                break;
        }
    }
    
    public void OnStopButtonClicked() {
        SimulationController.Instance.StopSimulation();
        HandleSimulationStop();
    }

    // Called when the simulation ends naturally 
    public void OnSimulationEnded()
    {   
        HandleSimulationStop();
    }

    public void OnEntitiesWatchlistPanelClicked()
    {
        GameObject ewpContainer = GameObject.Find("EWP_container");
        ToggleSidebarPanel(ewpContainer, isEwpPanelOpen);
        isEwpPanelOpen = !isEwpPanelOpen;
    }

    public void OnGeneticInformationPanelClicked()
    {
        GameObject gipContainer = GameObject.Find("GIP_container");
        ToggleSidebarPanel(gipContainer, isGipPanelOpen);
        isGipPanelOpen = !isGipPanelOpen;
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
        if (entityStatsPanelCoroutine != null)
        {
            StopCoroutine(entityStatsPanelCoroutine);
            entityStatsPanelCoroutine = null;
        }

        // Start the coroutine to update the entity stats panel
        entityStatsPanelCoroutine = StartCoroutine(UpdateEntityStatsPanel(entity));
    }

    public void HideEntityStatsPanel()
    {
        entityStatsPanel.SetActive(false);

        if (entityStatsPanelCoroutine != null)
        {
            StopCoroutine(entityStatsPanelCoroutine);
            entityStatsPanelCoroutine = null;
        }
    }

    public void UpdateEntitiesWatchlistPanel(List<GameObject> entities) 
    {
        Transform entityListScrollView = GameObject.Find("EWP_EntityListScrollView/Viewport/Content").transform;
        GameObject entityListItemPrefab = Resources.Load<GameObject>("ListItemPrefab");
        
        // Clear existing list items
        foreach (Transform child in entityListScrollView)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("UpdateEntitiesWatchlistPanel called. Number of entities: " + entities.Count);
        
        // Populate the list with new items
        foreach (GameObject entity in entities)
        {
            GameObject listItem = Instantiate(entityListItemPrefab, entityListScrollView);
            listItem.transform.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;

            // Set up the EventTrigger for click events
            EventTrigger trigger = listItem.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = listItem.AddComponent<EventTrigger>();
            }
            EventTrigger.Entry entry = new();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) => { 
                entity.GetComponent<EntityController>().SelectEntity();    
                Debug.Log("Entity clicked on list " + entity.name);
            });
            trigger.triggers.Add(entry);
            Debug.Log("Trigger added to " + entity.name);
        }
    }

    private void HandleSimulationStop()
    {
        playPauseButtonIcon.sprite = playIcon;

        StopAllCoroutines();
        entityStatsPanelCoroutine = null;
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

    private IEnumerator SlidePanel(RectTransform panel, Vector2 start, Vector2 end)
    {
        float elapsedTime = 0;
        float duration = 0.1f;

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
        while(true)  
        {
            GameObject.Find("ESP_Name").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
            GameObject.Find("ESP_Health").GetComponent<TextMeshProUGUI>().text = "Health: " + Math.Round(entity.healthMeter, 2);
            GameObject.Find("ESP_Hunger").GetComponent<TextMeshProUGUI>().text = "Hunger: " + Math.Round(entity.hungerMeter, 2);
            GameObject.Find("ESP_Reproduction").GetComponent<TextMeshProUGUI>().text = "Reproduction: " + Math.Round(entity.reproductionMeter, 2);

            yield return new WaitForSeconds(SimulationController.simulationStepInterval);
        }
    }
}
