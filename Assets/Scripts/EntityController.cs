using System;
using System.Collections;
using System.Collections.Generic;
using Data.Gene;
using UI_Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = System.Random;

public class EntityController : MonoBehaviour
{
    // Age of the Entity. 
    public float age;

    // The speed at which the Entity moves
    public float speed = 5f;

    // Coefficient to adjust the interval between direction changes
    public float entityIntervalCoefficient = 4f;

    // The health meter of the Entity. 0.0f means dead, 100.0f means healthy
    public float healthMeter = 100f;

    // The hunger meter of the Entity. 100.0f means starving, 0.0f means full
    public float hungerMeter;

    // Is the Entity in process of Mating?
    public bool isMating;

    // The genome of the Entity made from list of Genes
    public List<Gene> Genome;

    // The reproduction meter of the Entity. 100.0f means ready to reproduce, 0.0f means not ready
    public float reproductionMeter;

    // Private variables 
    private static readonly Random Rnd = new();

    // Reference to the Rigidbody2D component attached to the Entity
    private Rigidbody2D _rb;

    // The current direction the Entity is facing. 0 means right. 
    private float _currentDirection;

    // Reference to the coroutine that updates the movement direction of Entities
    private Coroutine _updateDirectionCoroutine;

    // Reference to the coroutine that updates internal states of Entities, such as hunger, reproductive drive or age
    private Coroutine _updateInternalStatesCoroutine;

    // The importance of hunger for the health of the Entity. The smaller the value, the more important it is
    private const float HungerImportance = 0.4f; 
    
    // The importance of reproduction for the health of the Entity. The smaller the value, the more important it is
    private const float ReproductionImportance = 15f; 

    private GameObject _selectionOutline;

    private void Start()
    {
        // Prevent the Entity from rotating
        _rb = GetComponent<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        StartEntityCoroutines();
    }

    public void OnSimulationUpdate()
    {
        // If Entity is mating, it should be immobilized
        if (!isMating & TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Allow Entities to move after simulation pause
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Move the entity in the direction it is facing
            Vector2 facingDirection = new(
                Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad),
                Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad)
            );

