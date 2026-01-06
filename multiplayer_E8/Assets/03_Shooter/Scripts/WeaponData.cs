using UnityEngine;

namespace Starter.Shooter
{
	/// <summary>
	/// ScriptableObject that defines weapon properties.
	/// Create different weapon types by creating assets from this class.
	/// </summary>
	[CreateAssetMenu(fileName = "NewWeapon", menuName = "Shooter/Weapon Data", order = 1)]
	public class WeaponData : ScriptableObject
	{
		public int WeaponID;
        [Header("Weapon Info")]
		public string WeaponName = "Weapon";
		public Sprite WeaponIcon;
		public GameObject weaponPrefab;
        public int Price = 200;
        
        [Header("Special Unlock")]
        public bool IsSpecial = false;
        [Header("Weapon Type")]
		public WeaponType Type = WeaponType.Hitscan;
		
		[Header("Combat Stats")]
		[Tooltip("Damage per shot")]
		public int Damage = 1;
		[Tooltip("Range in units (for hitscan) or max travel distance (for projectiles)")]
		public float Range = 200f;
		[Tooltip("Shots per second")]
		public float FireRate = 5f;
		[Tooltip("Time in seconds between shots")]
		public float FireCooldown => 1f / FireRate;

		[Header("Hitscan Settings")]
		[Tooltip("Spread angle in degrees (0 = perfect accuracy)")]
		public float Spread = 0f;
		[Tooltip("Number of rays to cast (for shotguns)")]
		public int PelletCount = 1;

		[Header("Projectile Settings")]
		[Tooltip("Projectile prefab to spawn (only used if Type is Projectile)")]
		public GameObject ProjectilePrefab;
		[Tooltip("Projectile speed in units per second")]
		public float ProjectileSpeed = 50f;
		[Tooltip("Gravity applied to projectile (0 = no gravity)")]
		public float ProjectileGravity = 0f;
		[Tooltip("Lifetime of projectile in seconds")]
		public float ProjectileLifetime = 5f;
		[Tooltip("Spread angle in degrees for projectiles (0 = perfect accuracy). Uses Spread from Hitscan Settings if 0.")]
		public float ProjectileSpread = 0f;
		[Tooltip("Number of projectiles to spawn per shot (for shotguns). Uses PelletCount from Hitscan Settings if 1.")]
		public int ProjectilePelletCount = 1;

		[Header("Visual & Audio")]
		public GameObject ImpactPrefab;
		public ParticleSystem MuzzleParticlePrefab;
		public AudioClip FireSoundClip;
		[Tooltip("Volume for fire sound (0-1)")]
		[Range(0f, 1f)]
		public float FireSoundVolume = 1f;

		[Header("Ammo (Optional)")]
		[Tooltip("If > 0, weapon uses ammo system")]
		public int MaxAmmo = 0;
		[Tooltip("Ammo per clip/magazine")]
		public int ClipSize = 0;
		[Tooltip("Reload time in seconds")]
		public float ReloadTime = 2f;

		public enum WeaponType
		{
			Hitscan,    // Instant raycast
			Projectile  // Spawns physical projectile
		}
	}

}