using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mautrack.PlayFabIntegration;

namespace Mautrack.Gameplay
{
    public class MatchmakingManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int entryFee = 50;
        [SerializeField] private string gameSceneName = "Scene1"; // Replace with your actual game scene name

        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject notEnoughCoinsPopup;
        [SerializeField] private Text entryFeeText;

        private void Start()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(TryFindMatch);
            }

            if (entryFeeText != null)
            {
                entryFeeText.text = $"ENTRY FEE: {entryFee}";
            }

            if (notEnoughCoinsPopup != null)
            {
                notEnoughCoinsPopup.SetActive(false);
            }
        }

        public void TryFindMatch()
        {
            var pm = PlayFabManager.Instance;
            if (pm == null)
            {
                Debug.LogError("PlayFabManager instance not found!");
                return;
            }

            // Check if user has enough coins
            if (pm.CurrentPlayerData.Coins >= entryFee)
            {
                // Deduct coins
                pm.CurrentPlayerData.Coins -= entryFee;
                
                // Save and Load Scene
                pm.SavePlayerData(() => 
                {
                    Debug.Log($"Paid {entryFee} coins. Entering match...");
                    LoadGameScene();
                });
            }
            else
            {
                // Not enough coins
                Debug.Log("Not enough coins to play!");
                if (notEnoughCoinsPopup != null)
                {
                    notEnoughCoinsPopup.SetActive(true);
                }
            }
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void ClosePopup()
        {
            if (notEnoughCoinsPopup != null)
            {
                notEnoughCoinsPopup.SetActive(false);
            }
        }
    }
}
