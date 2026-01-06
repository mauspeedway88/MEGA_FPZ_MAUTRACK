using UnityEngine;
using System.Collections.Generic;
using Mautrack.Data;
using Mautrack.PlayFabIntegration;

namespace Mautrack.UI
{
    public class CarShopUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private List<CarData> availableCars;
        
        [Header("UI References")]
        [SerializeField] private GameObject shopSlotPrefab;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private UnityEngine.UI.Text coinsText; // Display current coins

        private List<CarShopSlotUI> _spawnedSlots = new List<CarShopSlotUI>();

        private void Start()
        {
            // Subscribe to events to refresh UI
            if (PlayFabManager.Instance != null)
            {
                PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataUpdated;
                PlayFabManager.Instance.OnLoginSuccess += RefreshShop;
            }

            RefreshShop();
        }

        private void OnDestroy()
        {
            if (PlayFabManager.Instance != null)
            {
                PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataUpdated;
                PlayFabManager.Instance.OnLoginSuccess -= RefreshShop;
            }
        }

        private void OnPlayerDataUpdated(PlayFabPlayerData data)
        {
            RefreshShop();
        }

        public void RefreshShop()
        {
            // Clear old slots
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }
            _spawnedSlots.Clear();

            // Spawn new slots
            foreach (var car in availableCars)
            {
                GameObject slotObj = Instantiate(shopSlotPrefab, contentContainer);
                var slotUI = slotObj.GetComponent<CarShopSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Init(car, this);
                    _spawnedSlots.Add(slotUI);
                }
            }

            UpdateCoinsDisplay();
        }

        private void UpdateCoinsDisplay()
        {
            if (coinsText && PlayFabManager.Instance != null)
            {
                coinsText.text = PlayFabManager.Instance.CurrentPlayerData.Coins.ToString();
            }
        }

        public void TryBuyCar(CarData car)
        {
            var pm = PlayFabManager.Instance;
            if (pm == null) return;

            if (pm.CurrentPlayerData.OwnedCars.Contains(car.CarID))
            {
                Debug.Log("Already own this car!");
                return;
            }

            if (pm.CurrentPlayerData.Coins >= car.Price)
            {
                // Transaction
                pm.CurrentPlayerData.Coins -= car.Price;
                pm.CurrentPlayerData.OwnedCars.Add(car.CarID);
                
                // Save
                pm.SavePlayerData(() => 
                {
                    Debug.Log($"Bought car {car.CarName} for {car.Price}");
                    RefreshShop(); // Update UI state
                });
            }
            else
            {
                Debug.Log("Not enough coins!");
                // Optional: Show "Not enough money" popup
            }
        }

        public void SelectCar(CarData car)
        {
            var pm = PlayFabManager.Instance;
            if (pm == null) return;

            if (pm.CurrentPlayerData.OwnedCars.Contains(car.CarID))
            {
                pm.CurrentPlayerData.SelectedCar = car.CarID;
                pm.SavePlayerData(() => 
                {
                    Debug.Log($"Selected car {car.CarName}");
                    RefreshShop();
                });
            }
        }
    }
}
