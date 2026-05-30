using UnityEngine;

[DisallowMultipleComponent]
public class MovableFurniture : MonoBehaviour
{
    const string SelectionProxyName = "Furniture Selection Proxy";

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
    BoxCollider selectionProxyCollider;
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

    public void EnsureInteractionSetup(int interactionLayer, bool assignLayer, bool createSelectionProxy)
    {
        CacheComponents();

        if (assignLayer && interactionLayer >= 0)
        {
            ApplyLayerRecursively(transform, interactionLayer);
        }

        EnsureCollider();

        if (createSelectionProxy)
        {
            EnsureSelectionProxy(interactionLayer, assignLayer);
        }
    }

    public void EnsureCollider()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
        }
    }

    public Bounds GetWorldBounds()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheComponents();
        }

        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool initialized = false;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null || !renderer.enabled)
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

    void CacheComponents()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
        cachedRigidbody = GetComponent<Rigidbody>();
        selectionProxyCollider = FindSelectionProxyCollider();
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

    void EnsureSelectionProxy(int interactionLayer, bool assignLayer)
    {
        if (!TryGetLocalRenderBounds(out Bounds localBounds))
        {
            return;
        }

        if (selectionProxyCollider == null)
        {
            selectionProxyCollider = CreateSelectionProxyCollider();
        }

        if (selectionProxyCollider == null)
        {
            return;
        }

        if (assignLayer && interactionLayer >= 0)
        {
            selectionProxyCollider.gameObject.layer = interactionLayer;
        }

        selectionProxyCollider.isTrigger = true;
        selectionProxyCollider.center = localBounds.center;
        selectionProxyCollider.size = new Vector3(
            Mathf.Max(localBounds.size.x, 0.05f),
            Mathf.Max(localBounds.size.y, 0.05f),
            Mathf.Max(localBounds.size.z, 0.05f));
    }

    BoxCollider FindSelectionProxyCollider()
    {
        Transform proxyTransform = transform.Find(SelectionProxyName);
        return proxyTransform != null ? proxyTransform.GetComponent<BoxCollider>() : null;
    }

    BoxCollider CreateSelectionProxyCollider()
    {
        GameObject proxyObject = new GameObject(SelectionProxyName);
        proxyObject.transform.SetParent(transform, false);
        proxyObject.transform.localPosition = Vector3.zero;
        proxyObject.transform.localRotation = Quaternion.identity;
        proxyObject.transform.localScale = Vector3.one;
        return proxyObject.AddComponent<BoxCollider>();
    }

    bool TryGetLocalRenderBounds(out Bounds localBounds)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheComponents();
        }

        localBounds = new Bounds(Vector3.zero, Vector3.one);
        bool initialized = false;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            EncapsulateRendererBounds(renderer.bounds, ref localBounds, ref initialized);
        }

        return initialized;
    }

    void EncapsulateRendererBounds(Bounds worldBounds, ref Bounds localBounds, ref bool initialized)
    {
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3 worldCorner = new Vector3(
                        x == 0 ? worldBounds.min.x : worldBounds.max.x,
                        y == 0 ? worldBounds.min.y : worldBounds.max.y,
                        z == 0 ? worldBounds.min.z : worldBounds.max.z);
                    Vector3 localCorner = transform.InverseTransformPoint(worldCorner);

                    if (!initialized)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }
        }
    }

    void ApplyLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            ApplyLayerRecursively(child, layer);
        }
    }

    float Snap(float value)
    {
        float size = Mathf.Max(0.05f, gridSize);
        return Mathf.Round(value / size) * size;
    }
}
