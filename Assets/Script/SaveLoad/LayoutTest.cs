using UnityEngine;

public class LayoutTest : MonoBehaviour
{
    public LayoutManager layoutManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            layoutManager.LoadDefaultLayout();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            layoutManager.SaveLayout("Slot1");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            layoutManager.LoadLayout("Slot1");
        }
    }
}