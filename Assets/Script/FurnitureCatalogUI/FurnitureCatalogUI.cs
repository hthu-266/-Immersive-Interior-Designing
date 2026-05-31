using UnityEngine;

public class FurnitureCatalogUI : MonoBehaviour
{
    public FurnitureSpawner spawner;
    public FurniturePreviewRenderer previewRenderer;
    public bool generatePreviews = true;
    public bool hideTemplateItem = true;

    public GameObject[] furniturePrefabs;

    public Transform contentParent;

    public FurnitureItemUI itemPrefab;

    void Start()
    {
        ResolveReferences();

        if (itemPrefab == null || contentParent == null)
        {
            return;
        }

        if (hideTemplateItem && itemPrefab.transform.parent == contentParent)
        {
            itemPrefab.gameObject.SetActive(false);
        }

        foreach (GameObject prefab in furniturePrefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            FurnitureItemUI item =
                Instantiate(itemPrefab, contentParent);

            item.gameObject.SetActive(true);
            item.Setup(prefab, spawner, generatePreviews ? previewRenderer : null);
        }
    }

    void ResolveReferences()
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<FurnitureSpawner>();
        }

        if (generatePreviews && previewRenderer == null)
        {
            previewRenderer = FurniturePreviewRenderer.FindOrCreate();
        }
    }
}
