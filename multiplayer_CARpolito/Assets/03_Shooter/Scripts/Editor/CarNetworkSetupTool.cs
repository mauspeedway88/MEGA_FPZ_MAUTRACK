using UnityEngine;
using UnityEditor;
using Fusion;
using Starter.Shooter;
using UnityStandardAssets.Vehicles.Car;

public class CarNetworkSetupTool : EditorWindow
{
    [MenuItem("Mautrack/Setup Network Car")]
    public static void SetupCar()
    {
        string sourcePath = "Assets/MAUTRACK GAME/Prefabs/Car prefab.prefab";
        string destPath = "Assets/03_Shooter/Prefabs/NetworkedCar.prefab";

        // 1. Copy original prefab to new location
        AssetDatabase.CopyAsset(sourcePath, destPath);
        AssetDatabase.Refresh();

        // 2. Load Prefab Contents
        GameObject carObj = PrefabUtility.LoadPrefabContents(destPath);

        try
        {
            // 3. Add Fusion Components
            if (!carObj.GetComponent<NetworkObject>())
            {
                var netObj = carObj.AddComponent<NetworkObject>();
                // Enable Predicted Spawning if needed, but defaults are usually fine
            }

            if (!carObj.GetComponent<NetworkTransform>())
            {
                carObj.AddComponent<NetworkTransform>();
            }

            /* NetworkRigidbody not available in this Fusion version
            if (!carObj.GetComponent<NetworkRigidbody>())
            {
                carObj.AddComponent<NetworkRigidbody>();
            }
            */

            // 4. Add Gameplay Components
            if (!carObj.GetComponent<PlayerInput>())
            {
                carObj.AddComponent<PlayerInput>();
            }

            Health health = carObj.GetComponent<Health>();
            if (!health)
            {
                health = carObj.AddComponent<Health>();
                health.InitialHealth = 100;
                // Assign Visual Root (First Child usually)
                if (carObj.transform.childCount > 0)
                {
                    health.VisualRoot = carObj.transform.GetChild(0).gameObject;
                    health.ScalingRoot = carObj.transform;
                }
            }

            Player player = carObj.GetComponent<Player>();
            if (!player)
            {
                player = carObj.AddComponent<Player>();
            }

            // 5. Setup Player References
            player.PlayerInput = carObj.GetComponent<PlayerInput>();
            player.Health = health;
            player.KCC = null; // Explicitly null for Car Mode
            player.ScalingRoot = carObj.transform;
            
            // Find Car Controller
            player.CarController = carObj.GetComponentInChildren<CarController>();
            if (!player.CarController)
            {
                Debug.LogError("CarController NOT FOUND on prefab children!");
            }

            // Setup Camera Handle
            Transform cameraHandle = carObj.transform.Find("CameraHandle");
            if (!cameraHandle)
            {
                GameObject handleObj = new GameObject("CameraHandle");
                handleObj.transform.SetParent(carObj.transform, false);
                handleObj.transform.localPosition = new Vector3(0, 3f, -6f); // Behind and Above
                handleObj.transform.localRotation = Quaternion.Euler(15, 0, 0); // Look down slightly
                cameraHandle = handleObj.transform;
            }
            player.CameraHandle = cameraHandle;
            player.CameraPivot = cameraHandle; // Reuse handle as pivot

            Debug.Log("NetworkedCar Prefab Configured!");

            // 6. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(carObj, destPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(carObj);
        }

        // 7. Update GameManager in Scene
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm)
        {
            Player newPrefab = AssetDatabase.LoadAssetAtPath<Player>(destPath);
            Undo.RecordObject(gm, "Set Car Prefab");
            gm.PlayerPrefab = newPrefab;
            EditorUtility.SetDirty(gm);
            Debug.Log("GameManager Updated with NetworkedCar!");
        }
        else
        {
            Debug.LogWarning("GameManager not found in open scene. Please assign prefab manually.");
        }
    }
}
