using Starter.Lobby;
using Starter.PlayFabIntegration;
using Starter.Shooter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUIHandler : MonoBehaviour
{
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI priceTxt;
    [SerializeField] private Button buyBtn;

    private WeaponData weaponData;

    public void Init(WeaponData data)
    {
        weaponData = data;
        weaponImage.sprite = weaponData.WeaponIcon;

        buyBtn.onClick.RemoveAllListeners();

        if (weaponData.IsSpecial)
        {
            buyBtn.onClick.AddListener(OnSpecialAction);
        }
        else
        {
            buyBtn.onClick.AddListener(BuyWeapon);
        }

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        bool isOwned = IsWeaponOwned();

        if (isOwned)
        {
            buyBtn.interactable = false;
            priceTxt.text = "OWNED"; 
            if(priceTxt.color != Color.green) priceTxt.color = Color.green;
        }
        else
        {
            buyBtn.interactable = true;
            if (weaponData.IsSpecial)
            {
                 priceTxt.text = "INVITE";
                 priceTxt.color = Color.yellow; // Highlight special
            }
            else
            {
                priceTxt.text = $"{weaponData.Price}";
                priceTxt.color = Color.white;
            }
        }
    }

    private bool IsWeaponOwned()
    {
        var manager = PlayFabManager.Instance;
        
        // Check local or cloud data transparently via PlayFabManager
        if (manager != null && manager.CurrentPlayerData != null)
        {
            return manager.CurrentPlayerData.OwnedWeapons.Contains(weaponData.WeaponID);
        }
        return false;
    }

    private void BuyWeapon()
    {
        var integration = PlayFabGameIntegration.Instance;
        if (integration == null) 
        {
            Debug.LogError("PlayFabGameIntegration not found!");
            return;
        }

        // Double check ownership
        if (IsWeaponOwned()) return;

        // Try to spend coins (Handling Guest/User abstraction)
        if (integration.SpendCoins(weaponData.Price))
        {
             // Add Weapon to Inventory
             var manager = PlayFabManager.Instance;
             manager.CurrentPlayerData.OwnedWeapons.Add(weaponData.WeaponID);
             manager.SavePlayerData(); // Persist changes (Disk for Guest, Cloud for User)
             
             // Update this Slot UI
             UpdateButtonState();
             Debug.Log($"[Shop] Purchased {weaponData.WeaponName}");
             
             // Refresh Main Menu Carousel if it exists
             var mainMenu = FindObjectOfType<MainMenuController>();
             if(mainMenu != null) 
             {
                 mainMenu.PopulateWeaponCarousel();
             }
        }
        else
        {
            Debug.Log("[Shop] Not enough coins!");
            ShowNoMoneyFeedback();
        }
    }

    private void ShowNoMoneyFeedback()
    {
         priceTxt.text = "NO MONEY";
         priceTxt.color = Color.red;
         
         // Reset UI state after 1 second
         CancelInvoke(nameof(UpdateButtonState));
         Invoke(nameof(UpdateButtonState), 1.0f);
    }

    private void OnSpecialAction()
    {
        Debug.Log("[Shop] Special Weapon Action: Showing Referral Popup");
        
        var referralUI = FindObjectOfType<ReferralUIHandler>(true); // Find even if disabled
        if (referralUI != null)
        {
            referralUI.Show();
        }
        else
        {
            Debug.LogError("ReferralUIHandler not found in scene!");
        }
    }
}
