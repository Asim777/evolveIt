using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class SimulationController : MonoBehaviour
{
    // Public variables
    public static SimulationController Instance  { get; private set; } // Singleton instance for easy access
    internal static readonly float simulationStepInterval = 0.5f; // Interval in seconds to evaluate the entities and update the simulation state
    public SimulationState simulationState = SimulationState.Stopped; // State of the simulation
    public SimualtionSpeed simulationSpeed = SimualtionSpeed.Normal; // Speed of the simulation

    // World Settings
    public int numberOfEntities; // Number of entities to spawn initially
    public int numberOfFood; // Number of food items to spawn initially
    public int worldSize; // World is square shaped

    // Private variables
    private static readonly System.Random rnd = new();
    public ObservableCollection<GameObject> entities = new();
    private readonly List<GameObject> foodItems = new();
    private GameObject selectedEntity;
    private TimeSpan totalTimeElapsed = TimeSpan.Zero;
    private TimeSpan timeElapsedInCurrentSession = TimeSpan.Zero;
    private DateTime startTime; // Time when the simulation started
    private Coroutine simulationJobsCoroutine;

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
            // Place your code to update the simulation each frame here
            // Loop through each Entity and call their Move() method
            foreach (var entity in entities)
            {
                if (entity != null) {
                    entity.GetComponent<EntityController>().OnSimulationUpdate();
                }
            }
        }

        // Deselect Selected Entity when right mouse button is clicked
        if (Input.GetMouseButton(1))
        {
            DeselectSelectedEntity();
        }
    }

    public void RemoveFood(GameObject food) {
        foodItems.Remove(food);
        Destroy(food);
    }

    public void StartSimulation() 
    {
        if (simulationState == SimulationState.Stopped) {
            startTime = DateTime.Now;
            
            StartCoroutine(InitializeSimulation());

            simulationState = SimulationState.Running;

            if (simulationJobsCoroutine == null)
            {
                simulationJobsCoroutine = StartCoroutine(LaunchSimulationJobs());
            }

            Debug.Log("Simulation Started.");
        }
    }

    public void ResumeSimulation()
    {
        Debug.Log("Simulation Resumed.");

        startTime = DateTime.Now;
        simulationState = SimulationState.Running;
        StartCoroutine(LaunchSimulationJobs());

        foreach (var entity in entities) 
        {
            if (entity != null) 
            {
                entity.GetComponent<EntityController>().Resume();
            }
        }
    }   

    public void PauseSimulation()
    {
        simulationState = SimulationState.Paused;

        foreach (var entity in entities)
        {
            if (entity != null) 
            {
                entity.GetComponent<EntityController>().Pause();
            }
        }

        totalTimeElapsed += timeElapsedInCurrentSession;
        timeElapsedInCurrentSession = TimeSpan.Zero;

        Debug.Log("Simulation Paused.");
    }

    
    public void StopSimulation()
    {
        simulationState = SimulationState.Stopped;

        DeselectSelectedEntity();

        // Destroy all entities GameObjects
        foreach (GameObject entity in entities)
        {
          Destroy(entity);
        }

        // Destroy all food GameObjects
        foreach (GameObject food in foodItems)
        {
          Destroy(food);
        }

        // Clear the list 
        entities.Clear();
        foodItems.Clear();

        // Reset the time elapsed
        timeElapsedInCurrentSession = TimeSpan.Zero;
        totalTimeElapsed = TimeSpan.Zero;

        // Stop the simulation jobs coroutine
        StopCoroutine(simulationJobsCoroutine);
        simulationJobsCoroutine = null;
        Debug.Log("Simulation Stopped.");
    }

    public void RegisterSelectedEntity(GameObject entity)
    {
        DeselectSelectedEntity();
        selectedEntity = entity;    
    }

    private IEnumerator InitializeSimulation()
    {
        // Using Coroutine to spread out the spawning over multiple frames
        SpawnFood();
        SpawnEntities();

        yield return true;
    }

    private IEnumerator LaunchSimulationJobs()
    {
        while (simulationState == SimulationState.Running) 
        { 
            RemoveDeadEntities();

            timeElapsedInCurrentSession = DateTime.Now - startTime;
            TimeSpan timeToDisplay = totalTimeElapsed + timeElapsedInCurrentSession;
            UiController.Instance.UpdateSimulationInformationPanel(timeToDisplay, simulationSpeed, entities.Count, foodItems.Count);
                
            // Check if all entities are dead
            if (entities.Count == 0)
            {
                Debug.Log("All entities are dead. Stopping simulation.");
                StopSimulation();
                UiController.Instance.OnSimulationEnded();
                yield break;
            }
        
            yield return new WaitForSeconds(simulationStepInterval);
        }
    }

    private void SpawnEntities()
    {
        // Load the prefab from Resources
        GameObject entityPrefab = Resources.Load<GameObject>("EntityPrefab");

        if (entityPrefab == null)
         {
            Debug.LogError("Entity prefab is not loaded!");
            return;
        }

        for (int i = 0; i < numberOfEntities; i++)
        {
            // Generate a random position within the world boundaries
            Vector2 randomPosition = new Vector2(
                rnd.Next(-worldSize / 2, worldSize / 2),
                rnd.Next(-worldSize / 2, worldSize / 2)
            );
            
            // Instantiate the entity and position it
            GameObject entity = Instantiate(entityPrefab, randomPosition, Quaternion.identity);
            entity.name = "Entity_" + i;

            // If Entity GameObject is not null, register it
            if (entity != null)
            {
                entities.Add(entity);
            }
            else
            {
                Debug.LogError("Entity prefab was not initiated!");
            }
        }
        Debug.Log("Spawned " + entities.Count + " entities.");
    }

    private void SpawnFood() {
        Debug.Log("Spawning food");
        // Load the prefab from Resources
        GameObject foodPrefab = Resources.Load<GameObject>("FoodPrefab");

        if (foodPrefab == null)
        {
            Debug.LogError("Food prefab is not loaded!");
            return;
        }
            
        for (int i = 0; i < numberOfFood; i++)
        {
            // Generate a random position within the world boundaries
            Vector2 randomPosition = new Vector2(
                rnd.Next(-worldSize / 2, worldSize / 2),
                rnd.Next(-worldSize / 2, worldSize / 2)
            );

            // Instantiate the food and position it
            GameObject food = Instantiate(foodPrefab, randomPosition, Quaternion.identity);

            // If Entity GameObject is not null, register it
            if (food != null)
            {
                foodItems.Add(food);
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
        foreach (GameObject entity in entities) {
            if (entity == null) 
            {
                Debug.Log("Entity died because entity was null");
                entitiesToRemove.Add(entity);
            }   
            EntityController entityController = entity.GetComponent<EntityController>();
            if (entityController == null)
            {
                Debug.Log("Entity died because controller was null: " + entity.name);
                entitiesToRemove.Add(entity);
            }   

            if (entityController.healthMeter <= 0f) 
            {
                Debug.Log("Entity died because health reached 0 : " + entity.name);
                entitiesToRemove.Add(entity);
            } 
        }
        entitiesToRemove.ForEach(entity => {
            EntityController entityController = entity.GetComponent<EntityController>();
            entities.Remove(entity);
            entityController.OnDeath();
            Destroy(entity);
        });
    }

    private void DeselectSelectedEntity()
    {
        if (selectedEntity != null)
        {
            selectedEntity.GetComponent<EntityController>().DeselectEntity();
            selectedEntity = null;
        }
    }
}

public enum SimualtionSpeed {
    Normal,
    Double,
    Quadruple,
    Octuple,
    x16
}

public enum SimulationState {
    Running,
    Paused,
    Stopped
}
