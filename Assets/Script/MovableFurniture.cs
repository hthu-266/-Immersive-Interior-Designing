using UnityEngine;

[DisallowMultipleComponent]
public class MovableFurniture : MonoBehaviour
{
    [Header("Placement")]
    public bool isMovable = true;
    public bool snapToGrid = false;
    public float gridSize = 0.25f;
    public float boundaryPadding = 0.15f;
    public float rotationStep = 15f;

    [Header("Selection")]
    public Color selectedTint = new Color(0.75f, 0.95f, 1f, 1f);

    Renderer[] cachedRenderers;
    Rigidbody cachedRigidbody;
    bool wasKinematic;
    bool isSelected;
    MaterialPropertyBlock selectedBlock;

    public string DisplayName => string.IsNullOrWhiteSpace(gameObject.name) ? "Furniture" : gameObject.name;

    void Awake()
    {
        CacheComponents();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
        {
            return;
        }

        isSelected = selected;
        ApplySelectionState();
    }

    public void BeginMove()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        if (cachedRigidbody == null)
        {
            return;
        }

        wasKinematic = cachedRigidbody.isKinematic;
        cachedRigidbody.isKinematic = true;
    }

    public void EndMove()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        cachedRigidbody.isKinematic = wasKinematic;
    }

    public void MoveTo(Vector3 worldPosition, FloorController floorController)
    {
        if (!isMovable)
        {
            return;
        }

        worldPosition.y = transform.position.y;

        if (snapToGrid)
        {
            worldPosition.x = Snap(worldPosition.x);
            worldPosition.z = Snap(worldPosition.z);
        }

        if (floorController != null)
        {
            worldPosition = floorController.ClampPointToRoom(worldPosition, GetPlacementPadding());
        }

        transform.position = worldPosition;
    }

    public void Nudge(Vector3 delta, FloorController floorController)
    {
        MoveTo(transform.position + delta, floorController);
    }

    public void Rotate(float degrees)
    {
        if (!isMovable)
        {
            return;
        }

        transform.Rotate(Vector3.up, degrees, Space.World);
    }

    public float GetPlacementPadding()
    {
        Bounds bounds = GetWorldBounds();
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        return radius + boundaryPadding;
    }

    public void EnsureCollider()
    {
        if (GetComponentInChildren<MeshCollider>() != null)
        {
            return;
        }

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            MeshCollider meshCollider =
                meshFilter.gameObject.GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider =
                    meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
        }
    }

    void CacheComponents()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    void ApplySelectionState()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheComponents();
        }

        if (selectedBlock == null)
        {
            selectedBlock = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!isSelected)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            selectedBlock.Clear();
            selectedBlock.SetColor("_BaseColor", selectedTint);
            selectedBlock.SetColor("_Color", selectedTint);
            renderer.SetPropertyBlock(selectedBlock);
        }
    }

    Bounds GetWorldBounds()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheComponents();
        }

        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool initialized = false;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    float Snap(float value)
    {
        float size = Mathf.Max(0.05f, gridSize);
        return Mathf.Round(value / size) * size;
    }

    float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / Mathf.Abs(divisor);
    }
}
