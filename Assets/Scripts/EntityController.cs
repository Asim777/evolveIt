using UnityEngine;
using System.Collections;
using Random = System.Random;
using UnityEditor.Experimental.GraphView;
using System;
using Unity.VisualScripting;

public class EntityController : MonoBehaviour
{
    // Public variables
    public float speed = 5f; // The speed at which the Entity moves
    public float entityIntervalCoefficient = 4f; // Coefficient to adjust the interval between direction changes
    public float healthMeter = 100f; // The health meter of the Entity. 0.0f means dead, 100.0f means healthy
    public float hungerMeter = 0f; // The hunger meter of the Entity. 100.0f means starving, 0.0f means full
    public float reproductionMeter = 0f; // The reproduction meter of the Entity. 100.0f means ready to reproduce, 0.0f means not ready

    // Private variables 
    private static readonly System.Random rnd = new();
    private Rigidbody2D rb; // Reference to the Rigidbody2D component attached to the Entity
    private float currentDirection = 0; // The current direction the Entity is facing. 0 means right. 
    private Coroutine updateDirectionCoroutine; // Reference to the coroutine that updates the movement direction of Entities
    private Coroutine incrementHungerMeterCoroutine; // Reference to the coroutine that increments the hunger meter of Entities
    private readonly float hungerImportance = 0.4f; // The importance of hunger for the health of the Entity. The smaller the value, the more important it is
    private readonly float reproductionImportance = 15f; // The importance of reproduction for the health of the Entity. The smaller the value, the more important it is
    private GameObject selectionOutline; 
    void Start()
    {
         // Prevent the Entity from rotating
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        StartEntityCoroutines();
    }

    public void OnSimulationUpdate() 
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null) {
            // Allow Entities to move again
            rb.bodyType = RigidbodyType2D.Dynamic;
            
            // Move the entity in the direction it is facing
            Vector2 facingDirection = new(
                Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad), 
                Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad)
            );
            rb.linearVelocity = facingDirection * speed;
        } else {
            Debug.LogError("Rigidbody2D component not found on Entity." + gameObject.name);
        }
    }

    public void Resume() 
    {
        StartEntityCoroutines();
    }

    public void Pause() {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null) {
            // Stop Entities from moving
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        } else {
            Debug.LogError("Rigidbody2D component not found on Entity." + gameObject.name);
        }
        
        StopEntityCoroutines();
    }

    public void OnDeath() 
    {
        DeselectEntity();
        StopEntityCoroutines();
    }

    public void SelectEntity() 
    {
        // Focus on the Entity when it is clicked and show its stats and action buttons
        SimulationController.Instance.RegisterSelectedEntity(gameObject);
        CameraController.Instance.FocusOnTarget(gameObject);
        UiController.Instance.ShowEntityStatsPanel(this);

        // Instantiate the outline and set it as a child of the entity
        GameObject outlinePrefab = Resources.Load<GameObject>("EntitySelectionOutlinePrefab");

        if (outlinePrefab == null)
        {
            Debug.LogError("Entity selection outline prefab is not loaded!");
            return;
        }

        selectionOutline = Instantiate(outlinePrefab, transform);
    }

    public void DeselectEntity()
    {
        // Stop the Camera from following the Entity
        CameraController.Instance.StopFollowing();
        UiController.Instance.HideEntityStatsPanel();

        // Destroy the outline
        if (selectionOutline != null)
        {
            Destroy(selectionOutline);
            selectionOutline = null;
        }
    }

    public void OnMouseDown()
    {
        SelectEntity();
    }

    private void StartEntityCoroutines() 
    {
        updateDirectionCoroutine ??= StartCoroutine(UpdateMovementDirection());
        incrementHungerMeterCoroutine ??= StartCoroutine(UpdateInternalStates());
    }

    private void StopEntityCoroutines()
    {
        StopCoroutine(updateDirectionCoroutine);
        updateDirectionCoroutine = null;

        StopCoroutine(incrementHungerMeterCoroutine);
        incrementHungerMeterCoroutine = null;
    }

    private IEnumerator UpdateMovementDirection()
    {
        while (SimulationController.Instance.simulationState == SimulationState.Running && healthMeter > 0f)
        {
            // Generate a random angle to turn
            float randomAngle = rnd.Next(-90, 90);
            currentDirection += randomAngle;
            // Apply the rotation to the Entity
            transform.rotation = Quaternion.Euler(0, 0, currentDirection);

            // Wait for the next direction change interval
            yield return new WaitForSeconds(SimulationController.simulationStepInterval/entityIntervalCoefficient);
        }
    }

    private IEnumerator UpdateInternalStates()
    {
        while (SimulationController.Instance.simulationState == SimulationState.Running && healthMeter > 0f)
        {
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
                healthChangeFromHunger = Math.Cbrt((50 - hungerMeter) / hungerImportance);
            }
            if (reproductionMeter > 50) 
            {
                healthChangeFromReproduction = Math.Cbrt((50 - reproductionMeter) / reproductionImportance);
            }
        
            // x = cubic root of (50 - y / a) 
            // healthMeter can't be  less than 0 and greater than 100
            healthMeter = Mathf.Clamp((float)(healthMeter + healthChangeFromHunger + healthChangeFromReproduction), 0f, 100f);

            // Wait 1 second for the next increment
            yield return new WaitForSeconds(SimulationController.simulationStepInterval);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food"))
        {
            // Decrease the hunger meter after eating food and remove the food from the simulation. If hungerMeter goes below 0, set it to 0
            hungerMeter = Mathf.Max(hungerMeter - 10f, 0f);
            SimulationController.Instance.RemoveFood(other.gameObject);
        }
    }
}