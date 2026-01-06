using UnityEngine;

namespace Mautrack.Data
{
    [CreateAssetMenu(fileName = "NewCar", menuName = "Mautrack/Car Data", order = 1)]
    public class CarData : ScriptableObject
    {
        public int CarID;
        
        [Header("Display Info")]
        public string CarName = "New Car";
        public Sprite CarIcon;
        [TextArea] public string Description = "A fast and reliable car.";
        
        [Header("Economy")]
        public int Price = 500;
        public bool IsDefault = false; // Is this car unlocked by default?

        [Header("Prefab Reference")]
        public GameObject CarPrefab; // The multiplayer vehicle prefab

        [Header("Stats (Visual Only)")]
        [Range(1, 10)] public int SpeedStat = 5;
        [Range(1, 10)] public int HandlingStat = 5;
        [Range(1, 10)] public int AccelerationStat = 5;
    }
}
