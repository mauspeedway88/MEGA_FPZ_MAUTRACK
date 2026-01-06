using UnityEngine;

namespace Starter.PlayFabIntegration
{
    /// <summary>
    /// Setup helper that ensures PlayFab managers exist in the scene.
    /// Attach this to a GameObject in your first scene (00_MainMenu).
    /// It will create the necessary managers if they don't exist.
    /// </summary>
    public class PlayFabSetup : MonoBehaviour
    {
        [Header("PlayFab Title ID")]
        [Tooltip("Get this from your PlayFab Dashboard: https://developer.playfab.com")]
        [SerializeField] private string playFabTitleId = "YOUR_TITLE_ID";

        [Header("Auto Create Managers")]
        [SerializeField] private bool autoCreatePlayFabManager = true;
        [SerializeField] private bool autoCreateGameIntegration = true;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Awake()
        {
            SetupPlayFab();
        }

        private void SetupPlayFab()
        {
            // Set up PlayFab Title ID
            if (!string.IsNullOrEmpty(playFabTitleId) && playFabTitleId != "YOUR_TITLE_ID")
            {
                PlayFab.PlayFabSettings.staticSettings.TitleId = playFabTitleId;
                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayFabSetup] Title ID set to: {playFabTitleId}");
                }
            }
            else
            {
                Debug.LogWarning("[PlayFabSetup] PlayFab Title ID not set! Please configure in the inspector.");
            }

            // Create PlayFabManager if needed
            if (autoCreatePlayFabManager && PlayFabManager.Instance == null)
            {
                var managerGO = new GameObject("PlayFabManager");
                var manager = managerGO.AddComponent<PlayFabManager>();
                DontDestroyOnLoad(managerGO);
                
                if (enableDebugLogs)
                {
                    Debug.Log("[PlayFabSetup] Created PlayFabManager");
                }
            }

            // Create GameIntegration if needed
            if (autoCreateGameIntegration && PlayFabGameIntegration.Instance == null)
            {
                var integrationGO = new GameObject("PlayFabGameIntegration");
                integrationGO.AddComponent<PlayFabGameIntegration>();
                DontDestroyOnLoad(integrationGO);
                
                if (enableDebugLogs)
                {
                    Debug.Log("[PlayFabSetup] Created PlayFabGameIntegration");
                }
            }
        }

        /// <summary>
        /// Validate the setup in Editor
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(playFabTitleId) || playFabTitleId == "YOUR_TITLE_ID")
            {
                Debug.LogWarning("[PlayFabSetup] Please set your PlayFab Title ID in the inspector!");
            }
        }
    }
}

