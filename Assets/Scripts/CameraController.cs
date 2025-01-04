using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    // Public variables
    public static CameraController Instance { get; private set; }
    public float moveSpeed = 10f;    // Speed for moving the camera
    public float zoomSpeed = 5f;     // Speed for zooming in/out
    public float followSpeed = 2f;   // Speed for following the target
    public float minZoom = 2f;       // Minimum zoom level
    public float maxZoom = 20f;      // Maximum zoom level

    // Private variables 
    private Camera mainCamera;
    private Vector3 dragOrigin; // Stores the position where dragging started
    private Transform target; // The target object to follow

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
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position;
            targetPosition.z = -10f; // Ensure the camera stays at the correct Z position
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            HandleCameraZoom();
        } else {
            HandleCameraMovement();
            HandleCameraZoom();  
            HandleCameraDrag();      
        } 
    }

    public void FocusOnTarget(GameObject target)
    {
        this.target = target.transform;
        StartCoroutine(ZoomInOnTarget());
    }

    public void StopFollowing()
    {
        target = null;
    }

    private void HandleCameraMovement() 
    {   
        // Camera movement (using arrow keys or WASD)
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + new Vector3(moveX, moveY, 0);

        mainCamera.transform.position = newPosition;
    }

    private void HandleCameraZoom() 
    {
        // Camera zoom (using mouse scroll wheel)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        mainCamera.orthographicSize -= scroll * zoomSpeed;
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
    }

    private void HandleCameraDrag() 
    {
        // Check if the cursor is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Do not process drag logic if the cursor is over a UI element
        }
        
        // Start dragging with left or middle mouse button
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Texture2D dragCursor = Resources.Load<Texture2D>("ic_drag");
            Cursor.SetCursor(dragCursor, Vector2.zero, CursorMode.ForceSoftware); // Change cursor to hand
        }

        // // Continue dragging
        if (Input.GetMouseButton(0) || Input.GetMouseButton(2))
        {
            Vector3 currentMousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentMousePosition;

            // Move the camera opposite to the drag direction
            Vector3 newPosition = transform.position + difference;

            mainCamera.transform.position = newPosition;
        }

        // Stop dragging when the button is released
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset cursor to default
        }
    }

    private IEnumerator ZoomInOnTarget()
    {
        Camera mainCamera = Camera.main;
        while (mainCamera.orthographicSize > 8f)
        {
            mainCamera.orthographicSize -= zoomSpeed * Time.deltaTime;
            yield return null;
        }
    }
}  