using UnityEngine;

public class FurnitureSpawner : MonoBehaviour
{
    public Transform furnitureParent;
    public FloorController floorController;
    public FurnitureInteractionController interactionController;

    public void SpawnFurniture(GameObject prefab)
    {
        Vector3 spawnPosition = Vector3.zero;

        if (floorController != null)
        {
            spawnPosition = floorController.transform.position;
            spawnPosition.y += 0.05f;
        }

        GameObject spawned = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity,
            furnitureParent);

        MovableFurniture movable =
            spawned.GetComponent<MovableFurniture>();

        if (movable == null)
        {
            movable = spawned.AddComponent<MovableFurniture>();
        }

        movable.EnsureInteractionSetup(
            LayerMask.NameToLayer("Furniture"),
            true,
            true);

        interactionController.Select(movable);
    }
}