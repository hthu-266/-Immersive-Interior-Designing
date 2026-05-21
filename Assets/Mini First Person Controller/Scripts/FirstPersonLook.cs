using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;

    public float sensitivity = 2;
    public float smoothing = 1.5f;

    [Header("View Switching")]
    public KeyCode switchViewKey = KeyCode.V;
    public bool startInTopDownView = false;
    public Vector3 topDownOffset = new Vector3(0f, 18f, 0f);
    public Vector3 topDownEulerAngles = new Vector3(90f, 0f, 0f);
    public float topDownFieldOfView = 60f;

    [Header("Top Down View")]
    public bool useOrthographicTopDown = true;
    public bool fitRoomOnFirstTopDownView = true;
    public float topDownOrthographicSize = 8f;
    public float minTopDownOrthographicSize = 2f;
    public float maxTopDownOrthographicSize = 30f;
    public float roomFitPadding = 1f;

    [Header("Zoom Settings")]
    public float zoomSensitivity = 5f;
    public float minFOV = 30f;
    public float maxFOV = 90f;
    public float minHeight = 3f;
    public float maxHeight = 30f;

    [Header("Furniture Interaction")]
    public bool freezeLookWhileDraggingFurniture = true;

    Camera firstPersonCamera;
    Camera topDownCamera;
    bool isTopDownView;
    bool hasFittedTopDownSize;
    Vector2 velocity;
    Vector2 frameVelocity;
    FirstPersonMovement movementScript;
    FloorController floorController;

    void Reset()
    {
        FirstPersonMovement movement = GetComponentInParent<FirstPersonMovement>();
        if (movement != null)
        {
            character = movement.transform;
        }
    }

    void Awake()
    {
        firstPersonCamera = GetComponent<Camera>();
        movementScript = GetComponentInParent<FirstPersonMovement>();
        floorController = FindFirstObjectByType<FloorController>();

        if (character == null)
        {
            FirstPersonMovement movement = GetComponentInParent<FirstPersonMovement>();
            if (movement != null)
            {
                character = movement.transform;
            }
        }

        CreateTopDownCameraIfNeeded();
        isTopDownView = startInTopDownView;
        ApplyViewState();
    }

    void Start()
    {
        if (isTopDownView && fitRoomOnFirstTopDownView)
        {
            FitTopDownSizeToRoom();
            hasFittedTopDownSize = true;
            ApplyTopDownProjection();
            ApplyTopDownCameraTransform();
        }

        UpdateCursorState();
    }

    void Update()
    {
        HandleViewToggle();
        HandleZoom();

        if (isTopDownView)
        {
            return;
        }

        if (freezeLookWhileDraggingFurniture && FurnitureInteractionController.AnyFurnitureDragActive)
        {
            frameVelocity = Vector2.zero;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        if (character == null)
        {
            return;
        }

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    void LateUpdate()
    {
        ApplyTopDownCameraTransform();
    }

    void OnDisable()
    {
        if (firstPersonCamera != null)
        {
            firstPersonCamera.enabled = true;
        }

        if (topDownCamera != null)
        {
            topDownCamera.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (topDownCamera == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(topDownCamera.gameObject);
        }
        else
        {
            DestroyImmediate(topDownCamera.gameObject);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f || IsPointerOverUI())
        {
            return;
        }

        if (isTopDownView)
        {
            if (useOrthographicTopDown && topDownCamera != null)
            {
                topDownOrthographicSize = Mathf.Clamp(
                    topDownOrthographicSize - scroll * zoomSensitivity,
                    minTopDownOrthographicSize,
                    maxTopDownOrthographicSize);
                topDownCamera.orthographicSize = topDownOrthographicSize;
            }
            else
            {
                float newHeight = Mathf.Clamp(topDownOffset.y - scroll * zoomSensitivity, minHeight, maxHeight);
                topDownOffset = new Vector3(0f, newHeight, 0f);
            }

            ApplyTopDownCameraTransform();
            return;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.fieldOfView = Mathf.Clamp(
                firstPersonCamera.fieldOfView - scroll * zoomSensitivity * 10f,
                minFOV,
                maxFOV);
        }
    }

    void HandleViewToggle()
    {
        if (topDownCamera == null || !Input.GetKeyDown(switchViewKey))
        {
            return;
        }

        isTopDownView = !isTopDownView;
        ApplyViewState();
        UpdateCursorState();
    }

    void CreateTopDownCameraIfNeeded()
    {
        if (topDownCamera != null || firstPersonCamera == null)
        {
            return;
        }

        GameObject topDownCameraObject = new GameObject("Top Down Camera");
        topDownCameraObject.tag = "Untagged";

        topDownCamera = topDownCameraObject.AddComponent<Camera>();
        if (!topDownCameraObject.TryGetComponent<UniversalAdditionalCameraData>(out _))
        {
            topDownCameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        topDownCamera.CopyFrom(firstPersonCamera);
        topDownCamera.depth = firstPersonCamera.depth + 10f;
        topDownCamera.enabled = false;

        ApplyTopDownProjection();
        ApplyTopDownCameraTransform();
    }

    void ApplyViewState()
    {
        if (firstPersonCamera == null)
        {
            return;
        }

        firstPersonCamera.enabled = !isTopDownView;

        if (topDownCamera != null)
        {
            if (isTopDownView && fitRoomOnFirstTopDownView && !hasFittedTopDownSize)
            {
                FitTopDownSizeToRoom();
                hasFittedTopDownSize = true;
            }

            ApplyTopDownProjection();
            ApplyTopDownCameraTransform();
            topDownCamera.enabled = isTopDownView;
        }

        if (movementScript != null)
        {
            movementScript.enabled = !isTopDownView;
        }
    }

    void ApplyTopDownProjection()
    {
        if (topDownCamera == null)
        {
            return;
        }

        topDownCamera.orthographic = useOrthographicTopDown;
        topDownCamera.fieldOfView = topDownFieldOfView;
        topDownCamera.orthographicSize = topDownOrthographicSize;
    }

    void ApplyTopDownCameraTransform()
    {
        if (topDownCamera == null)
        {
            return;
        }

        Vector3 anchor = GetTopDownAnchor();
        float height = Mathf.Clamp(Mathf.Abs(topDownOffset.y), minHeight, maxHeight);
        topDownOffset = new Vector3(0f, height, 0f);

        topDownCamera.transform.position = new Vector3(anchor.x, anchor.y + height, anchor.z);
        topDownCamera.transform.rotation = Quaternion.Euler(90f, topDownEulerAngles.y, 0f);
    }

    Vector3 GetTopDownAnchor()
    {
        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        if (floorController != null)
        {
            Vector3 floorPosition = floorController.transform.position;
            Bounds roomBounds = floorController.GetRoomBounds();
            return new Vector3(roomBounds.center.x, floorPosition.y, roomBounds.center.z);
        }

        if (character != null)
        {
            return character.position;
        }

        return transform.position;
    }

    void FitTopDownSizeToRoom()
    {
        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        if (floorController == null)
        {
            return;
        }

        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
        aspect = Mathf.Max(aspect, 0.01f);
        float fitSize = Mathf.Max(floorController.Length * 0.5f, floorController.Width * 0.5f / aspect) + roomFitPadding;
        topDownOrthographicSize = Mathf.Clamp(fitSize, minTopDownOrthographicSize, maxTopDownOrthographicSize);

        if (topDownCamera != null)
        {
            topDownCamera.orthographicSize = topDownOrthographicSize;
        }
    }

    void UpdateCursorState()
    {
        if (isTopDownView)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    bool IsPointerOverUI()
    {
        return Cursor.lockState != CursorLockMode.Locked
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }
}
