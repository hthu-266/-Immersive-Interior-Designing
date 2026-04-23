using TMPro;
using UnityEngine;

public class RoomUIController : MonoBehaviour
{
    public TMP_InputField widthInput;
    public TMP_InputField lengthInput;

    public FloorController floorController;

    public void OnGenerateRoom()
    {
        if (!float.TryParse(widthInput.text, out float width))
        {
            Debug.Log("Invalid width");
            return;
        }

        if (!float.TryParse(lengthInput.text, out float length))
        {
            Debug.Log("Invalid length");
            return;
        }

        floorController.SetFloorSize(width, length);
    }
}