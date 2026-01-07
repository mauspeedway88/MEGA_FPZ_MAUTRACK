using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Starter.Shooter;

public class WeaponItem : MonoBehaviour
{
    public Image weaponIcon;
    public TextMeshProUGUI weaponNameText;
    public Button selectButton;

    [HideInInspector] public WeaponData weaponData;
    [HideInInspector] public int WeaponId;
    public void Setup(WeaponData weaponData, System.Action<int> onSelect)
    {
        WeaponId = weaponData.WeaponID;
        if (weaponIcon != null) weaponIcon.sprite = weaponData.WeaponIcon;
        if (weaponNameText != null) weaponNameText.text = weaponData.WeaponName;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(WeaponId));
        }
    }

}
