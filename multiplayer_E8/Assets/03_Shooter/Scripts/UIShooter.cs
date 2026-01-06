using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starter.Shooter
{
    /// <summary>
    /// Main UI script for Shooter sample.
    /// </summary>
    public class UIShooter : MonoBehaviour
    {
        [Header("References")]
        public GameManager GameManager;
        public CanvasGroup CanvasGroup;
        public TextMeshProUGUI ChickenCount;
        public TextMeshProUGUI BestHunter;
        public GameObject AliveGroup;
        public GameObject DeathGroup;
        public Image[] HealthIndicators;
        public CanvasGroup HitIndicator;

        [Header("UI Sound Setup")]
        public AudioSource AudioSource;
        public AudioClip ChickenKillClip;
        public AudioClip HitReceivedClip;
        public AudioClip DeathClip;

        private int _lastChickens = -1;
        private int _lastHealth = -1;
        private PlayerRef _bestHunter;

        private void OnEnable()
        {
            BestHunter.gameObject.SetActive(false);
        }

        private void Update()
        {
            var player = GameManager.LocalPlayer;  
            if (player != null && _lastHealth != player.Health.CurrentHealth)
            {
                bool isAlive = player.Health.IsAlive;

                if (_lastHealth > player.Health.CurrentHealth)
                {
                    // Show hit received
                    HitIndicator.alpha = 1f;

                    var clip = isAlive ? HitReceivedClip : DeathClip;
                    AudioSource.PlayOneShot(clip);
                }

                _lastHealth = player.Health.CurrentHealth;

                AliveGroup.SetActive(isAlive);
                DeathGroup.SetActive(isAlive == false);

                for (int i = 0; i < HealthIndicators.Length; i++)
                {
                    HealthIndicators[i].enabled = _lastHealth > i;
                }
            }
        }
    }
}