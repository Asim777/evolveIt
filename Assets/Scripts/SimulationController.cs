using System;
using System.Collections;
using System.Collections.Generic;
using UI_Panels;
using UnityEngine;
using Util;
using Random = System.Random;

public class SimulationController : MonoBehaviour
{
    // Public variables
    public static SimulationController Instance { get; private set; } // Singleton instance for easy access

    internal const float
        SimulationStepInterval = 0.5f; // Interval in seconds to evaluate the entities and update the simulation state

    public SimulationState simulationState = SimulationState.Stopped; // State of the simulation
    public SimualationSpeed simulationSpeed = SimualationSpeed.Normal; // Speed of the simulation

    // World Settings
    public int numberOfEntities; // Number of entities to spawn initially
    public int initialNumberOfFood; // Number of food items to spawn initially
    public int perStepumberOfFood; // Number of food to spawn every simulation step
    public int worldSize; // World is square shaped

    // Private variables
    private static readonly Random Rnd = new();
    public readonly RangeObservableCollection<GameObject> Entities = new();
    public readonly RangeObservableCollection<GameObject> Watchlist = new();
    private readonly List<GameObject> _foodItems = new();
    private GameObject _selectedEntity;
    private TimeSpan _totalTimeElapsed = TimeSpan.Zero;
    private TimeSpan _timeElapsedInCurrentSession = TimeSpan.Zero;
    private DateTime _startTime; // Time when the simulation started
    private Coroutine _simulationJobsCoroutine;

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
            foreach (var entity in Entities)
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

        // Stop the simulation jobs coroutine
        if (_simulationJobsCoroutine != null)
        {
            StopCoroutine(_simulationJobsCoroutine);
        }

        _simulationJobsCoroutine = null;
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

    private IEnumerator InitializeSimulation()
    {
        // Using Coroutine to spread out the spawning over multiple frames
        SpawnFood(initialNumberOfFood);
        SpawnInitialEntities();

        yield return true;
    }

    private IEnumerator LaunchSimulationJobs()
    {
        while (simulationState == SimulationState.Running)
        {
            RemoveDeadEntities();
            SpawnFood(numberOfFood);

            _timeElapsedInCurrentSession = DateTime.Now - _startTime;
            var timeToDisplay = _totalTimeElapsed + _timeElapsedInCurrentSession;
            UiController.Instance.UpdateSimulationInformationPanel(timeToDisplay, simulationSpeed, Entities.Count,
                _foodItems.Count);

            // Check if all entities are dead
            if (Entities.Count == 0)
            {
                StopSimulation();
                UiController.Instance.OnSimulationEnded();
                Debug.Log("All entities are dead. Stopping simulation.");
                yield break;
            }

            yield return new WaitForSeconds(SimulationStepInterval);
        }
    }

    private void SpawnInitialEntities()
    {
        // Load the prefab from Resources
        var entityPrefab = Resources.Load<GameObject>("EntityPrefab");

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

        Debug.Log("Spawned " + Entities.Count + " entities.");
    }

    private void SpawnFood(int numberOfFood)
    {
        Debug.Log("Spawning food");
        // Load the prefab from Resources
        var foodPrefab = Resources.Load<GameObject>("FoodPrefab");

        if (!foodPrefab)
        {
            Debug.LogError("Food prefab is not loaded!");
            return;
        }

        for (var i = 0; i < numberOfFood; i++)
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

                if (entityController.healthMeter <= 0f || entityController.age <= 120)
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
}

public enum SimualationSpeed
{
    Normal,
    Double,
    Quadruple,
    Octuple,
    X16
}

public enum SimulationState
{
    Running,
    Paused,
    Stopped
}