            rb.linearVelocity = facingDirection * (speed * SimulationController.SimulationSpeed.GetValue());
        }
    }

    public void Resume()
    {
        StartEntityCoroutines();
    }

    public void Pause()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Stop Entities from moving
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            Debug.LogError("Rigidbody2D component not found on Entity." + gameObject.name);
        }

        StopEntityCoroutines();
    }

    public void OnDeath()
    {
        if (SimulationController.Instance.GetSelectedEntity() == gameObject)
        {
            DeselectEntity();
        }

        StopEntityCoroutines();
    }

    public void SelectEntity(bool isSelectedFromUi)
    {
        // Focus on the Entity when it is clicked and show its stats and action buttons
        SimulationController.Instance.RegisterSelectedEntity(gameObject);
        CameraController.Instance.FocusOnTarget(gameObject);
        UiController.Instance.OnEntitySelected(this, isSelectedFromUi);

        // Instantiate the outline and set it as a child of the entity
        var outlinePrefab = Resources.Load<GameObject>("EntitySelectionOutlinePrefab");

        if (outlinePrefab == null)
        {
            Debug.LogError("Entity selection outline prefab is not loaded!");
            return;
        }

        _selectionOutline = Instantiate(outlinePrefab, transform);
    }

    public void DeselectEntity()
    {
        // Stop the Camera from following the Entity
        CameraController.Instance.StopFollowing();
        UiController.Instance.OnEntityDeselected();

        // Destroy the outline
        if (_selectionOutline != null)
        {
            Destroy(_selectionOutline);
            _selectionOutline = null;
        }
    }

    public void OnMouseDown()
    {
        //  Do not process click logic if the cursor is over a UI element
        if (EventSystem.current.IsPointerOverGameObject()) return;
        SelectEntity(false);
    }

    private void StartEntityCoroutines()
    {
        _updateDirectionCoroutine ??= StartCoroutine(UpdateMovementDirection());
        _updateInternalStatesCoroutine ??= StartCoroutine(UpdateInternalStates());
    }

    private void StopEntityCoroutines()
    {
        StopAllCoroutines();
        _updateDirectionCoroutine = null;
        _updateInternalStatesCoroutine = null;
    }

    private IEnumerator UpdateMovementDirection()
    {
        while (SimulationController.Instance.simulationState == SimulationState.Running)
        {
            if (SimulationController.Instance.simulationState == SimulationState.Running && healthMeter > 0f &&
                !isMating)
            {
                // Generate a random angle to turn
                float randomAngle = Rnd.Next(-90, 90);
                _currentDirection += randomAngle;
                // Apply the rotation to the Entity
                transform.rotation = Quaternion.Euler(0, 0, _currentDirection);
            }
            else
            {
                yield return null; // pause the coroutine
                continue;
            }

            // Wait for the next direction change interval
            yield return new WaitForSeconds(SimulationController.GetSimulationStepInterval() / entityIntervalCoefficient);
        }
    }

    private IEnumerator UpdateInternalStates()
    {
        while (SimulationController.Instance.simulationState == SimulationState.Running && healthMeter > 0f)
        {
            // Increase the age of the Entity
            if (age < 120) age += 1;
            // Increase the Entity state meters and reproduction meter. Cap the meters at 100
            if (hungerMeter < 100) hungerMeter += 1f;
            if (reproductionMeter < 100) reproductionMeter += 1f;

            // Update healthMeter depending on Entity states.
            // Health dependency on hunger and reproduction is a cubic function (y=ax^{3}+50), where a is the importance of the state for health
            // Hunger and reproduction only affect health if they are below 50 
            double healthChangeFromHunger = 0f;
            double healthChangeFromReproduction = 0f;

            if (hungerMeter > 50)
            {
                healthChangeFromHunger = Math.Cbrt((50 - hungerMeter) / HungerImportance);
            }

            if (reproductionMeter > 50)
            {
                healthChangeFromReproduction = Math.Cbrt((50 - reproductionMeter) / ReproductionImportance);
            }

            // x = cubic root of (50 - y / a) 
            // healthMeter can't be  less than 0 and greater than 100
            healthMeter = Mathf.Clamp((float)(healthMeter + healthChangeFromHunger + healthChangeFromReproduction), 0f,
                100f);

            // Wait 1 second for the next increment
            yield return new WaitForSeconds(SimulationController.GetSimulationStepInterval());
        }
    }

    private IEnumerator GiveBirth(EntityController otherController)
    {
        yield return new WaitForSeconds(SimulationController.GetSimulationStepInterval() * 5);
        isMating = false;
        otherController.isMating = false;
        reproductionMeter = Mathf.Max(reproductionMeter - 50, 0);
        otherController.reproductionMeter = Mathf.Max(otherController.reproductionMeter - 50, 0);
        SimulationController.Instance.SpawnEntity(gameObject.transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food"))
        {
            // Decrease the hunger meter after eating food and remove the food from the simulation. If hungerMeter goes below 0, set it to 0
            hungerMeter = Mathf.Max(hungerMeter - 10f, 0f);
            SimulationController.Instance.RemoveFood(other.gameObject);
        }
        // If not already mating, and reproduction drive is high enough, mate with the encountered Entity producing offspring.
        // GetInstanceID comparison is for symmetry break so that only one Entity runs reproduction code
        else if (other.CompareTag("Entity") && isMating == false && reproductionMeter > 50 &&
                 GetInstanceID() < other.GetInstanceID())
        {
            var otherController = other.gameObject.GetComponent<EntityController>();
            if (otherController is { isMating: false, reproductionMeter: > 50 })
            {
                isMating = true;
                otherController.isMating = true;
                StartCoroutine(GiveBirth(otherController));
                Debug.Log(gameObject.name + " is mating with " + other.gameObject.name);
            }
        }
    }
}