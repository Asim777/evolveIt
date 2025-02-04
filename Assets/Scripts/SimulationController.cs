using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Gene;
using Data.Neuron;
using UI_Panels;
using UnityEngine;
using Util;
using Random = System.Random;

public class SimulationController : MonoBehaviour
{
    // Public variables
    public static SimulationController Instance { get; private set; } // Singleton instance for easy access

    public SimulationState simulationState = SimulationState.Stopped; // State of the simulation
    public static SimulationSpeed SimulationSpeed = SimulationSpeed.X1; // Speed of the simulation

    // World Settings
    public int numberOfEntities; // Number of entities to spawn initially
    public int initialNumberOfFood; // Number of food items to spawn initially
    public float numberOfFoodPerStep; // Number of food to spawn every simulation step
    public int worldSize; // World is square shaped
    public ActivationGroup activationGroup; // Combination of Neurons to use in Simulation 
    public int neuronConnectionLimit; // Maximum number of Genes that the Entities are allowed to have

    // Private variables
    private static readonly Random Rnd = new();
    private int _entityCounter;
    public readonly RangeObservableCollection<GameObject> Entities = new();
    public readonly RangeObservableCollection<GameObject> Watchlist = new();
    private readonly List<GameObject> _foodItems = new();
    private GameObject _selectedEntity;
    private TimeSpan _totalTimeElapsed = TimeSpan.Zero;
    private TimeSpan _timeElapsedInCurrentSession = TimeSpan.Zero;
    private DateTime _startTime; // Time when the simulation started
    private Coroutine _simulationJobsCoroutine;
    private Coroutine _spawnFoodCoroutine;

