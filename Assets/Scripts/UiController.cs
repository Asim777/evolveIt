using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class UiController : MonoBehaviour
{
    public static UiController Instance { get; private set; }
    public Sprite playIcon; // Assign the Play icon sprite in the Inspector
    public Sprite pauseIcon; // Assign the Pause icon sprite in the Inspector
    public Button playPauseButton; // Assign the Button in the Inspector
    public GameObject entityUiPanelPrefab; // Assign the Entity UI Panel in the Inspector

    //private Image playPauseButtonBackground;
    private GameObject entityUiPanel;
    private Image playPauseButtonIcon;
    private Coroutine entityUiPanelCoroutine;

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
        playPauseButtonIcon.sprite = playIcon;
    }

    // Called when the simulation ends naturally 
    public void OnSimulationEnded()
    {   
        playPauseButtonIcon.sprite = playIcon;
    }

    public void UpdateInfoPanels(TimeSpan timeElapsed, SimualtionSpeed simulationSpeed, int entitiesCount, int foodCount)
    {
        TextMeshProUGUI simulationSpeedInfo = GameObject.FindWithTag("SimulationSpeedInfo").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeElapsedInfo = GameObject.FindWithTag("TimeElapsedInfo").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI entitiesInfo = GameObject.FindWithTag("EntitiesInfo").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI foodInfo = GameObject.FindWithTag("FoodInfo").GetComponent<TextMeshProUGUI>();

        
        string formattedTimeElapsed = timeElapsed.ToString(@"hh\:mm\:ss");
        simulationSpeedInfo.text = "Simulation Speed: " + simulationSpeed;
        timeElapsedInfo.text = "Time Elapsed: " + formattedTimeElapsed;
        entitiesInfo.text = "Entities: " + entitiesCount;
        foodInfo.text = "Food: " + foodCount;
    }

    public void ShowEntityUiPanel(EntityController entity)
    {
        // Destroy the existing entity UI panel if it exists
        if (entityUiPanel != null)
        {
            Destroy(entityUiPanel);
            StopCoroutine(entityUiPanelCoroutine);
            entityUiPanelCoroutine = null;
        }
        Vector3 position = entity.transform.position + new Vector3(0, 1, 0);
        // Instantiate the UI panel and set it as a child of the entity
        entityUiPanel = Instantiate(entityUiPanelPrefab, position, Quaternion.identity, entity.transform);

        // Start the coroutine to update the entity stats
        entityUiPanelCoroutine = StartCoroutine(UpdateEntityStats(entity));
    }

    public void HideEntityUiPanel()
    {
        Destroy(entityUiPanel);
        if (entityUiPanelCoroutine != null)
        {
            StopCoroutine(entityUiPanelCoroutine);
            entityUiPanelCoroutine = null;
        }
    }

    private IEnumerator UpdateEntityStats(EntityController entity)
    {
        while(true)  
        {
            GameObject entityStatsPanel = entityUiPanel.transform.GetChild(0).Find("Entity Stats Panel").gameObject;
            entityStatsPanel.transform.Find("Entity Name").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
            entityStatsPanel.transform.Find("Health").GetComponent<TextMeshProUGUI>().text = "Health: " + Math.Round(entity.healthMeter, 2);
            entityStatsPanel.transform.Find("Hunger").GetComponent<TextMeshProUGUI>().text = "Hunger: " + Math.Round(entity.hungerMeter, 2);
            entityStatsPanel.transform.Find("Reproduction").GetComponent<TextMeshProUGUI>().text = "Reproduction: " + Math.Round(entity.reproductionMeter, 2);

            yield return new WaitForSeconds(SimulationController.simulationStepInterval);

        }
    }
}
