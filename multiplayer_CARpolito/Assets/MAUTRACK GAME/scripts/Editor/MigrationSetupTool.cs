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
        [MenuItem("Mautrack/1. Auto Setup Scene (Magic Button)")]
        public static void ShowWindow()
        {
            SetupScene();
        }

        [MenuItem("Mautrack/2. Create 'Main Menu' Scene (Cover)")]
        public static void CreateMainMenu()
        {
            CreateMainMenuScene();
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
                
                // 1. Coins Container (Top Right)
                GameObject coinsCk = new GameObject("CoinsContainer");
                coinsCk.transform.SetParent(panelObj.transform, false);
                RectTransform cRect = coinsCk.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(1, 1);
                cRect.anchorMax = new Vector2(1, 1);
                cRect.pivot = new Vector2(1, 1);
                cRect.anchoredPosition = new Vector2(-20, -20); // Closer to corner but with padding
                cRect.sizeDelta = new Vector2(500, 120); // Fixed size box for coins
                
                Image cBg = coinsCk.AddComponent<Image>();
                cBg.color = new Color(0,0,0,0.7f); // Dark background for contrast

                GameObject coinsObj = new GameObject("Text");
                coinsObj.transform.SetParent(coinsCk.transform, false);
                Text coinsText = coinsObj.AddComponent<Text>();
                coinsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                coinsText.fontSize = 65; 
                coinsText.alignment = TextAnchor.MiddleCenter; 
                coinsText.color = Color.yellow;
                coinsText.text = "Coins: 0";
                
                RectTransform crt = coinsText.GetComponent<RectTransform>();
                crt.anchorMin = Vector2.zero;
                crt.anchorMax = Vector2.one;
                crt.offsetMin = new Vector2(10, 0); // Padding
                crt.offsetMax = new Vector2(-10, 0);

                // Assign to script using SerializedObject
                SerializedObject so = new SerializedObject(shopScript);
                so.FindProperty("coinsText").objectReferenceValue = coinsText;
                
                // 2. Scroll View (Fills Top Half)
                GameObject svObj = new GameObject("Scroll View");
                svObj.transform.SetParent(panelObj.transform, false);
                RectTransform svRect = svObj.AddComponent<RectTransform>();
                svRect.anchorMin = new Vector2(0.1f, 0.25f); // Leave 25% at bottom for button
                svRect.anchorMax = new Vector2(0.9f, 0.80f); // Leave 20% at top for coins header
                svRect.offsetMin = Vector2.zero;
                svRect.offsetMax = Vector2.zero;
                
                // Add ScrollRect Component
                ScrollRect scroll = svObj.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.scrollSensitivity = 20;

                // Add Viewport (Mask) for clean scrolling
                GameObject viewObj = new GameObject("Viewport");
                viewObj.transform.SetParent(svObj.transform, false);
                RectTransform viewRect = viewObj.AddComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewObj.AddComponent<Mask>().showMaskGraphic = false;
                Image viewImg = viewObj.AddComponent<Image>();
                viewImg.color = new Color(1,1,1,0.01f); // Invisible but raycast target

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewObj.transform, false);
                RectTransform contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.sizeDelta = new Vector2(0, 300); // Initial height
                
                scroll.content = contentRect;

                GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(400, 500); // BIG CARDS
                grid.spacing = new Vector2(50, 50);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1; // 1 Column for mobile friendliness/clarity
                
                // Content Size Fitter (CRITICAL)
                ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                so.FindProperty("contentContainer").objectReferenceValue = contentObj.transform;

                // Create Slot Prefab Helper
                string prefabPath = "Assets/MAUTRACK GAME/Prefabs/ShopSlot.prefab";
                GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                // Start Fresh for Prefab to ensure new style
                if (slotPrefab != null) AssetDatabase.DeleteAsset(prefabPath);

                if (slotPrefab == null)
                {
                    // Create minimal slot for prefab
                    GameObject tempSlot = new GameObject("ShopSlot");
                    Image slotBg = tempSlot.AddComponent<Image>();
                    slotBg.color = new Color(0.2f, 0.2f, 0.2f); // Dark Grey Card
                    CarShopSlotUI slotUI = tempSlot.AddComponent<CarShopSlotUI>();
                    
                    // Add Name Text (Top)
                    GameObject nObj = new GameObject("Name");
                    nObj.transform.SetParent(tempSlot.transform);
                    Text nText = nObj.AddComponent<Text>();
                    nText.text = "Car Name";
                    nText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    nText.fontSize = 50; // BIG
                    nText.color = Color.white;
                    nText.alignment = TextAnchor.UpperCenter;
                    RectTransform nRect = nText.GetComponent<RectTransform>();
                    nRect.anchorMin = new Vector2(0, 0.8f);
                    nRect.anchorMax = new Vector2(1, 1);
                    nRect.offsetMin = Vector2.zero;
                    nRect.offsetMax = Vector2.zero;
                    
                    // Add Buy Button (Bottom)
                    GameObject bObj = new GameObject("BuyBtn");
                    bObj.transform.SetParent(tempSlot.transform);
                    Image bImg = bObj.AddComponent<Image>();
                    bImg.color = Color.green;
                    Button bBtn = bObj.AddComponent<Button>();
                    RectTransform bRect = bObj.GetComponent<RectTransform>();
                    bRect.anchorMin = new Vector2(0.1f, 0.05f);
                    bRect.anchorMax = new Vector2(0.9f, 0.25f);
                    bRect.offsetMin = Vector2.zero;
                    bRect.offsetMax = Vector2.zero;

                    // Button Text
                    GameObject bTxtObj = new GameObject("BtnText");
                    bTxtObj.transform.SetParent(bObj.transform, false);
                    Text bText = bTxtObj.AddComponent<Text>();
                    bText.text = "BUY";
                    bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    bText.fontSize = 40;
                    bText.color = Color.black;
                    bText.alignment = TextAnchor.MiddleCenter;
                    RectTransform btRect = bText.GetComponent<RectTransform>();
                    btRect.anchorMin = Vector2.zero;
                    btRect.anchorMax = Vector2.one;
                    btRect.offsetMin = Vector2.zero;
                    btRect.offsetMax = Vector2.zero;

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

        public static void CreateMainMenuScene()
        {
            string scenePath = "Assets/MAUTRACK GAME/Scenes/MainMenu.unity";
            
            // Check if scene exists, if not create it
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Setup the scene content
            
            // 1. Create Camera (CRITICAL)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f); // Dark Blue
            camObj.AddComponent<AudioListener>();

            // 2. Create EventSystem (CRITICAL for UI)
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            SetupScene(); // Reuse the logic to create Manager and UI

            // 3. Improve UI Layout
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                // Fix Canvas Scaler
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                // Create Play Button Container (Bottom Center)
                GameObject playBtnObj = new GameObject("PLAY BUTTON - FIND MATCH");
                playBtnObj.transform.SetParent(canvas.transform, false);
                Image btnImg = playBtnObj.AddComponent<Image>();
                btnImg.color = new Color(0.8f, 0.2f, 0.2f); // Better Red
                Button btn = playBtnObj.AddComponent<Button>();
                
                RectTransform rect = playBtnObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0); // Bottom Center
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 120); // Slightly higher
                rect.sizeDelta = new Vector2(600, 180); // TALLER and WIDER for 2 lines

                // Add Text
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(playBtnObj.transform, false);
                Text txt = txtObj.AddComponent<Text>();
                txt.text = "FIND MATCH\n(50 Coins)";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 45; // Slightly smaller to fit comfortably
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                // Add Shadow for better readability
                txtObj.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                
                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                // Add Logic
                Mautrack.Gameplay.MatchmakingManager matchManager = playBtnObj.AddComponent<Mautrack.Gameplay.MatchmakingManager>();
                
                // Assign references via SerializedObject
                SerializedObject so = new SerializedObject(matchManager);
                so.FindProperty("entryFee").intValue = 50;
                // Try to find the gameplay scene path
                string gameplayScene = "Assets/MAUTRACK GAME/Scenes/track0001.unity"; 
                // We will add it to build settings later, name is simply "track0001"
                so.FindProperty("gameSceneName").stringValue = "track0001";
                so.FindProperty("playButton").objectReferenceValue = btn;
                so.ApplyModifiedProperties();
            }

            // Save Scene
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[+] Created Main Menu Scene at {scenePath}");

            // Add to Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            
            // Add MainMenu first
            if (!buildScenes.Exists(s => s.path == scenePath))
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            
            // Add Review Track scene if exists
            string trackPath = "Assets/MAUTRACK GAME/Scenes/track0001.unity";
            if (System.IO.File.Exists(trackPath) && !buildScenes.Exists(s => s.path == trackPath))
                buildScenes.Add(new EditorBuildSettingsScene(trackPath, true));

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[+] Updated Build Settings with MainMenu and track0001");
        }
    }
}
#endif
