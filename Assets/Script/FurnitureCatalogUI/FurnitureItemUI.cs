using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureItemUI : MonoBehaviour
{
    public TMP_Text titleText;
    public Button button;
    public RawImage previewImage;
    public bool autoCreatePreviewImage = true;

    GameObject furniturePrefab;
    FurnitureSpawner spawner;

    public void Setup(
        GameObject prefab,
        FurnitureSpawner targetSpawner,
        FurniturePreviewRenderer previewRenderer = null)
    {
        ResolveReferences();

        furniturePrefab = prefab;
        spawner = targetSpawner;

        if (titleText != null)
        {
            titleText.text = GetDisplayName(prefab);
        }

        SetupPreview(prefab, previewRenderer);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (spawner == null || furniturePrefab == null)
        {
            return;
        }

        spawner.SpawnFurniture(furniturePrefab);
    }

    void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (titleText == null)
        {
            titleText = GetComponentInChildren<TMP_Text>(true);
        }

        if (previewImage == null)
        {
            previewImage = GetComponentInChildren<RawImage>(true);
        }
    }

    void SetupPreview(GameObject prefab, FurniturePreviewRenderer previewRenderer)
    {
        if (previewRenderer == null || prefab == null)
        {
            if (previewImage != null)
            {
                previewImage.enabled = false;
            }

            return;
        }

        if (previewImage == null && autoCreatePreviewImage)
        {
            previewImage = CreatePreviewImage();
        }

        if (previewImage == null)
        {
            return;
        }

        Texture previewTexture = previewRenderer.GetPreview(prefab);

        previewImage.texture = previewTexture;
        previewImage.enabled = previewTexture != null;
        previewImage.color = Color.white;

        FormatTitleForPreview();
    }

    RawImage CreatePreviewImage()
    {
        GameObject previewObject = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.SetParent(transform, false);
        previewRect.anchorMin = new Vector2(0f, 0.26f);
        previewRect.anchorMax = new Vector2(1f, 1f);
        previewRect.offsetMin = new Vector2(8f, 4f);
        previewRect.offsetMax = new Vector2(-8f, -8f);

        RawImage image = previewObject.GetComponent<RawImage>();
        image.raycastTarget = false;

        AspectRatioFitter fitter = previewObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;

        previewRect.SetSiblingIndex(0);
        return image;
    }

    void FormatTitleForPreview()
    {
        if (titleText == null)
        {
            return;
        }

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0f, 5f);
            titleRect.sizeDelta = new Vector2(-10f, 24f);
            titleRect.SetAsLastSibling();
        }

        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 12f;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 8f;
        titleText.fontSizeMax = 12f;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.raycastTarget = false;
    }

    string GetDisplayName(GameObject prefab)
    {
        return prefab == null
            ? "Furniture"
            : prefab.name.Replace('_', ' ');
    }
}
