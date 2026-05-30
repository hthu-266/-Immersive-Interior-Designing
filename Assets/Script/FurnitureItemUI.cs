using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureItemUI : MonoBehaviour
{
    public TMP_Text titleText;
    public Button button;

    GameObject furniturePrefab;
    FurnitureSpawner spawner;

    public void Setup(
        GameObject prefab,
        FurnitureSpawner targetSpawner)
    {
        furniturePrefab = prefab;
        spawner = targetSpawner;

        titleText.text = prefab.name;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        spawner.SpawnFurniture(furniturePrefab);
    }
}