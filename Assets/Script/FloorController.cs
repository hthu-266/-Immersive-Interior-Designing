using UnityEngine;

public class FloorController : MonoBehaviour
{
    public void SetFloorSize(float width, float length)
    {
        // Plane mặc định là 10x10
        float scaleX = width / 10f;
        float scaleZ = length / 10f;

        transform.localScale = new Vector3(scaleX, 1f, scaleZ);
    }
}