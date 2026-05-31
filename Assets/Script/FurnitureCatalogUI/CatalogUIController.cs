using UnityEngine;

public class CatalogUIController : MonoBehaviour
{
    [SerializeField] private GameObject furniturePanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            furniturePanel.SetActive(!furniturePanel.activeSelf);
        }
    }
}