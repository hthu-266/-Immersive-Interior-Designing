using UnityEngine;
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
    public Vector3 topDownOffset = new Vector3(0f, 8f, -6f);
    public Vector3 topDownEulerAngles = new Vector3(55f, 0f, 0f);
    public float topDownFieldOfView = 60f;

    Camera firstPersonCamera;
    Camera topDownCamera;
    bool isTopDownView;
    Vector2 velocity;
    Vector2 frameVelocity;


    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        FirstPersonMovement movement = GetComponentInParent<FirstPersonMovement>();
        if (movement != null)
        {
            character = movement.transform;
        }
    }

    void Awake()
    {
        firstPersonCamera = GetComponent<Camera>();

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
        // Lock the mouse cursor to the game screen.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleViewToggle();

        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        if (character == null)
        {
            return;
        }

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    void LateUpdate()
    {
        if (topDownCamera == null)
        {
            return;
        }

        Vector3 anchorPosition = character != null ? character.position : transform.position;
        topDownCamera.transform.position = anchorPosition + topDownOffset;
        topDownCamera.transform.rotation = Quaternion.Euler(topDownEulerAngles);
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

    void HandleViewToggle()
    {
        if (topDownCamera == null || !Input.GetKeyDown(switchViewKey))
        {
            return;
        }

        isTopDownView = !isTopDownView;
        ApplyViewState();
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
        topDownCamera.fieldOfView = topDownFieldOfView;
        topDownCamera.enabled = false;

        Vector3 anchorPosition = character != null ? character.position : transform.position;
        topDownCamera.transform.position = anchorPosition + topDownOffset;
        topDownCamera.transform.rotation = Quaternion.Euler(topDownEulerAngles);
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
            topDownCamera.enabled = isTopDownView;
        }
    }
}