    public void Awake()
    {
        // Ensure that there is only one SimulationController instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    public void Update()
    {
        if (simulationState == SimulationState.Running)
        {
            foreach (var entity in Entities.ToList())
            {
                if (!entity) continue;

                if (entity.TryGetComponent(out EntityController entityController))
                {
                    entityController.OnSimulationUpdate();
                }
            }
        }

        // Deselect Selected Entity when right mouse button is clicked
        if (Input.GetMouseButton(1))
        {
            DeselectSelectedEntity();
        }
    }

    public void RemoveFood(GameObject food)
    {
        _foodItems.Remove(food);
        Destroy(food);
    }

    public void StartSimulation()
    {
        if (simulationState != SimulationState.Stopped) return;

        _startTime = DateTime.Now;
        StartCoroutine(InitializeSimulation());
        simulationState = SimulationState.Running;
        _simulationJobsCoroutine ??= StartCoroutine(LaunchSimulationJobs());
        _spawnFoodCoroutine ??= StartCoroutine(SpawnRegularFood());
        Debug.Log("Simulation Started.");
    }

    public void ResumeSimulation()
    {
        Debug.Log("Simulation Resumed.");

        _startTime = DateTime.Now;
        simulationState = SimulationState.Running;
        StartCoroutine(LaunchSimulationJobs());

        foreach (var entity in Entities)
        {
            if (entity)
            {
                entity.GetComponent<EntityController>().Resume();
            }
        }
    }

    public void PauseSimulation()
    {
        simulationState = SimulationState.Paused;

        foreach (var entity in Entities)
        {
            if (entity)
            {
                entity.GetComponent<EntityController>().Pause();
            }
        }

        _totalTimeElapsed += _timeElapsedInCurrentSession;
        _timeElapsedInCurrentSession = TimeSpan.Zero;

        Debug.Log("Simulation Paused.");
    }


    public void StopSimulation()
    {
        simulationState = SimulationState.Stopped;
        SimulationSpeed = SimulationSpeed.X1;

        // Destroy all entities GameObjects
        foreach (var entity in Entities)
        {
            Destroy(entity);
        }

        // Destroy all food GameObjects
        foreach (var food in _foodItems)
        {
            Destroy(food);
        }

        // Clear the list 
        Entities.Clear();
        Watchlist.Clear();
        _foodItems.Clear();

        // Reset the time elapsed
        _timeElapsedInCurrentSession = TimeSpan.Zero;
        _totalTimeElapsed = TimeSpan.Zero;
        _entityCounter = 0;

        StopAllCoroutines();

        _simulationJobsCoroutine = null;
        _spawnFoodCoroutine = null;
        Debug.Log("Simulation Stopped.");
    }

    public void RegisterSelectedEntity(GameObject entity)
    {
        DeselectSelectedEntity();
        _selectedEntity = entity;
    }

    public GameObject GetSelectedEntity()
    {
        return _selectedEntity;
    }

    public static float GetSimulationStepInterval()
    {
        return 1 / SimulationSpeed.GetValue();
    }

    private IEnumerator InitializeSimulation()
    {
        // Using Coroutine to spread out the spawning over multiple frames
        SpawnInitialFood();
        SpawnInitialEntities();

        yield return true;
    }

    // All this work is done once per Simulation step
    private IEnumerator LaunchSimulationJobs()
    {
        while (simulationState == SimulationState.Running)
        {
            RemoveDeadEntities();

            _timeElapsedInCurrentSession = DateTime.Now - _startTime;
            var timeToDisplay = _totalTimeElapsed + _timeElapsedInCurrentSession;
            UiController.Instance.UpdateSimulationInformationPanel(timeToDisplay, SimulationSpeed, Entities.Count,
                _foodItems.Count);

            // Check if all entities are dead
            if (Entities.Count == 0)
            {
                StopSimulation();
                UiController.Instance.OnSimulationEnded();
                Debug.Log("All entities are dead. Stopping simulation.");
                yield break;
            }

            yield return new WaitForSeconds(GetSimulationStepInterval());
        }
    }

    private void SpawnInitialEntities()
    {
        var entityPrefab = Resources.Load<GameObject>("EntityPrefab");
        var neurons = NeuronExtensions.GetNeurons(activationGroup);
        var sensorNeurons = neurons.OfType<SensorNeuron>().ToList();
        var innerNeurons = neurons.OfType<InnerNeuron>().ToList();
        var sinkNeurons = neurons.OfType<SinkNeuron>().ToList();

        if (!entityPrefab)
        {
            Debug.LogError("Entity prefab is not loaded!");
            return;
        }

        var deferredList = new List<GameObject>();
        for (var i = 0; i < numberOfEntities; i++)
        {
            // Generate a random position within the world boundaries
            var randomPosition = new Vector2(
                Rnd.Next(-worldSize / 2, worldSize / 2),
                Rnd.Next(-worldSize / 2, worldSize / 2)
            );

            // Instantiate the entity and position it
            var entity = Instantiate(entityPrefab, randomPosition, Quaternion.identity);
            entity.name = "Entity_" + i;
            entity.TryGetComponent<EntityController>(out var entityController);
            entityController.Genome = GetRandomGenome(sensorNeurons, innerNeurons, sinkNeurons);

            // If Entity GameObject is not null, register it
            if (entity)
            {
                deferredList.Add(entity);
            }
            else
            {
                Debug.LogError("Entity prefab was not initiated!");
            }
        }

        Entities.AddRange(deferredList);
        _entityCounter += deferredList.Count;

        Debug.Log("Spawned " + Entities.Count + " entities.");
    }

    public void SpawnEntity(Vector2 position)
    {
        // Instantiate the entity and position it
        _entityCounter++;
        var entityPrefab = Resources.Load<GameObject>("EntityPrefab");
        var entity = Instantiate(entityPrefab, position, Quaternion.identity);
        entity.name = "Entity_" + _entityCounter;
        Entities.Add(entity);
        Debug.Log("New entity is born " + entity.name);
    }

    private void SpawnInitialFood()
    {
        // Load the prefab from Resources
        var foodPrefab = Resources.Load<GameObject>("FoodPrefab");

        if (!foodPrefab)
        {
            Debug.LogError("Food prefab is not loaded!");
            return;
        }

        for (var i = 0; i < initialNumberOfFood; i++)
        {
            SpawnFood(foodPrefab);
        }
    }

    private IEnumerator SpawnRegularFood()
    {
        while (simulationState == SimulationState.Running)
        {
            // If numberOfFoodPerStep is lower than 1, we want to skip some steps before spawning food. 
            var foodProductionIntervalCoefficient = numberOfFoodPerStep < 1 ? 1 / numberOfFoodPerStep : 1;
            var foodPrefab = Resources.Load<GameObject>("FoodPrefab");
            var numberOfFood = numberOfFoodPerStep < 1 ? 1 : numberOfFoodPerStep;
            for (var i = 0; i < numberOfFood; i++)
            {
                SpawnFood(foodPrefab);
            }

            yield return new WaitForSeconds(GetSimulationStepInterval() * foodProductionIntervalCoefficient);
        }
    }

    private void SpawnFood(GameObject foodPrefab)
    {
        // Generate a random position within the world boundaries
        var randomPosition = new Vector2(
            Rnd.Next(-worldSize / 2, worldSize / 2),
            Rnd.Next(-worldSize / 2, worldSize / 2)
        );

        // Instantiate the food and position it
        var food = Instantiate(foodPrefab, randomPosition, Quaternion.identity);

        // If Entity GameObject is not null, register it
        if (food)
        {
            _foodItems.Add(food);
        }
        else
        {
            Debug.LogError("Food prefab was not initiated!");
        }
    }

    private void RemoveDeadEntities()
    {
        List<GameObject> entitiesToRemove = new();
        // Remove dead entities from the list
        foreach (var entity in Entities)
        {
            if (entity != null)
            {
                if (!entity.TryGetComponent<EntityController>(out var entityController))
                {
                    Debug.Log("Entity died because controller was null: " + entity.name);
                    entitiesToRemove.Add(entity);
                }

                if (entityController.healthMeter <= 0f || entityController.age >= 120)
                {
                    Debug.Log("Entity died because health reached 0 : " + entity.name);
                    entitiesToRemove.Add(entity);
                }
            }
        }

        entitiesToRemove.ForEach(entity =>
        {
            entity.TryGetComponent<EntityController>(out var entityController);

            Entities.Remove(entity);
            Watchlist.Remove(entity);
            entityController.OnDeath();
            Destroy(entity);
        });
    }

    // Called when right button is clicked, simulation is stopped, when another entity is selected and when multiple
    // selection mode entered on Entity List UI
    public void DeselectSelectedEntity()
    {
        if (_selectedEntity == null) return;

        _selectedEntity.TryGetComponent<EntityController>(out var entityController);
        entityController.DeselectEntity();
        _selectedEntity = null;
    }

    private List<NeuronConnection> GetRandomConnections(
        List<SensorNeuron> sensorNeurons,
        List<InnerNeuron> innerNeurons,
        List<SinkNeuron> sinkNeurons
    )
    {
        var neuronConnections = new List<NeuronConnection>();
        for (var i = 0; i < neuronConnectionLimit; i++)
        {
            var connection = GenerateRandomConnection(sensorNeurons, innerNeurons, sinkNeurons, neuronConnections);
            neuronConnections.Add(connection);
        }

        return neuronConnections;
    }

    private NeuronConnection GenerateRandomConnection(
        List<SensorNeuron> sensorNeurons,
        List<InnerNeuron> innerNeurons,
        List<SinkNeuron> sinkNeurons,
        List<NeuronConnection> neuronConnections
    )
    {
        NeuronConnection connectionToAdd;
        do
        {
            connectionToAdd = GetRandomNeuronConnection(
                sensorNeurons.OfType<Neuron>().ToList(),
                innerNeurons.OfType<Neuron>().ToList(),
                sinkNeurons.OfType<Neuron>().ToList()
            );
        } while (neuronConnections.Any(nc => nc.Equals(connectionToAdd)));

        return connectionToAdd;
    }

    private NeuronConnection GetRandomNeuronConnection(
        List<Neuron> sensorNeurons,
        List<Neuron> innerNeurons,
        List<Neuron> sinkNeurons)
    {
        // Create a NeuronConnection and assign to the genome
        var inputList = sensorNeurons.Concat(innerNeurons).ToList();
        var outputList = sinkNeurons;
        Random random = new();
        var input = inputList[random.Next(0, inputList.Count-1)];
        // If the input is a sensor, we can connect to both inner and sink neurons, otherwise, if input is
        // inner, then we connect only to sink neurons
        if (input is SensorNeuron)
        {
            outputList = outputList.Concat(innerNeurons).ToList();
        }

        var output = outputList[random.Next(0, outputList.Count-1)];

        return new NeuronConnection((IInputNeuron)input, (IOutputNeuron)output);
    }

    private List<Gene> GetRandomGenome(
        List<SensorNeuron> sensorNeurons,
        List<InnerNeuron> innerNeurons,
        List<SinkNeuron> sinkNeurons
    )
    {
        var genome = new List<Gene>();

        var neuronConnections = GetRandomConnections(sensorNeurons, innerNeurons, sinkNeurons);
        foreach (var connection in neuronConnections)
        {
            // If we have a direct connection between a Sensor and Sink, we create a Gene and if it doesn't
            // already exist in Genome, we add the Gene to Genome.
            if (connection.Input is SensorNeuron sensorNeuron && connection.Output is SinkNeuron sinkNeuron)
            {
                var newGene = new Gene(
                    sensors: new List<Neuron> { sensorNeuron },
                    inner: null,
                    sink: sinkNeuron
                );

                if (!genome.Contains(newGene)) genome.Add(newGene);
            }

            // If it is a connection between Sensor and Inner
            if (connection.Input is SensorNeuron && connection.Output is InnerNeuron innerNeuron)
            {
                // Find all the connections between this Inner and any Sensors. We want to include all of them in
                // one Gene
                var allSensors = neuronConnections
                    .Where(c => c.Input is SensorNeuron && c.Output == innerNeuron)
                    .Select(c => c.Input as SensorNeuron)
                    .ToList();

                // Find all the connections between this Inner and any Sinks. We want to create a separate Gene for
                // each of them with the list of Sensors and this Inner Neuron
                var allSinks = neuronConnections
                    .Where(c => c.Input == innerNeuron && c.Output is SinkNeuron)
                    .Select(c => c.Output as SinkNeuron);

                foreach (var sink in allSinks)
                {
                    var geneToAdd = new Gene(
                        sensors: allSensors.OfType<Neuron>().ToList(),
                        inner: innerNeuron,
                        sink: sink
                    );
                    if (!genome.Contains(geneToAdd)) genome.Add(geneToAdd);
                }
            }
        }

        return genome;
    }

    /*private List<Gene> GenerateRandomGenome()
    {
        var genome = new List<Gene>();

        return genome;
    }*/
}

public enum SimulationSpeed
{
    X05,
    X1,
    X2,
    X4,
    X8,
    X16
}

public static class SimulationSpeedExtensions
{
    // Interval in seconds to evaluate the entities and update the simulation state
    public static float GetValue(this SimulationSpeed speed)
    {
        switch (speed)
        {
            case SimulationSpeed.X05:
                return 0.5f;
            case SimulationSpeed.X1:
                return 1.0f;
            case SimulationSpeed.X2:
                return 2.0f;
            case SimulationSpeed.X4:
                return 4.0f;
            case SimulationSpeed.X8:
                return 8.0f;
            case SimulationSpeed.X16:
                return 16.0f;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}


public enum SimulationState
{
    Running,
    Paused,
    Stopped
}