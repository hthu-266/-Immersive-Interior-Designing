using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FloorController : MonoBehaviour
{
    public enum FloorPattern
    {
        Wood,
        Tile,
        Concrete,
        Carpet
    }

    [Serializable]
    public class FloorTexturePreset
    {
        public string displayName = "Wood";
        public FloorPattern pattern = FloorPattern.Wood;
        public Color baseColor = new Color(0.58f, 0.39f, 0.22f);
        public Color accentColor = new Color(0.28f, 0.18f, 0.09f);
        [Min(0.25f)] public float repeatSize = 1f;
    }

    const float DefaultPlaneSize = 10f;
    const string WallRootName = "Generated Room Boundaries";

    [Header("Room Size")]
    public float minWidth = 2f;
    public float maxWidth = 40f;
    public float minLength = 2f;
    public float maxLength = 40f;

    [Header("Floor Textures")]
    public List<FloorTexturePreset> floorPresets = new List<FloorTexturePreset>();
    public int currentPresetIndex;

    [Header("Walls")]
    public bool wallsEnabled = true;
    public float wallHeight = 2.6f;
    public float wallThickness = 0.18f;
    public Color wallColor = new Color(0.86f, 0.84f, 0.78f);
    public Material wallMaterial;

    Renderer floorRenderer;
    Material runtimeFloorMaterial;
    Material runtimeWallMaterial;
    Texture2D generatedFloorTexture;
    readonly GameObject[] walls = new GameObject[4];
    Transform wallRoot;

    public event Action RoomChanged;

    public float Width { get; private set; }
    public float Length { get; private set; }
    public int CurrentPresetIndex => currentPresetIndex;
    public string CurrentPresetName => floorPresets.Count == 0 ? "None" : floorPresets[currentPresetIndex].displayName;

    void Awake()
    {
        floorRenderer = GetComponent<Renderer>();
        EnsureDefaultPresets();

        Width = Mathf.Clamp(Mathf.Abs(transform.localScale.x) * DefaultPlaneSize, minWidth, maxWidth);
        Length = Mathf.Clamp(Mathf.Abs(transform.localScale.z) * DefaultPlaneSize, minLength, maxLength);

        ApplyFloorTexture();
        RebuildRoomGeometry(false);
    }

    public void SetFloorSize(float width, float length)
    {
        Width = Mathf.Clamp(Mathf.Abs(width), minWidth, maxWidth);
        Length = Mathf.Clamp(Mathf.Abs(length), minLength, maxLength);

        transform.localScale = new Vector3(Width / DefaultPlaneSize, transform.localScale.y, Length / DefaultPlaneSize);

        ApplyFloorTextureTiling();
        RebuildRoomGeometry(true);
    }

    public void SetFloorMaterial(int presetIndex)
    {
        EnsureDefaultPresets();

        if (floorPresets.Count == 0)
        {
            return;
        }

        currentPresetIndex = ((presetIndex % floorPresets.Count) + floorPresets.Count) % floorPresets.Count;
        ApplyFloorTexture();
        RoomChanged?.Invoke();
    }

    public void SelectNextFloorMaterial()
    {
        SetFloorMaterial(currentPresetIndex + 1);
    }

    public void SelectPreviousFloorMaterial()
    {
        SetFloorMaterial(currentPresetIndex - 1);
    }

    public void SetWallsEnabled(bool enabled)
    {
        wallsEnabled = enabled;
        RebuildRoomGeometry(true);
    }

    public void ToggleWalls()
    {
        SetWallsEnabled(!wallsEnabled);
    }

    public Bounds GetRoomBounds(float padding = 0f)
    {
        float halfWidth = Mathf.Max(0.1f, Width * 0.5f - padding);
        float halfLength = Mathf.Max(0.1f, Length * 0.5f - padding);
        Vector3 center = transform.position + new Vector3(0f, wallHeight * 0.5f, 0f);
        return new Bounds(center, new Vector3(halfWidth * 2f, wallHeight, halfLength * 2f));
    }

    public Vector3 ClampPointToRoom(Vector3 point, float padding = 0f)
    {
        Bounds bounds = GetRoomBounds(padding);
        point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
        point.z = Mathf.Clamp(point.z, bounds.min.z, bounds.max.z);
        return point;
    }

    public void ClampTransformToRoom(Transform target, float padding = 0f)
    {
        if (target == null)
        {
            return;
        }

        target.position = ClampPointToRoom(target.position, padding);
    }

    void EnsureDefaultPresets()
    {
        if (floorPresets.Count > 0)
        {
            currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, floorPresets.Count - 1);
            return;
        }

        floorPresets.Add(new FloorTexturePreset
        {
            displayName = "Wood",
            pattern = FloorPattern.Wood,
            baseColor = new Color(0.58f, 0.39f, 0.22f),
            accentColor = new Color(0.29f, 0.17f, 0.08f),
            repeatSize = 1.2f
        });

        floorPresets.Add(new FloorTexturePreset
        {
            displayName = "Light Tile",
            pattern = FloorPattern.Tile,
            baseColor = new Color(0.78f, 0.77f, 0.72f),
            accentColor = new Color(0.42f, 0.42f, 0.38f),
            repeatSize = 1f
        });

        floorPresets.Add(new FloorTexturePreset
        {
            displayName = "Concrete",
            pattern = FloorPattern.Concrete,
            baseColor = new Color(0.45f, 0.46f, 0.43f),
            accentColor = new Color(0.27f, 0.28f, 0.26f),
            repeatSize = 1.8f
        });

        floorPresets.Add(new FloorTexturePreset
        {
            displayName = "Carpet",
            pattern = FloorPattern.Carpet,
            baseColor = new Color(0.32f, 0.43f, 0.50f),
            accentColor = new Color(0.15f, 0.23f, 0.28f),
            repeatSize = 0.8f
        });
    }

    void ApplyFloorTexture()
    {
        if (floorRenderer == null)
        {
            floorRenderer = GetComponent<Renderer>();
        }

        if (floorRenderer == null || floorPresets.Count == 0)
        {
            return;
        }

        FloorTexturePreset preset = floorPresets[currentPresetIndex];
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (runtimeFloorMaterial == null)
        {
            runtimeFloorMaterial = new Material(shader != null ? shader : floorRenderer.sharedMaterial.shader)
            {
                name = "Runtime Floor Material"
            };
            floorRenderer.material = runtimeFloorMaterial;
        }

        generatedFloorTexture = GenerateFloorTexture(preset, 128);
        SetMaterialTexture(runtimeFloorMaterial, generatedFloorTexture);
        SetMaterialColor(runtimeFloorMaterial, Color.white);
        ApplyFloorTextureTiling();
    }

    void ApplyFloorTextureTiling()
    {
        if (runtimeFloorMaterial == null || floorPresets.Count == 0)
        {
            return;
        }

        float repeatSize = Mathf.Max(0.25f, floorPresets[currentPresetIndex].repeatSize);
        Vector2 tiling = new Vector2(Mathf.Max(1f, Width / repeatSize), Mathf.Max(1f, Length / repeatSize));

        if (runtimeFloorMaterial.HasProperty("_BaseMap"))
        {
            runtimeFloorMaterial.SetTextureScale("_BaseMap", tiling);
        }

        if (runtimeFloorMaterial.HasProperty("_MainTex"))
        {
            runtimeFloorMaterial.SetTextureScale("_MainTex", tiling);
        }
    }

    Texture2D GenerateFloorTexture(FloorTexturePreset preset, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Generated " + preset.displayName + " Floor",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, EvaluateFloorPixel(preset, x, y, size));
            }
        }

        texture.Apply();
        return texture;
    }

    Color EvaluateFloorPixel(FloorTexturePreset preset, int x, int y, int size)
    {
        switch (preset.pattern)
        {
            case FloorPattern.Tile:
                int tileSize = size / 4;
                bool grout = x % tileSize < 3 || y % tileSize < 3;
                float tileNoise = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.12f;
                return grout ? preset.accentColor : Color.Lerp(preset.baseColor, Color.white, tileNoise);

            case FloorPattern.Concrete:
                float concreteNoise = Mathf.PerlinNoise(x * 0.09f + 17.3f, y * 0.09f + 9.7f);
                return Color.Lerp(preset.accentColor, preset.baseColor, 0.45f + concreteNoise * 0.45f);

            case FloorPattern.Carpet:
                float fiber = Mathf.PerlinNoise(x * 0.4f + 4.2f, y * 0.4f + 2.8f);
                return Color.Lerp(preset.accentColor, preset.baseColor, 0.55f + fiber * 0.35f);

            default:
                int plankWidth = size / 8;
                bool seam = x % plankWidth < 2;
                float grain = Mathf.PerlinNoise(x * 0.08f, y * 0.22f);
                Color wood = Color.Lerp(preset.accentColor, preset.baseColor, 0.55f + grain * 0.35f);
                return seam ? Color.Lerp(preset.accentColor, Color.black, 0.15f) : wood;
        }
    }

    void RebuildRoomGeometry(bool notify)
    {
        EnsureWallRoot();

        if (wallRoot != null)
        {
            wallRoot.gameObject.SetActive(wallsEnabled);
        }

        if (wallsEnabled)
        {
            UpdateWalls();
        }

        if (notify)
        {
            RoomChanged?.Invoke();
        }
    }

    void EnsureWallRoot()
    {
        if (wallRoot != null)
        {
            return;
        }

        GameObject rootObject = GameObject.Find(WallRootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(WallRootName);
        }

        if (!rootObject.TryGetComponent(out RoomBoundary _))
        {
            rootObject.AddComponent<RoomBoundary>();
        }

        wallRoot = rootObject.transform;
    }

    void UpdateWalls()
    {
        EnsureWallRoot();

        float halfWidth = Width * 0.5f;
        float halfLength = Length * 0.5f;
        float halfWallHeight = wallHeight * 0.5f;
        Vector3 center = transform.position;

        SetWall(0, "North Wall",
            center + new Vector3(0f, halfWallHeight, halfLength + wallThickness * 0.5f),
            new Vector3(Width + wallThickness * 2f, wallHeight, wallThickness));
        SetWall(1, "South Wall",
            center + new Vector3(0f, halfWallHeight, -halfLength - wallThickness * 0.5f),
            new Vector3(Width + wallThickness * 2f, wallHeight, wallThickness));
        SetWall(2, "East Wall",
            center + new Vector3(halfWidth + wallThickness * 0.5f, halfWallHeight, 0f),
            new Vector3(wallThickness, wallHeight, Length));
        SetWall(3, "West Wall",
            center + new Vector3(-halfWidth - wallThickness * 0.5f, halfWallHeight, 0f),
            new Vector3(wallThickness, wallHeight, Length));
    }

    void SetWall(int index, string wallName, Vector3 position, Vector3 scale)
    {
        if (walls[index] == null)
        {
            walls[index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls[index].name = wallName;
            walls[index].transform.SetParent(wallRoot, true);
            walls[index].AddComponent<RoomBoundary>();
        }

        walls[index].transform.position = position;
        walls[index].transform.rotation = Quaternion.identity;
        walls[index].transform.localScale = scale;
        walls[index].SetActive(true);

        Renderer wallRenderer = walls[index].GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.sharedMaterial = GetWallMaterial();
        }
    }

    Material GetWallMaterial()
    {
        if (wallMaterial != null)
        {
            return wallMaterial;
        }

        if (runtimeWallMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            runtimeWallMaterial = new Material(shader)
            {
                name = "Runtime Wall Material"
            };
            SetMaterialColor(runtimeWallMaterial, wallColor);
        }

        return runtimeWallMaterial;
    }

    void SetMaterialTexture(Material material, Texture texture)
    {
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
    }

    void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}

public sealed class RoomBoundary : MonoBehaviour
{
}
