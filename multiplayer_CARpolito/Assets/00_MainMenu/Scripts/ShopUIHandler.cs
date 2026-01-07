using Starter.Shooter;
using System.Collections.Generic;
using UnityEngine;

public class ShopUIHandler : MonoBehaviour
{
    [SerializeField] private List<WeaponData> weaponsData;
    [SerializeField] private GameObject shopSlotPrefab;
    [SerializeField] private Transform content;
    private void Start()
    {
        foreach(WeaponData weaponData in weaponsData)
        {
            GameObject shopSlot= Instantiate(shopSlotPrefab, content);
            shopSlot.GetComponent<ShopSlotUIHandler>().Init(weaponData);
        }
    }
}
