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

    //private Image playPauseButtonBackground;
    private Image playPauseButtonIcon;
    private Coroutine entityStatsPanelCoroutine;
    public GameObject entityStatsPanel; 

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
        playPauseButtonIcon.sprite = playIcon;
    }

    // Called when the simulation ends naturally 
    public void OnSimulationEnded()
    {   
        playPauseButtonIcon.sprite = playIcon;
    }

    public void UpdateSimulationInformationPanel(TimeSpan timeElapsed, SimualtionSpeed simulationSpeed, int entitiesCount, int foodCount)
    {   
        string formattedTimeElapsed = timeElapsed.ToString(@"hh\:mm\:ss");
        GameObject.Find("SIP_SimulationSpeed").GetComponent<TextMeshProUGUI>().text = "Simulation Speed: " + simulationSpeed;
        GameObject.Find("SIP_TimeElapsed").GetComponent<TextMeshProUGUI>().text = "Time Elapsed: " + formattedTimeElapsed;
        GameObject.Find("SIP_EntitiesCount").GetComponent<TextMeshProUGUI>().text = "Entities: " + entitiesCount;
        GameObject.Find("SIP_FoodCount").GetComponent<TextMeshProUGUI>().text = "Food: " + foodCount;
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
}
