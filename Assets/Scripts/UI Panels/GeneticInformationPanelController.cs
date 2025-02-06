using System;
using System.Collections.Generic;
using System.Linq;
using Data.Gene;
using Data.Neuron;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Panels
{
    public class GeneticInformationPanelController : MonoBehaviour, IGeneticInformationPanelController
    {
        public static GeneticInformationPanelController Instance { get; private set; }

        private Transform _genesScrollViewContent;
        private Transform _neuronsScrollViewContent;
        private Transform _currentlySelectedGeneListItem;
        private Transform _currentlySelectedNeuronListItem;
        private Transform _geneSelectionResetButton;
        private Transform _neuronSelectionResetButton;
        private GameObject _entityListItemPrefab;

        private readonly Color32 _lightListItemColor = new(255, 255, 255, 171);
        private readonly Color32 _darkListItemColor = new(0, 0, 0, 171);
        private readonly Color32 _lightListItemTextColor = new(255, 255, 255, 255);
        private readonly Color32 _darkListItemTextColor = new(0, 0, 0, 255);

        private Gene _selectedGene;
        private Neuron _selectedNeuron;

        private void Awake()
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
            _genesScrollViewContent = GameObject.Find("GIP_GenesScrollView/Viewport/Content").transform;
            _neuronsScrollViewContent = GameObject.Find("GIP_NeuronsScrollView/Viewport/Content").transform;

            _entityListItemPrefab = Resources.Load<GameObject>("ListItemPrefab");
        }

        public void OnEntitySelected()
        {
            UpdateGeneList();
            UpdateNeuralMap(SimulationController.Instance.GetSelectedEntity()?.GetComponent<EntityController>()
                ?.Genome);
        }

        public void UpdateGeneList()
        {
            ClearScrollView(_genesScrollViewContent);

            var selectedEntityController =
                SimulationController.Instance.GetSelectedEntity()?.GetComponent<EntityController>();
            if (selectedEntityController == null) return;

            foreach (var gene in selectedEntityController.Genome)
            {
                var listItem = Instantiate(_entityListItemPrefab, _genesScrollViewContent);
                listItem.name = gene.Name;
                var button = listItem.transform.Find("ListItem_Button");

                button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = gene.Name;
                AddGeneListItemClickListener(button, listItem.transform, gene);
            }
        }

        public void OnSimulationStopped()
        {
            ClearScrollView(_genesScrollViewContent);
            ClearScrollView(_neuronsScrollViewContent);
        }

        public void OnGenesResetButtonClick()
        {
            ChangeListItemUi(_currentlySelectedGeneListItem, false);
            _selectedGene = null;
            _currentlySelectedGeneListItem = null;
            ClearScrollView(_neuronsScrollViewContent);
        }

        public void OnNeuronsResetButtonClick()
        {
            ChangeListItemUi(_currentlySelectedNeuronListItem, false);
            _selectedNeuron = null;
            _currentlySelectedNeuronListItem = null;
        }

        private void UpdateNeuralMap(List<Gene> genome)
        {
            if (genome != null)
            {
                var neurons = genome.SelectMany(gene => gene.GetAllNeurons()).Distinct().ToList();
                var neuralMapContainer = GameObject.Find("GIP_NeuronsContainer");

                // Clear all previous Neurons
                foreach (Transform child in neuralMapContainer.transform)
                {
                    Destroy(child.gameObject);
                }

                var sensorNeurons = neurons.OfType<SensorNeuron>().ToList();
                var innerNeurons = neurons.OfType<InnerNeuron>().ToList();
                var sinkNeurons = neurons.OfType<SinkNeuron>().ToList();
                var neuronPrefab = Resources.Load<GameObject>("NeuronPrefab");

                CreateNeuronCircle(neuronPrefab, neuralMapContainer.transform, sensorNeurons.OfType<Neuron>().ToList(),
                    160);
                CreateNeuronCircle(neuronPrefab, neuralMapContainer.transform, innerNeurons.OfType<Neuron>().ToList(),
                    85);
                CreateNeuronCircle(neuronPrefab, neuralMapContainer.transform, sinkNeurons.OfType<Neuron>().ToList(),
                    30);
            }
        }

        private static void CreateNeuronCircle(GameObject neuronPrefab, Transform neuralMapContainer, List<Neuron> neurons,
            int radius)
        {
            // We arrange all Neurons around the center of NeuralMap Container
            var point = neuralMapContainer.transform.position;
            var center = new Vector2(point.x, point.y);

            for (var i = 0; i < neurons.Count; i++)
            {
                // Distance around the circle
                var radian = 2 * MathF.PI / neurons.Count * i;

                // Get the vector direction
                var vertical = MathF.Sin(radian);
                var horizontal = MathF.Cos(radian);

                var spawnDir = new Vector2(horizontal, vertical);

                // Get the spawn position
                var spawnPos = center + spawnDir * radius; // Radius is just the distance away from the point

                // Spawning Neuron object
                Instantiate(neuronPrefab, spawnPos, Quaternion.identity, neuralMapContainer);
                var neuronId = neuronPrefab.transform.Find("Neuron_Id").GetComponent<TextMeshProUGUI>();
                neuronId.text = neurons[i].Id;
            }
        }

        private void AddGeneListItemClickListener(Transform button, Transform listItem, Gene gene)
        {
            var listItemButton = button.GetComponent<Button>();
            listItemButton.onClick.AddListener(() => OnGeneClicked(gene, listItem));
        }

        private void OnGeneClicked(Gene gene, Transform listItem)
        {
            if (_currentlySelectedGeneListItem != null)
            {
                ChangeListItemUi(_currentlySelectedGeneListItem, false);
            }

            ChangeListItemUi(listItem, true);
            _currentlySelectedGeneListItem = listItem;
            _selectedGene = gene;

            InitiateNeuronsList();
        }

        private void InitiateNeuronsList()
        {
            ClearScrollView(_neuronsScrollViewContent);

            var selectedEntityController =
                SimulationController.Instance.GetSelectedEntity()?.GetComponent<EntityController>();
            if (selectedEntityController == null) return;

            foreach (var neuron in _selectedGene.GetAllNeurons())
            {
                var listItem = Instantiate(_entityListItemPrefab, _neuronsScrollViewContent);
                listItem.name = neuron.Id;
                var button = listItem.transform.Find("ListItem_Button");

                button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = neuron.Id;
                AddNeuronListItemClickListener(button, listItem.transform, neuron);
            }
        }

        private void AddNeuronListItemClickListener(Transform button, Transform listItem, Neuron neuron)
        {
            var listItemButton = button.GetComponent<Button>();
            listItemButton.onClick.AddListener(() => OnNeuronClicked(neuron, listItem));
        }

        private void OnNeuronClicked(Neuron neuron, Transform listItem)
        {
            if (_currentlySelectedNeuronListItem != null)
            {
                ChangeListItemUi(_currentlySelectedNeuronListItem, false);
            }

            ChangeListItemUi(listItem, true);
            _currentlySelectedNeuronListItem = listItem;
            _selectedNeuron = neuron;
        }

        private void ChangeListItemUi(Transform listItem, bool isSelected)
        {
            // Sometimes list item is deleted by the time we arrive here. We want to prevent NullReferenceException
            if (listItem == null) return;

            var backgroundColor = isSelected ? _lightListItemColor : _darkListItemColor;
            var textColor = isSelected ? _darkListItemTextColor : _lightListItemTextColor;

            var button = listItem.transform.Find("ListItem_Button");
            var image = button.GetComponent<Image>();
            var text = button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>();
            image.color = backgroundColor;
            text.color = textColor;
            text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }

        private void ClearScrollView(Transform scrollView)
        {
            foreach (Transform child in scrollView)
            {
                Destroy(child.gameObject);
            }
        }
    }
}