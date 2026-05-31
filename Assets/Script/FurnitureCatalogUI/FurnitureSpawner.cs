using UnityEngine;

public class FurnitureSpawner : MonoBehaviour
{
    public Transform furnitureParent;
    public FloorController floorController;
    public FurnitureInteractionController interactionController;

    void Awake()
    {
        ResolveReferences();
    }

    public void SpawnFurniture(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        ResolveReferences();

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
            GetFurnitureLayer(),
            true,
            true);

        if (interactionController != null)
        {
            interactionController.Select(movable);
        }
    }

    void ResolveReferences()
    {
        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        if (interactionController == null)
        {
            interactionController = FindFirstObjectByType<FurnitureInteractionController>();
        }
    }

    int GetFurnitureLayer()
    {
        int furnitureLayer = LayerMask.NameToLayer("Furniture");
        return furnitureLayer >= 0 ? furnitureLayer : gameObject.layer;
    }
}
