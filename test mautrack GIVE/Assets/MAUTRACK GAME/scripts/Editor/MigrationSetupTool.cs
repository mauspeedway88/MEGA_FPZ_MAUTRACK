#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mautrack.PlayFabIntegration;
using Mautrack.Data;
using Mautrack.UI;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Mautrack.Editor
{
    public class MigrationSetupTool : EditorWindow
    {
        [MenuItem("Mautrack/Auto Setup Scene (Magic Button)")]
        public static void ShowWindow()
        {
            SetupScene();
        }

        public static void SetupScene()
        {
            Debug.Log("--- Starting Auto Setup ---");

            // 1. SETUP PLAYFAB MANAGER
            var manager = FindObjectOfType<PlayFabManager>();
            if (manager == null)
            {
                GameObject go = new GameObject("PlayFabManager");
                manager = go.AddComponent<PlayFabManager>();
                Debug.Log("[+] Created PlayFabManager GameObject.");
                Undo.RegisterCreatedObjectUndo(go, "Create PlayFabManager");
            }
            else
            {
                Debug.Log("[-] PlayFabManager already exists.");
            }

            // 2. SETUP CAR DATA ASSETS
            string dataPath = "Assets/MAUTRACK GAME/Data";
            if (!AssetDatabase.IsValidFolder("Assets/MAUTRACK GAME"))
            {
                AssetDatabase.CreateFolder("Assets", "MAUTRACK GAME");
            }
            if (!AssetDatabase.IsValidFolder("Assets/MAUTRACK GAME/Data"))
            {
                AssetDatabase.CreateFolder("Assets/MAUTRACK GAME", "Data");
                Debug.Log("[+] Created Data folder.");
            }

            // Load Car Prefab
            GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MAUTRACK GAME/Prefabs/Car prefab.prefab");
            if (carPrefab == null) Debug.LogWarning("[!] Could not find 'Assets/MAUTRACK GAME/Prefabs/Car prefab.prefab'. Please assign prefab manually.");

            List<CarData> createdCars = new List<CarData>();

            for (int i = 0; i < 3; i++)
            {
                string assetPath = $"{dataPath}/Car_{i}.asset";
                CarData carData = AssetDatabase.LoadAssetAtPath<CarData>(assetPath);
                
                if (carData == null)
                {
                    carData = ScriptableObject.CreateInstance<CarData>();
                    carData.CarID = i;
                    carData.CarName = $"Super Car {i+1}";
                    carData.Price = (i + 1) * 500;
                    carData.CarPrefab = carPrefab;
                    carData.IsDefault = (i == 0); // First car is default
                    
                    AssetDatabase.CreateAsset(carData, assetPath);
                    Debug.Log($"[+] Created Car Data: {assetPath}");
                }
                createdCars.Add(carData);
            }
            AssetDatabase.SaveAssets();

            // 3. SETUP SHOP UI
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[!] No Canvas found in scene. Creating one...");
                GameObject canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }

            // Create Shop Panel
            Transform shopPanel = canvas.transform.Find("Car Shop Panel");
            CarShopUI shopScript = null;

            if (shopPanel == null)
            {
                GameObject panelObj = new GameObject("Car Shop Panel");
                panelObj.transform.SetParent(canvas.transform, false);
                
                // Add Background
                Image bg = panelObj.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.9f);
                RectTransform rect = panelObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one; // Full screen
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Add Shop Script
                shopScript = panelObj.AddComponent<CarShopUI>();
                
                // Create Coins Text
                GameObject coinsObj = new GameObject("Coins Text");
                coinsObj.transform.SetParent(panelObj.transform, false);
                Text coinsText = coinsObj.AddComponent<Text>();
                coinsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                coinsText.fontSize = 40;
                coinsText.alignment = TextAnchor.UpperRight;
                coinsText.color = Color.yellow;
                coinsText.text = "Coins: 0";
                RectTransform crt = coinsText.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.anchoredPosition = new Vector2(-50, -50);
                crt.sizeDelta = new Vector2(300, 100);

                // Assign to script using SerializedObject to avoid "private" issues if fields are private
                SerializedObject so = new SerializedObject(shopScript);
                so.FindProperty("coinsText").objectReferenceValue = coinsText;
                
                // Create Scroll View (Basic)
                GameObject svObj = new GameObject("Scroll View");
                svObj.transform.SetParent(panelObj.transform, false);
                // ... (Simplified: Just a container for now)
                RectTransform svRect = svObj.AddComponent<RectTransform>();
                svRect.sizeDelta = new Vector2(800, 400);

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(svObj.transform, false);
                GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(200, 300);
                grid.spacing = new Vector2(10, 10);
                
                so.FindProperty("contentContainer").objectReferenceValue = contentObj.transform;

                // Create Slot Prefab Helper
                string prefabPath = "Assets/MAUTRACK GAME/Prefabs/ShopSlot.prefab";
                GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (slotPrefab == null)
                {
                    // Create minimal slot for prefab
                    GameObject tempSlot = new GameObject("ShopSlot");
                    tempSlot.AddComponent<Image>().color = Color.gray;
                    CarShopSlotUI slotUI = tempSlot.AddComponent<CarShopSlotUI>();
                    
                    // Add Name Text
                    GameObject nObj = new GameObject("Name");
                    nObj.transform.SetParent(tempSlot.transform);
                    Text nText = nObj.AddComponent<Text>();
                    nText.text = "Car Name";
                    nText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    nText.color = Color.black;
                    
                    // Add Buy Button
                    GameObject bObj = new GameObject("BuyBtn");
                    bObj.transform.SetParent(tempSlot.transform);
                    bObj.AddComponent<Image>().color = Color.green;
                    Button bBtn = bObj.AddComponent<Button>();
                    bObj.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

                    // Assign refs
                    SerializedObject slotSo = new SerializedObject(slotUI);
                    slotSo.FindProperty("carNameText").objectReferenceValue = nText;
                    slotSo.FindProperty("buyButton").objectReferenceValue = bBtn;
                    slotSo.ApplyModifiedProperties();

                    // Save as Prefab
                    if (!AssetDatabase.IsValidFolder("Assets/MAUTRACK GAME/Prefabs"))
                        AssetDatabase.CreateFolder("Assets/MAUTRACK GAME", "Prefabs");
                        
                    slotPrefab = PrefabUtility.SaveAsPrefabAsset(tempSlot, prefabPath);
                    Object.DestroyImmediate(tempSlot);
                    Debug.Log("[+] Created ShopSlot Prefab.");
                }

                so.FindProperty("shopSlotPrefab").objectReferenceValue = slotPrefab;
                
                // Assign Available Cars
                SerializedProperty carsProp = so.FindProperty("availableCars");
                carsProp.ClearArray();
                for (int i = 0; i < createdCars.Count; i++)
                {
                    carsProp.InsertArrayElementAtIndex(i);
                    carsProp.GetArrayElementAtIndex(i).objectReferenceValue = createdCars[i];
                }

                so.ApplyModifiedProperties();
                Debug.Log("[+] Created Shop UI.");
            }

            Debug.Log("--- Auto Setup Complete! Check your Scene and Assets. ---");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
