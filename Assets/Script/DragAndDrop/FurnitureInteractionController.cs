using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class FurnitureInteractionController : MonoBehaviour
{
    [Header("References")]
    public Camera interactionCamera;
    public FloorController floorController;
    public TMP_Text statusText;

    [Header("Input")]
    public LayerMask furnitureLayer = ~0;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;
    public KeyCode cancelKey = KeyCode.Escape;
    public KeyCode snapToggleKey = KeyCode.G;
    public float nudgeStep = 0.25f;
    public float maxRayDistance = 200f;

    [Header("Registration")]
    public bool autoRegisterSceneFurniture = true;
    public bool onlyRegisterCartoonMaterialObjects = true;
    public bool autoAssignFurnitureLayer = true;
    public bool createSelectionProxies = true;

    [Header("Selection")]
    [Min(0f)] public float screenPickRadius = 28f;
    [Min(0f)] public float selectedFurniturePickBias = 0.35f;

    [Header("Drag")]
    [Min(0f)] public float dragResponsiveness = 0f;
    [Min(0f)] public float maxDragSpeed = 0f;

    readonly RaycastHit[] pickHits = new RaycastHit[64];
    static int activeDragControllerCount;

    MovableFurniture selectedFurniture;
    bool isDragging;
    Plane dragPlane;
    Vector3 dragGrabOffset;
    Vector3 dragTargetPosition;

    public event Action<MovableFurniture> SelectionChanged;

    public MovableFurniture SelectedFurniture => selectedFurniture;
    public bool IsDragging => isDragging;
    public static bool AnyFurnitureDragActive => activeDragControllerCount > 0;

    void Awake()
    {
        ResolveReferences();

        if (furnitureLayer.value == ~0)
        {
            furnitureLayer = LayerMask.GetMask("Furniture");
        }
    }

    void Start()
    {
        ResolveReferences();

        if (autoRegisterSceneFurniture)
        {
            RegisterSceneFurniture();
        }

        ClampAllFurnitureToRoom();
        UpdateStatus();
    }

    void Update()
    {
        ResolveReferences();
        HandleMouseInput();
        HandleKeyboardInput();
    }

    void OnDisable()
    {
        SetDragging(false);

        if (selectedFurniture != null)
        {
            selectedFurniture.EndMove();
        }
    }

    public void RegisterSceneFurniture()
    {
        int furnitureLayerIndex = GetFurnitureLayerIndex();
        HashSet<MovableFurniture> registeredFurniture = new HashSet<MovableFurniture>();
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            if (!IsFurnitureRenderer(renderer))
            {
                continue;
            }

            MovableFurniture movable = renderer.GetComponentInParent<MovableFurniture>();
            if (movable == null)
            {
                movable = renderer.gameObject.AddComponent<MovableFurniture>();
            }

            if (movable.GetComponent<FurnitureID>() == null)
            {
                movable.gameObject.AddComponent<FurnitureID>();
            }
            
            if (registeredFurniture.Add(movable))
            {
                movable.EnsureInteractionSetup(
                    furnitureLayerIndex,
                    autoAssignFurnitureLayer,
                    createSelectionProxies);
            }
        }
    }

    public void Select(MovableFurniture furniture)
    {
        if (selectedFurniture == furniture)
        {
            return;
        }

        if (selectedFurniture != null)
        {
            selectedFurniture.SetSelected(false);
            selectedFurniture.EndMove();
        }

        SetDragging(false);
        selectedFurniture = furniture;

        if (selectedFurniture != null)
        {
            selectedFurniture.SetSelected(true);
        }

        SelectionChanged?.Invoke(selectedFurniture);
        UpdateStatus();
    }

    public void ClearSelection()
    {
        Select(null);
    }

    public void RotateSelected(float degrees)
    {
        if (selectedFurniture == null)
        {
            return;
        }

        selectedFurniture.Rotate(degrees);
        ClampSelectedToRoom();
        UpdateStatus();
    }

    public void ClampAllFurnitureToRoom()
    {
        if (floorController == null)
        {
            return;
        }

        MovableFurniture[] furnitureItems = FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None);
        foreach (MovableFurniture item in furnitureItems)
        {
            item.MoveTo(item.transform.position, floorController);
        }
    }

    void ResolveReferences()
    {
        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        Camera activeCamera = ResolveActiveCamera();
        if (activeCamera != null)
        {
            interactionCamera = activeCamera;
        }
    }

    Camera ResolveActiveCamera()
    {
        Camera bestCamera = null;
        float bestScore = float.NegativeInfinity;
        Camera[] cameras = Camera.allCameras;

        foreach (Camera camera in cameras)
        {
            if (camera == null || !camera.isActiveAndEnabled)
            {
                continue;
            }

            float score = camera.depth;
            if (camera.targetDisplay == 0)
            {
                score += 100f;
            }

            if (camera.CompareTag("MainCamera"))
            {
                score += 0.1f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCamera = camera;
            }
        }

        return bestCamera != null ? bestCamera : FindFirstObjectByType<Camera>();
    }

    void HandleMouseInput()
    {
        if (interactionCamera == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            if (TryPickFurniture(out MovableFurniture furniture))
            {
                Select(furniture);
                BeginDrag();
            }
            else
            {
                ClearSelection();
            }
        }

        if (isDragging && selectedFurniture != null && Input.GetMouseButton(0))
        {
            DragSelectedFurniture();
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(cancelKey))
        {
            ClearSelection();
            return;
        }

        if (selectedFurniture == null)
        {
            return;
        }

        if (Input.GetKeyDown(rotateLeftKey))
        {
            RotateSelected(-selectedFurniture.rotationStep);
        }

        if (Input.GetKeyDown(rotateRightKey))
        {
            RotateSelected(selectedFurniture.rotationStep);
        }

        if (Input.GetKeyDown(snapToggleKey))
        {
            selectedFurniture.snapToGrid = !selectedFurniture.snapToGrid;
            UpdateStatus();
        }

        Vector3 nudge = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            nudge.z += nudgeStep;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            nudge.z -= nudgeStep;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            nudge.x += nudgeStep;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            nudge.x -= nudgeStep;
        }

        if (nudge != Vector3.zero)
        {
            selectedFurniture.Nudge(nudge, floorController);
            UpdateStatus();
        }
    }

    bool TryPickFurniture(out MovableFurniture furniture)
    {
        furniture = null;

        Ray ray = BuildPointerRay();
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            pickHits,
            maxRayDistance,
            GetEffectiveFurnitureMask(),
            QueryTriggerInteraction.Collide);

        MovableFurniture bestFurniture = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = pickHits[i];
            MovableFurniture candidate = hit.collider != null
                ? hit.collider.GetComponentInParent<MovableFurniture>()
                : null;

            if (!IsSelectable(candidate))
            {
                continue;
            }

            float score = ScoreRaycastCandidate(candidate, hit);
            if (score < bestScore)
            {
                bestScore = score;
                bestFurniture = candidate;
            }
        }

        if (bestFurniture != null)
        {
            furniture = bestFurniture;
            return true;
        }

        return TryPickFurnitureFromScreenBounds(out furniture);
    }

    void BeginDrag()
    {
        if (selectedFurniture == null)
        {
            return;
        }

        dragPlane = new Plane(Vector3.up, selectedFurniture.transform.position);
        dragTargetPosition = selectedFurniture.transform.position;
        dragGrabOffset = Vector3.zero;

        if (TryGetDragPoint(out Vector3 dragPoint))
        {
            dragGrabOffset = selectedFurniture.transform.position - dragPoint;
            dragGrabOffset.y = 0f;
        }

        SetDragging(true);
        selectedFurniture.BeginMove();
    }

    void DragSelectedFurniture()
    {
        if (!TryGetDragPoint(out Vector3 dragPoint))
        {
            return;
        }

        dragTargetPosition = dragPoint + dragGrabOffset;
        dragTargetPosition.y = selectedFurniture.transform.position.y;

        if (floorController != null)
        {
            dragTargetPosition = floorController.ClampPointToRoom(
                dragTargetPosition,
                selectedFurniture.GetPlacementPadding());
        }

        Vector3 currentPosition = selectedFurniture.transform.position;
        Vector3 nextPosition = dragResponsiveness <= 0f
            ? dragTargetPosition
            : Vector3.Lerp(
                currentPosition,
                dragTargetPosition,
                1f - Mathf.Exp(-dragResponsiveness * Time.deltaTime));

        if (maxDragSpeed > 0f)
        {
            nextPosition = Vector3.MoveTowards(
                currentPosition,
                nextPosition,
                maxDragSpeed * Mathf.Max(Time.deltaTime, 0.0001f));
        }

        selectedFurniture.MoveTo(nextPosition, floorController);
        UpdateStatus();
    }

    void EndDrag()
    {
        SetDragging(false);

        if (selectedFurniture != null)
        {
            selectedFurniture.EndMove();
            ClampSelectedToRoom();
        }
    }

    void SetDragging(bool dragging)
    {
        if (isDragging == dragging)
        {
            return;
        }

        isDragging = dragging;
        activeDragControllerCount += dragging ? 1 : -1;
        activeDragControllerCount = Mathf.Max(0, activeDragControllerCount);
    }

    void ClampSelectedToRoom()
    {
        if (selectedFurniture != null)
        {
            selectedFurniture.MoveTo(selectedFurniture.transform.position, floorController);
        }
    }

    bool TryGetFloorPoint(out Vector3 point)
    {
        point = Vector3.zero;
        float floorY = floorController != null ? floorController.transform.position.y : 0f;
        Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
        Ray ray = BuildPointerRay();

        if (!floorPlane.Raycast(ray, out float distance))
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return true;
    }

    bool TryGetDragPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = BuildPointerRay();

        if (!dragPlane.Raycast(ray, out float distance))
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return true;
    }

    Ray BuildPointerRay()
    {
        return interactionCamera.ScreenPointToRay(Input.mousePosition);
    }

    bool IsPointerOverUI()
    {
        return Cursor.lockState != CursorLockMode.Locked
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    bool IsFurnitureRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled)
        {
            return false;
        }

        if (renderer.GetComponentInParent<FloorController>() != null ||
            renderer.GetComponentInParent<RoomBoundary>() != null ||
            renderer.GetComponentInParent<Canvas>() != null)
        {
            return false;
        }

        string objectName = renderer.gameObject.name.ToLowerInvariant();
        if (objectName.Contains("wall") ||
            objectName.Contains("floor") ||
            objectName.Contains("camera") ||
            objectName.Contains("light") ||
            objectName.Contains("volume"))
        {
            return false;
        }

        if (renderer.GetComponent<MeshFilter>() == null && renderer.GetComponent<SkinnedMeshRenderer>() == null)
        {
            return false;
        }

        if (!onlyRegisterCartoonMaterialObjects)
        {
            return true;
        }

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (material.name.Contains("Cartoon_Mat") ||
                material.name.Contains("M_LowPolyLivingRoom") ||
                material.name.Contains("(Mat)Gradient"))
            {
                return true;
            }
        }

        return renderer.GetComponentInParent<MovableFurniture>() != null;
    }

    bool IsSelectable(MovableFurniture furniture)
    {
        return furniture != null && furniture.isMovable && furniture.isActiveAndEnabled;
    }

    float ScoreRaycastCandidate(MovableFurniture candidate, RaycastHit hit)
    {
        float score = hit.distance;
        Vector3 screenCenter = interactionCamera.WorldToScreenPoint(candidate.GetWorldBounds().center);

        if (screenCenter.z > 0f)
        {
            Vector2 center = new Vector2(screenCenter.x, screenCenter.y);
            score += Vector2.Distance(Input.mousePosition, center) * 0.0025f;
        }

        if (candidate == selectedFurniture)
        {
            score -= selectedFurniturePickBias;
        }

        return score;
    }

    bool TryPickFurnitureFromScreenBounds(out MovableFurniture furniture)
    {
        furniture = null;

        if (screenPickRadius <= 0f)
        {
            return false;
        }

        Vector2 mousePosition = Input.mousePosition;
        MovableFurniture[] furnitureItems = FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None);
        float bestScore = float.PositiveInfinity;

        foreach (MovableFurniture candidate in furnitureItems)
        {
            if (!IsSelectable(candidate) ||
                !TryGetScreenBounds(candidate.GetWorldBounds(), out Rect screenBounds, out float depth))
            {
                continue;
            }

            Rect expandedBounds = Expand(screenBounds, screenPickRadius);
            if (!expandedBounds.Contains(mousePosition))
            {
                continue;
            }

            float score = DistanceToRect(mousePosition, screenBounds) + depth * 0.01f;
            if (candidate == selectedFurniture)
            {
                score -= selectedFurniturePickBias;
            }

            if (score < bestScore)
            {
                bestScore = score;
                furniture = candidate;
            }
        }

        return furniture != null;
    }

    bool TryGetScreenBounds(Bounds bounds, out Rect screenBounds, out float nearestDepth)
    {
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        nearestDepth = float.PositiveInfinity;
        bool found = false;

        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? bounds.min.x : bounds.max.x,
                        y == 0 ? bounds.min.y : bounds.max.y,
                        z == 0 ? bounds.min.z : bounds.max.z);

                    Vector3 screenPoint = interactionCamera.WorldToScreenPoint(corner);
                    if (screenPoint.z <= 0f)
                    {
                        continue;
                    }

                    min = Vector2.Min(min, screenPoint);
                    max = Vector2.Max(max, screenPoint);
                    nearestDepth = Mathf.Min(nearestDepth, screenPoint.z);
                    found = true;
                }
            }
        }

        screenBounds = found ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
        return found;
    }

    Rect Expand(Rect rect, float amount)
    {
        rect.xMin -= amount;
        rect.xMax += amount;
        rect.yMin -= amount;
        rect.yMax += amount;
        return rect;
    }

    float DistanceToRect(Vector2 point, Rect rect)
    {
        float dx = point.x < rect.xMin ? rect.xMin - point.x : Mathf.Max(0f, point.x - rect.xMax);
        float dy = point.y < rect.yMin ? rect.yMin - point.y : Mathf.Max(0f, point.y - rect.yMax);
        return new Vector2(dx, dy).magnitude;
    }

    int GetEffectiveFurnitureMask()
    {
        return furnitureLayer.value == 0 ? ~0 : furnitureLayer.value;
    }

    int GetFurnitureLayerIndex()
    {
        int mask = furnitureLayer.value;
        if (mask == 0 || (mask & (mask - 1)) != 0)
        {
            return -1;
        }

        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                return i;
            }
        }

        return -1;
    }

    void UpdateStatus()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = selectedFurniture == null
            ? "No furniture selected"
            : "Selected: " + selectedFurniture.DisplayName;
    }
}
