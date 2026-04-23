using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;

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


    [Header("Zoom Settings")]
    public float zoomSensitivity = 5f;
    // Zoom cho góc nhìn thứ nhất (thay đổi FOV)
    public float minFOV = 30f;
    public float maxFOV = 90f;
    // Zoom cho Top-down (thay đổi độ cao/offset)
    public float minHeight = 3f;
    public float maxHeight = 20f;
    Camera firstPersonCamera;
    Camera topDownCamera;
    bool isTopDownView;
    Vector2 velocity;
    Vector2 frameVelocity;
    // Một biến để tách input
    FirstPersonMovement movementScript;


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
        movementScript = GetComponentInParent<FirstPersonMovement>(); // ✅ THÊM

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
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Không lock mà update chuột để đổi mode cam
        UpdateCursorState();
    }

    void Update()
    {
        HandleViewToggle();
        HandleZoom(); // Gọi hàm xử lý zoom mỗi khung hình

        // nếu đang top-down thì KHÔNG xoay camera
        if (isTopDownView) return;

        // nếu đang click UI thì KHÔNG xoay camera
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return; 
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

    // --- HÀM XỬ LÝ ZOOM MỚI ---
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;

        if (isTopDownView)
        {
            // Zoom Top-down bằng cách thay đổi độ cao của offset (trục Y)
            // và tỉ lệ thuận trục Z để giữ góc nhìn ổn định
            float zoomAmount = scroll * zoomSensitivity;
            float currentHeight = topDownOffset.y;
            float newHeight = Mathf.Clamp(currentHeight - zoomAmount, minHeight, maxHeight);
            
            // Tính toán tỉ lệ để lùi camera ra xa khi lên cao
            float ratio = newHeight / currentHeight;
            topDownOffset.y = newHeight;
            topDownOffset.z *= ratio; 
        }
        else
        {
            // Zoom First Person bằng cách thay đổi Field of View
            float currentFOV = firstPersonCamera.fieldOfView;
            firstPersonCamera.fieldOfView = Mathf.Clamp(currentFOV - (scroll * zoomSensitivity * 10), minFOV, maxFOV);
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
        UpdateCursorState(); // cập nhật chuột khi đổi mode
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

            // ✅ THÊM: bật/tắt movement theo mode
        if (movementScript != null)
        {
            movementScript.enabled = !isTopDownView;
        }
    }

        void UpdateCursorState()
    {
        if (isTopDownView)
        {
            Cursor.lockState = CursorLockMode.None; // chuột tự do
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // khóa chuột
            Cursor.visible = false;
        }
    }
}
