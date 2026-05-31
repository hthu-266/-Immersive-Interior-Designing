using UnityEngine;

public class CatalogUIController : MonoBehaviour
{
    [SerializeField] GameObject furniturePanel;
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] bool showOnStart = true;

    void Awake()
    {
        ResolveFurniturePanel();
    }

    void Start()
    {
        if (ResolveFurniturePanel())
        {
            furniturePanel.SetActive(showOnStart);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && ResolveFurniturePanel())
        {
            furniturePanel.SetActive(!furniturePanel.activeSelf);
        }
    }

    bool ResolveFurniturePanel()
    {
        if (furniturePanel != null)
        {
            return true;
        }

        GameObject foundPanel = GameObject.Find("FurniturePanel");
        if (foundPanel != null)
        {
            furniturePanel = foundPanel;
        }

        return furniturePanel != null;
    }
}
