using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class FurnitureInteractionController : MonoBehaviour
{
    [Header("References")]
    public Camera interactionCamera;
    public FloorController floorController;
    public TMP_Text statusText;

    [Header("Input")]

    public LayerMask furnitureLayer  = ~0;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;
    public KeyCode cancelKey = KeyCode.Escape;
    public KeyCode snapToggleKey = KeyCode.G;
    public float nudgeStep = 0.25f;
    public float maxRayDistance = 200f;


    [Header("Registration")]
    public bool autoRegisterSceneFurniture = true;
    public bool onlyRegisterCartoonMaterialObjects = true;

    MovableFurniture selectedFurniture;
    bool isDragging;
    Vector3 dragOffset;
    Plane dragPlane;

    public event Action<MovableFurniture> SelectionChanged;

    public MovableFurniture SelectedFurniture => selectedFurniture;

    void Awake()
    {
        ResolveReferences();

        if (furnitureLayer == ~0)
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

    public void RegisterSceneFurniture()
    {
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

            movable.EnsureCollider();
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

        selectedFurniture = furniture;

        if (selectedFurniture != null)
        {
            selectedFurniture.SetSelected(true);
        }

        isDragging = false;
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

        if (interactionCamera == null || !interactionCamera.isActiveAndEnabled)
        {
            interactionCamera = Camera.main;

            if (interactionCamera == null)
            {
                interactionCamera = FindFirstObjectByType<Camera>();
            }
        }
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
            if (TryPickFurniture(out MovableFurniture furniture, out RaycastHit hit))
            {
                Select(furniture);
                BeginDrag(hit.point);
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

    bool TryPickFurniture(out MovableFurniture furniture, out RaycastHit hit)
    {
        furniture = null;

        Ray ray = BuildPointerRay();
        if (!Physics.Raycast(ray, out hit, maxRayDistance, furnitureLayer, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        furniture = hit.collider.GetComponentInParent<MovableFurniture>();
        return furniture != null && furniture.isMovable;
    }

    void BeginDrag(Vector3 hitPoint)
    {
        if (selectedFurniture == null)
        {
            return;
        }

        dragPlane = new Plane(
            Vector3.up,
            selectedFurniture.transform.position
        );

        Ray ray = BuildPointerRay();

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 dragPoint = ray.GetPoint(distance);
            dragOffset = selectedFurniture.transform.position - dragPoint;
        }

        isDragging = true;
        selectedFurniture.BeginMove();
    }

    void DragSelectedFurniture()
    {
        Ray ray = BuildPointerRay();

        if (!dragPlane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 dragPoint = ray.GetPoint(distance);
        Vector3 targetPosition = dragPoint + dragOffset;

        selectedFurniture.MoveTo(targetPosition, floorController);

        UpdateStatus();
    }

    void EndDrag()
    {
        isDragging = false;

        if (selectedFurniture != null)
        {
            selectedFurniture.EndMove();
            ClampSelectedToRoom();
        }
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
            if (material != null && material.name.Contains("Cartoon_Mat"))
            {
                return true;
            }
        }

        return renderer.GetComponentInParent<MovableFurniture>() != null;
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
