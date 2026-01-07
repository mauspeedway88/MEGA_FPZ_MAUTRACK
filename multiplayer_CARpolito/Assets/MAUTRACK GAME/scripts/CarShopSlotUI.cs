using UnityEngine;
using UnityEngine.UI;
using Mautrack.Data;
using Mautrack.PlayFabIntegration;

namespace Mautrack.UI
{
    public class CarShopSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text carNameText;
        [SerializeField] private Text priceText;
        [SerializeField] private Image carIconImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buttonText; // To show "BUY" or "OWNED"

        private CarData _data;
        private CarShopUI _shop;

        public void Init(CarData data, CarShopUI shop)
        {
            _data = data;
            _shop = shop;

            UpdateUI();
        }

        public void UpdateUI()
        {
            if (_data == null) return;

            if (carNameText) carNameText.text = _data.CarName;
            if (priceText) priceText.text = _data.Price.ToString();
            if (carIconImage) carIconImage.sprite = _data.CarIcon;

            bool isOwned = PlayFabManager.Instance.CurrentPlayerData.OwnedCars.Contains(_data.CarID);
            bool isSelected = PlayFabManager.Instance.CurrentPlayerData.SelectedCar == _data.CarID;

            if (buyButton)
            {
                buyButton.onClick.RemoveAllListeners();
                
                if (isOwned)
                {
                    if (isSelected)
                    {
                        if (buttonText) buttonText.text = "EQUIPPED";
                        buyButton.interactable = false;
                    }
                    else
                    {
                        if (buttonText) buttonText.text = "EQUIP";
                        buyButton.interactable = true;
                        buyButton.onClick.AddListener(() => _shop.SelectCar(_data));
                    }
                }
                else
                {
                    if (buttonText) buttonText.text = _data.Price.ToString(); // Show Price
                    buyButton.interactable = true;
                    buyButton.onClick.AddListener(() => _shop.TryBuyCar(_data));
                }
            }
        }
    }
}
