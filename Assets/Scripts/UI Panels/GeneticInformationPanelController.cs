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
        public Gene SelectedGene;
        public Neuron SelectedNeuron;

        private Transform _genesScrollViewContent;
        private Transform _neuronsScrollViewContent;
        private SelectedListItem _currentlySelectedGeneListItem;
        private SelectedListItem _currentlySelectedNeuronListItem;
        private Transform _geneSelectionResetButton;
        private Transform _neuronSelectionResetButton;
        private GameObject _listItemPrefab;

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

            _listItemPrefab = Resources.Load<GameObject>("ListItemPrefab");
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
                var listItem = Instantiate(_listItemPrefab, _genesScrollViewContent);
                listItem.GetComponent<UiListItemController<Gene>>().Init(UiListType.GeneList, gene.Name);

                var button = listItem.transform.Find("ListItem_Button");

                button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = gene.Name;
            }
        }

        public void OnSimulationStopped()
        {
            ClearScrollView(_genesScrollViewContent);
            ClearScrollView(_neuronsScrollViewContent);
        }

        public void OnGenesResetButtonClick()
        {
            _currentlySelectedGeneListItem.ListItem.GetComponent<UiListItemController<Gene>>().IsSelected = false;
            SelectedGene = null;
            _currentlySelectedGeneListItem = null;
            ClearScrollView(_neuronsScrollViewContent);
        }

        public void OnNeuronsResetButtonClick()
        {
            _currentlySelectedNeuronListItem.ListItem.GetComponent<UiListItemController<Neuron>>().IsSelected = false;
            SelectedNeuron = null;
            _currentlySelectedNeuronListItem = null;
        }

        public void SelectNextListItem(UiListType listType)
        {
            Transform[] listItems;
            Transform nextItem = null;

            switch (listType)
            {
                case UiListType.GeneList:
                    if (_currentlySelectedGeneListItem != null)
                    {
                        listItems = _genesScrollViewContent.transform.GetComponentsInChildren<Transform>(false);
                        nextItem = listItems[_currentlySelectedGeneListItem.Index + 1];
                    }

                    break;
                case UiListType.NeuronList:
                    if (_currentlySelectedNeuronListItem != null)
                    {
                        listItems = _neuronsScrollViewContent.transform.GetComponentsInChildren<Transform>(false);
                        nextItem = listItems[_currentlySelectedNeuronListItem.Index + 1];
                    }

                    break;
                default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }

            if (nextItem == null) return;
            var genome = SimulationController.Instance.GetSelectedEntity()?.GetComponent<EntityController>()?.Genome;
            if (genome == null) return;
            var nextEntity = selectedEntity.GetComponent<EntityController>()
                .First(entity => entity.name == nextItem.name);
            if (nextEntity != null) SimulationController.Instance.RegisterSelectedEntity(nextEntity);
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

        private static void CreateNeuronCircle(
            GameObject neuronPrefab, Transform neuralMapContainer, List<Neuron> neurons, int radius)
        {
            // We arrange all Neurons around the center of NeuralMap Container
            var point = neuralMapContainer.transform.position;
            var center = new Vector2(point.x, point.y);

            for (var i = 0; i < neurons.Count; i++)
            {
                // Distance around the circle
                var radian = 2 * MathF.PI / neurons.Count * (i + 1);

                // Get the vector direction
                var vertical = MathF.Sin(radian);
                var horizontal = MathF.Cos(radian);

                var spawnDir = new Vector2(horizontal, vertical);

                // Get the spawn position. Radius is just the distance away from the point
                var spawnPos = center + spawnDir * radius;

                // Spawning Neuron object
                GameObject neuronObject = Instantiate(neuronPrefab, spawnPos, Quaternion.identity, neuralMapContainer);
                var neuronId = neuronObject.transform.Find("Neuron_Id").GetComponent<TextMeshProUGUI>();
                neuronId.text = neurons[i].Id;
                Debug.Log("Created new " + neurons[i].Category + ", Neuron name: " + neurons[i].Id + " radius: " +
                          radius);
            }
        }

        private void OnGeneClicked(Gene gene, Transform listItem)
        {
            if (_currentlySelectedGeneListItem.ListItem == listItem) return;

            if (_currentlySelectedGeneListItem != null)
            {
                ChangeListItemUi(_currentlySelectedGeneListItem, false);
            }

            ChangeListItemUi(listItem, true);
            _currentlySelectedGeneListItem = listItem;
            SelectedGene = gene;
            UiController.Instance.SetActiveList(UiListType.GeneList);

            OnListItemClicked(listItem, GeneticListType.GeneList, gene);
            InitiateNeuronsList();
        }

        private void InitiateNeuronsList()
        {
            ClearScrollView(_neuronsScrollViewContent);

            var selectedEntityController =
                SimulationController.Instance.GetSelectedEntity()?.GetComponent<EntityController>();
            if (selectedEntityController == null) return;

            foreach (var neuron in SelectedGene.GetAllNeurons())
            {
                var listItem = Instantiate(_listItemPrefab, _neuronsScrollViewContent);
                listItem.GetComponent<UiListItemController<Neuron>>().Init(UiListType.NeuronList, neuron.Id);
                var button = listItem.transform.Find("ListItem_Button");

                button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = neuron.Id;
            }
        }

        private void OnNeuronClicked(Neuron neuron, Transform listItem)
        {
            if (_currentlySelectedNeuronListItem == listItem) return;

            if (_currentlySelectedNeuronListItem != null)
            {
                ChangeListItemUi(_currentlySelectedNeuronListItem, false);
            }

            ChangeListItemUi(listItem, true);
            _currentlySelectedNeuronListItem = listItem;
            SelectedNeuron = neuron;
            UiController.Instance.SetActiveList(UiListType.NeuronList);
        }

        private void OnListItemClicked(Transform listItem, UiListType listType, object selectedObject)
        {
            var currentlySelectedItem = listType switch
            {
                UiListType.GeneList => _currentlySelectedGeneListItem,
                UiListType.NeuronList => _currentlySelectedNeuronListItem,
                _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
            };

            if (currentlySelectedItem.ListItem == listItem) return;

            if (currentlySelectedItem != null)
            {
                ChangeListItemUi(currentlySelectedItem, false);
            }

            ChangeListItemUi(listItem, true);
            switch (listType)
            {
                case GeneticListType.GeneList when selectedObject is Gene gene:
                    _currentlySelectedGeneListItem = listItem;
                    SelectedGene = gene;
                    break;
                case GeneticListType.NeuronList when selectedObject is Neuron neuron:
                    _currentlySelectedNeuronListItem = listItem;
                    SelectedNeuron = neuron;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }


            UiController.Instance.SetActiveList(UiListType.GeneList);
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