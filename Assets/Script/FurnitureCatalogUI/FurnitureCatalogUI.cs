using UnityEngine;

public class FurnitureCatalogUI : MonoBehaviour
{
    public FurnitureSpawner spawner;

    public GameObject[] furniturePrefabs;

    public Transform contentParent;

    public FurnitureItemUI itemPrefab;

    void Start()
    {
        foreach (GameObject prefab in furniturePrefabs)
        {
            FurnitureItemUI item =
                Instantiate(itemPrefab, contentParent);

            item.Setup(prefab, spawner);
        }
    }
}