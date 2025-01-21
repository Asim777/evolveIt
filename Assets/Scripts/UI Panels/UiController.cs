using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace UI_Panels
{
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
            _playPauseButtonIcon = playPauseButton.transform.GetChild(0).GetComponent<Image>();
            _playPauseButtonIcon.sprite = playIcon;

            entityStatsPanel = GameObject.Find("ESP");
            entityStatsPanel.SetActive(false);
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
            // If the selected entity is already in the watchlist, don't add it again
            if (SimulationController.Instance.Watchlist.Contains(selectedEntity)) return;
            SimulationController.Instance.Watchlist.Add(selectedEntity);
            EntitiesWatchlistPanelController.Instance.OnEspAddToWatchlistButtonClicked(selectedEntity);
        }

        // Called when the simulation ends naturally 
        public void OnSimulationEnded()
        {
            HandleSimulationStop();
        }

        public void OnEntitiesWatchlistPanelClicked()
        {
            var ewpContainer = GameObject.Find("EWP_container");
            ToggleSidebarPanel(ewpContainer, _isEwpPanelOpen);
            _isEwpPanelOpen = !_isEwpPanelOpen;
            if (_isEwpPanelOpen) EntitiesWatchlistPanelController.Instance.InitiateEntitiesList();
        }

        public void OnGeneticInformationPanelClicked()
        {
            var gipContainer = GameObject.Find("GIP_container");
            ToggleSidebarPanel(gipContainer, _isGipPanelOpen);
            _isGipPanelOpen = !_isGipPanelOpen;
        }

        public void OnEntitiesMultipleSelectionButtonClicked()
        {
            EntitiesWatchlistPanelController.Instance.OnEntitiesMultipleSelectionButtonClicked();
        }

        public void OnEwpAddToWathclistButtonClicked()
        {
            EntitiesWatchlistPanelController.Instance.OnEwpAddToWathclistButtonClicked();
        }

        public void OnWatchlistMultipleSelectionButtonClicked()
        {
            EntitiesWatchlistPanelController.Instance.OnWatchlistMultipleSelectionButtonClicked();
        }

        public void OnDeleteFromWatchlistButtonClicked()
        {
            EntitiesWatchlistPanelController.Instance.OnDeleteFromWatchlistButtonClicked();
        }

        public void OnEntityDeselected()
        {
            HideEntityStatsPanel();
            EntitiesWatchlistPanelController.Instance.OnEntityDeselected();
        }
    
        /// <summary>
        /// Hides the entity stats panel and stops the coroutine that updates it.
        /// </summary>
        private void HideEntityStatsPanel()
        {
            entityStatsPanel.SetActive(false);

            if (_entityStatsPanelCoroutine != null)
            {
                StopCoroutine(_entityStatsPanelCoroutine);
                _entityStatsPanelCoroutine = null;
            }
        }

        public void UpdateSimulationInformationPanel(TimeSpan timeElapsed, SimualationSpeed simulationSpeed,
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

            EntitiesWatchlistPanelController.Instance.OnEntitySelected(entity, isSelectedFromUi);
        }

        private IEnumerator UpdateEntityStatsPanel(EntityController entity)
        {
            while (entity != null && SimulationController.Instance.simulationState == SimulationState.Running)
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

        private void HandleSimulationStop()
        {
            StopAllCoroutines();
            _playPauseButtonIcon.sprite = playIcon;
            _entityStatsPanelCoroutine = null;
            
            EntitiesWatchlistPanelController.Instance.OnSimulationStopped();
        }

        private void ToggleSidebarPanel(GameObject panelContainer, bool isPanelOpen)
        {
            var panelRectTransform = panelContainer.GetComponent<RectTransform>();
            if (isPanelOpen)
            {
                StartCoroutine(
                    SlidePanel(
                        panelRectTransform,
                        panelRectTransform.anchoredPosition,
                        new Vector2(485.48f, panelRectTransform.anchoredPosition.y)
                    )
                );
            }
            else
            {
                StartCoroutine(
                    SlidePanel(
                        panelRectTransform,
                        panelRectTransform.anchoredPosition,
                        new Vector2(0, panelRectTransform.anchoredPosition.y)
                    )
                );
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
    }
}