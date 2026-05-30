using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class LayoutManager : MonoBehaviour
{
    DefaultLayoutData defaultLayout =
        new DefaultLayoutData();

    string SaveFolder =>
        Application.persistentDataPath;

    IEnumerator Start()
    {
        // Test
        Debug.Log("LayoutManager Start");
        yield return null;

        // Lấy Default Layout xếp tay (F0 để reset về sau)
        CacheDefaultLayout();

        // Nếu có layout đã lưu thì load ra Slot 1
        if (File.Exists(GetPath("Slot1")))
        {
            LoadLayout("Slot1");
        }
    }

    string GetPath(string layoutName)
    {
        return Path.Combine(
            SaveFolder,
            layoutName + ".json");
    }

    void CacheDefaultLayout()
    {
        defaultLayout.positions.Clear();
        defaultLayout.rotations.Clear();

        FurnitureID[] furnitures =
            FindObjectsByType<FurnitureID>(
                FindObjectsSortMode.None);

        foreach (FurnitureID furniture in furnitures)
        {
            defaultLayout.positions[
                furniture.UniqueID] =
                furniture.transform.position;

            defaultLayout.rotations[
                furniture.UniqueID] =
                furniture.transform.rotation;
        }

        Debug.Log(
            $"Default Layout Cached ({furnitures.Length} items)");
    }

    public void LoadDefaultLayout()
    {
        FurnitureID[] furnitures =
            FindObjectsByType<FurnitureID>(
                FindObjectsSortMode.None);

        foreach (FurnitureID furniture in furnitures)
        {
            if (!defaultLayout.positions.TryGetValue(
                    furniture.UniqueID,
                    out Vector3 position))
            {
                continue;
            }

            furniture.transform.position =
                position;

            furniture.transform.rotation =
                defaultLayout.rotations[
                    furniture.UniqueID];
        }

        Debug.Log("Default Layout Loaded");
    }

    public void SaveLayout(string layoutName)
    {
        LayoutData layout =
            new LayoutData();

        MovableFurniture[] furnitures =
            FindObjectsByType<MovableFurniture>(
                FindObjectsSortMode.None);

        foreach (MovableFurniture furniture in furnitures)
        {
            FurnitureID id =
                furniture.GetComponent<FurnitureID>();

            if (id == null)
            {
                continue;
            }

            FurnitureSaveData data =
                new FurnitureSaveData();

            data.furnitureID =
                id.UniqueID;

            data.position =
                new Vector3Data(
                    furniture.transform.position);

            data.rotation =
                new Vector3Data(
                    furniture.transform.eulerAngles);

            layout.furnitures.Add(data);
        }

        string json =
            JsonUtility.ToJson(
                layout,
                true);

        File.WriteAllText(
            GetPath(layoutName),
            json);

        Debug.Log($"Saved : {layoutName}");
        
        Debug.Log($"Save Path: {GetPath(layoutName)}");
    }

    public void LoadLayout(string layoutName)
    {
        Debug.Log($"Load Path: {GetPath(layoutName)}");
        Debug.Log($"File Exists = {File.Exists(GetPath(layoutName))}");

        string path =
            GetPath(layoutName);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"Layout not found : {layoutName}");

            return;
        }


        string json =
            File.ReadAllText(path);

        LayoutData layout =
            JsonUtility.FromJson<LayoutData>(
                json);

        Dictionary<string, FurnitureID>
            furnitureMap =
            new Dictionary<string, FurnitureID>();

        FurnitureID[] ids =
            FindObjectsByType<FurnitureID>(
                FindObjectsSortMode.None);

        foreach (FurnitureID id in ids)
        {
            furnitureMap[id.UniqueID] = id;
        }

        foreach (FurnitureSaveData data
                 in layout.furnitures)
        {
            if (!furnitureMap.TryGetValue(
                    data.furnitureID,
                    out FurnitureID furniture))
            {
                continue;
            }

            furniture.transform.position =
                data.position.ToVector3();

            furniture.transform.rotation =
                Quaternion.Euler(
                    data.rotation.ToVector3());
        }

        Debug.Log(
            $"Loaded : {layoutName}");
    }
}