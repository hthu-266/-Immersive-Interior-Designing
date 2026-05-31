using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FurniturePreviewRenderer : MonoBehaviour
{
    const string PreviewCameraName = "PreviewCamera";
    const string PreviewRootName = "Furniture Preview Render Root";
    const string PreviewLightName = "Furniture Preview Light";

    public Camera previewCamera;
    public RenderTexture renderTexture;
    [Min(32)] public int textureSize = 160;
    public Color backgroundColor = new Color(0.07f, 0.08f, 0.09f, 0f);
    public Vector3 cameraEulerAngles = new Vector3(24f, -35f, 0f);
    public Vector3 prefabEulerAngles = new Vector3(0f, 35f, 0f);
    [Min(1f)] public float orthographicPadding = 1.35f;

    readonly Dictionary<GameObject, Texture2D> previewCache = new Dictionary<GameObject, Texture2D>();
    RenderTexture runtimeRenderTexture;
    Transform previewRoot;
    Light previewLight;
    bool createdCamera;

    public static FurniturePreviewRenderer FindOrCreate()
    {
        FurniturePreviewRenderer renderer = FindFirstObjectByType<FurniturePreviewRenderer>();
        if (renderer != null)
        {
            renderer.ResolvePreviewObjects();
            return renderer;
        }

        GameObject rendererObject = new GameObject("Furniture Preview Renderer");
        renderer = rendererObject.AddComponent<FurniturePreviewRenderer>();
        renderer.ResolvePreviewObjects();
        return renderer;
    }

    void Awake()
    {
        ResolvePreviewObjects();
    }

    void OnDestroy()
    {
        foreach (Texture2D texture in previewCache.Values)
        {
            DestroyUnityObject(texture);
        }

        previewCache.Clear();

        if (runtimeRenderTexture != null)
        {
            runtimeRenderTexture.Release();
            DestroyUnityObject(runtimeRenderTexture);
        }

        if (createdCamera && previewCamera != null)
        {
            DestroyUnityObject(previewCamera.gameObject);
        }

        if (previewRoot != null)
        {
            DestroyUnityObject(previewRoot.gameObject);
        }
    }

    public Texture2D GetPreview(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        if (previewCache.TryGetValue(prefab, out Texture2D cachedPreview) && cachedPreview != null)
        {
            return cachedPreview;
        }

        Texture2D preview = RenderPrefabPreview(prefab);
        if (preview != null)
        {
            previewCache[prefab] = preview;
        }

        return preview;
    }

    Texture2D RenderPrefabPreview(GameObject prefab)
    {
        ResolvePreviewObjects();

        if (previewCamera == null)
        {
            return null;
        }

        RenderTexture targetTexture = GetTargetTexture();
        if (targetTexture == null)
        {
            return null;
        }

        GameObject previewObject = Instantiate(prefab, previewRoot);
        previewObject.name = prefab.name + " Preview";
        previewObject.hideFlags = HideFlags.HideAndDontSave;
        previewObject.AddComponent<FurniturePreviewObject>();
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.Euler(prefabEulerAngles);
        previewObject.transform.localScale = Vector3.one;

        DisableNestedCameras(previewObject);
        ApplyPreviewLayer(previewObject);

        Texture2D preview = null;

        if (TryFramePreviewObject(previewObject, targetTexture))
        {
            preview = CapturePreview(prefab.name, targetTexture);
        }

        previewObject.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(previewObject);
        }
        else
        {
            DestroyImmediate(previewObject);
        }

        return preview;
    }

    void ResolvePreviewObjects()
    {
        ResolvePreviewCamera();
        ResolvePreviewRoot();
        ResolvePreviewLight();
    }

    void ResolvePreviewCamera()
    {
        if (previewCamera == null)
        {
            GameObject cameraObject = GameObject.Find(PreviewCameraName);
            if (cameraObject != null)
            {
                previewCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject(PreviewCameraName);
            previewCamera = cameraObject.AddComponent<Camera>();
            createdCamera = true;
        }

        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.orthographic = true;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 500f;

        int previewLayer = GetPreviewLayer();
        if (previewLayer >= 0)
        {
            previewCamera.cullingMask = 1 << previewLayer;
        }
    }

    void ResolvePreviewRoot()
    {
        if (previewRoot != null)
        {
            return;
        }

        GameObject rootObject = GameObject.Find(PreviewRootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(PreviewRootName);
        }

        rootObject.hideFlags = HideFlags.HideAndDontSave;
        if (rootObject.GetComponent<FurniturePreviewObject>() == null)
        {
            rootObject.AddComponent<FurniturePreviewObject>();
        }

        previewRoot = rootObject.transform;
        previewRoot.position = new Vector3(10000f, 10000f, 10000f);
        previewRoot.rotation = Quaternion.identity;
        previewRoot.localScale = Vector3.one;
        ApplyPreviewLayer(rootObject);
    }

    void ResolvePreviewLight()
    {
        if (previewLight != null)
        {
            return;
        }

        Transform lightTransform = previewRoot != null
            ? previewRoot.Find(PreviewLightName)
            : null;

        if (lightTransform != null)
        {
            previewLight = lightTransform.GetComponent<Light>();
        }

        if (previewLight == null && previewRoot != null)
        {
            GameObject lightObject = new GameObject(PreviewLightName);
            lightObject.transform.SetParent(previewRoot, false);
            previewLight = lightObject.AddComponent<Light>();
        }

        if (previewLight == null)
        {
            return;
        }

        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);
        ApplyPreviewLayer(previewLight.gameObject);
    }

    RenderTexture GetTargetTexture()
    {
        if (renderTexture != null)
        {
            return renderTexture;
        }

        if (runtimeRenderTexture == null ||
            runtimeRenderTexture.width != textureSize ||
            runtimeRenderTexture.height != textureSize)
        {
            if (runtimeRenderTexture != null)
            {
                runtimeRenderTexture.Release();
                DestroyUnityObject(runtimeRenderTexture);
            }

            runtimeRenderTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32)
            {
                name = "Runtime Furniture Preview RT",
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 4
            };
            runtimeRenderTexture.Create();
        }

        return runtimeRenderTexture;
    }

    bool TryFramePreviewObject(GameObject previewObject, RenderTexture targetTexture)
    {
        if (!TryGetRendererBounds(previewObject, out Bounds bounds))
        {
            return false;
        }

        Vector3 centerOffset = previewRoot.position - bounds.center;
        previewObject.transform.position += centerOffset;

        if (!TryGetRendererBounds(previewObject, out bounds))
        {
            return false;
        }

        Quaternion cameraRotation = Quaternion.Euler(cameraEulerAngles);
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
        float distance = Mathf.Max(radius * 3f, 1f);

        previewCamera.transform.SetPositionAndRotation(
            center - cameraRotation * Vector3.forward * distance,
            cameraRotation);

        previewCamera.aspect = targetTexture.width / (float)targetTexture.height;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = CalculateOrthographicSize(bounds, previewCamera) * orthographicPadding;

        if (previewLight != null)
        {
            previewLight.transform.rotation = cameraRotation * Quaternion.Euler(35f, 25f, 0f);
        }

        return true;
    }

    Texture2D CapturePreview(string prefabName, RenderTexture targetTexture)
    {
        RenderTexture previousActiveTexture = RenderTexture.active;
        RenderTexture previousCameraTarget = previewCamera.targetTexture;

        previewCamera.targetTexture = targetTexture;
        RenderTexture.active = targetTexture;
        previewCamera.Render();

        Texture2D texture = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, false)
        {
            name = prefabName + " Preview",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };

        texture.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        texture.Apply();

        previewCamera.targetTexture = previousCameraTarget;
        RenderTexture.active = previousActiveTexture;

        return texture;
    }

    float CalculateOrthographicSize(Bounds bounds, Camera camera)
    {
        Matrix4x4 worldToCamera = camera.worldToCameraMatrix;
        float halfHeight = 0f;
        float halfWidth = 0f;

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

                    Vector3 cameraPoint = worldToCamera.MultiplyPoint3x4(corner);
                    halfWidth = Mathf.Max(halfWidth, Mathf.Abs(cameraPoint.x));
                    halfHeight = Mathf.Max(halfHeight, Mathf.Abs(cameraPoint.y));
                }
            }
        }

        return Mathf.Max(halfHeight, halfWidth / Mathf.Max(camera.aspect, 0.01f), 0.25f);
    }

    bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.transform.position, Vector3.one);
        bool initialized = false;

        foreach (Renderer renderer in renderers)
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

        return initialized;
    }

    void DisableNestedCameras(GameObject root)
    {
        Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
        foreach (Camera camera in cameras)
        {
            camera.enabled = false;
        }

        AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = false;
        }
    }

    void ApplyPreviewLayer(GameObject root)
    {
        int previewLayer = GetPreviewLayer();
        if (previewLayer < 0)
        {
            return;
        }

        ApplyLayerRecursively(root.transform, previewLayer);
    }

    void ApplyLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            ApplyLayerRecursively(child, layer);
        }
    }

    int GetPreviewLayer()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer >= 0 ? uiLayer : 0;
    }

    void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}

public sealed class FurniturePreviewObject : MonoBehaviour
{
}
