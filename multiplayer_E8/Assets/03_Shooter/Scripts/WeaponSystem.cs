using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.Shooter
{
	/// <summary>
	/// Handles weapon firing logic for both hitscan and projectile weapons.
	/// Supports weapon switching and different weapon types.
	/// </summary>
	public class WeaponSystem : NetworkBehaviour
	{
		[SerializeField] private Transform rightHandSlot;
	[Header("References")]
	public Transform FirePoint;
	public LayerMask HitMask;
	public AudioSource AudioSource;
	public GameObject HitscanLineRendererPrefab; // Prefab for hitscan laser line renderer
	
	[Header("Hitscan Laser Settings")]
	[Tooltip("If true, the laser line will always be visible. If false, it only shows when firing.")]
	public bool AlwaysShowLaser = false;

		[Header("Current Weapon")]
		public WeaponData CurrentWeapon;

		[Networked]
		public int CurrentWeaponIndex { get; private set; }
		[Networked]
		private TickTimer _fireCooldown { get; set; }
		[Networked]
		private TickTimer _reloadTimer { get; set; }
		[Networked]
		private int _currentAmmo { get; set; }
		[Networked]
		private bool _isReloading { get; set; }

		// Networked hit data for hitscan weapons
		[Networked]
		private Vector3 _hitPosition { get; set; }
		[Networked]
		private Vector3 _hitNormal { get; set; }
		[Networked]
		public int FireCount { get; private set; }
        private ChangeDetector _changeDetector;
        // Local tracking
        private int _visibleFireCount;
	private ParticleSystem _currentMuzzleParticle;
	private List<LineRenderer> _hitscanLineRenderers = new List<LineRenderer>(); // Multiple line renderers for pellet spread
	private float _hitscanLineEndTime; // When to hide the hitscan line
	private List<Vector3> _hitscanLineStarts = new List<Vector3>(); // Start positions for each pellet
	private List<Vector3> _hitscanLineEnds = new List<Vector3>(); // End positions for each pellet

		// Weapon switching
		[Header("Available Weapons")]
		public WeaponData[] AvailableWeapons;

		public bool CanFire => !_isReloading && 
		                       _fireCooldown.ExpiredOrNotRunning(Runner) && 
		                       (CurrentWeapon.MaxAmmo == 0 || _currentAmmo > 0);

		public bool NeedsReload => CurrentWeapon != null && 
		                          CurrentWeapon.MaxAmmo > 0 && 
		                          _currentAmmo < CurrentWeapon.ClipSize && 
		                          !_isReloading;

		public int CurrentAmmo => _currentAmmo;
		public int MaxAmmo => CurrentWeapon != null ? CurrentWeapon.MaxAmmo : 0;
        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (HasStateAuthority && AvailableWeapons != null && AvailableWeapons.Length > 0)
            {
                int savedWeaponId = PlayerPrefs.GetInt("SelectedWeaponId", 0);
                int index = 0;
                for (int i = 0; i < AvailableWeapons.Length; i++)
                {
                    if (AvailableWeapons[i].WeaponID == savedWeaponId)
                    {
                        index = i;
                        break;
                    }
                }

                RPC_SetWeaponIndex(index); // ✅ this will propagate to all clients
            }

            _visibleFireCount = FireCount;

            if (FirePoint == null)
                FirePoint = transform;
        }


        public override void FixedUpdateNetwork()
		{
			if (CurrentWeapon == null)
				return;

			// Handle reload
			if (_isReloading && _reloadTimer.Expired(Runner))
			{
				ReloadComplete();
			}
		}

		public override void Render()
		{
			foreach (var change in _changeDetector.DetectChanges(this))
			{
				if (change == nameof(CurrentWeaponIndex))
				{
					SwitchWeapon(CurrentWeaponIndex);
				}
			}
			ShowFireEffects();
			UpdateHitscanLine();

			//For hitscan weapons, show laser lines if AlwaysShowLaser is enabled
			if (AlwaysShowLaser && CurrentWeapon != null && CurrentWeapon.Type == WeaponData.WeaponType.Hitscan)
				{
					UpdateHitscanLinesForSpread();
				}
				else if (!AlwaysShowLaser && CurrentWeapon != null && CurrentWeapon.Type == WeaponData.WeaponType.Hitscan)
				{
					// If AlwaysShowLaser is false and line is not being shown by firing, hide it
					if (Time.time >= _hitscanLineEndTime)
					{
						foreach (var lineRenderer in _hitscanLineRenderers)
						{
							if (lineRenderer != null)
							{
								lineRenderer.enabled = false;
							}
						}
					}
				}
		}

	/// <summary>
	/// Update all hitscan line renderers to show spread pattern.
	/// </summary>
	private void UpdateHitscanLinesForSpread()
	{
		if (FirePoint == null || _hitscanLineRenderers.Count == 0)
			return;

		Vector3 firePos = FirePoint.position;
		Vector3 fireDir = FirePoint.forward;
		float spread = CurrentWeapon.Spread;
		int pelletCount = CurrentWeapon.PelletCount;

		// Update each line renderer for each pellet
		for (int i = 0; i < _hitscanLineRenderers.Count && i < pelletCount; i++)
		{
			var lineRenderer = _hitscanLineRenderers[i];
			if (lineRenderer == null)
				continue;

			Vector3 direction = fireDir;

			// Apply horizontal line spread (same logic as firing)
			if (spread > 0f && pelletCount > 1)
			{
				int centerIndex = (pelletCount - 1) / 2;
				int offsetFromCenter = i - centerIndex;
				float horizontalAngle = offsetFromCenter * spread * Mathf.Deg2Rad;
				Quaternion horizontalRotation = Quaternion.Euler(0f, horizontalAngle, 0f);
				direction = horizontalRotation * fireDir;
			}

			// Calculate line end
			Vector3 lineEnd = firePos + direction * CurrentWeapon.Range;

			// Check for hit
			if (Physics.Raycast(firePos, direction, out var hit, CurrentWeapon.Range, HitMask))
			{
				lineEnd = hit.point;
			}

			// Ensure line end never exceeds weapon range
			float distanceToEnd = Vector3.Distance(firePos, lineEnd);
			if (distanceToEnd > CurrentWeapon.Range)
			{
				lineEnd = firePos + (lineEnd - firePos).normalized * CurrentWeapon.Range;
			}

			// Update line renderer
			lineRenderer.enabled = true;
			lineRenderer.SetPosition(0, firePos);
			lineRenderer.SetPosition(1, lineEnd);
		}
	}

		/// <summary>
		/// Fire the current weapon. Called from Player script.
		/// </summary>
		public bool TryFire(Vector3 firePosition, Vector3 fireDirection)
		{
			if (!CanFire || CurrentWeapon == null)
				return false;

			if (HasStateAuthority)
			{
				FireWeapon(firePosition, fireDirection);
				return true;
			}

			return false;
		}

		private void FireWeapon(Vector3 firePosition, Vector3 fireDirection)
		{
			// Clear hit position
			_hitPosition = Vector3.zero;

			if (CurrentWeapon.Type == WeaponData.WeaponType.Hitscan)
			{
				FireHitscan(firePosition, fireDirection);
			}
			else if (CurrentWeapon.Type == WeaponData.WeaponType.Projectile)
			{
				FireProjectile(firePosition, fireDirection);
			}

			// Consume ammo
			if (CurrentWeapon.MaxAmmo > 0)
			{
				_currentAmmo = Mathf.Max(0, _currentAmmo - 1);
			}

			// Set fire cooldown
			_fireCooldown = TickTimer.CreateFromSeconds(Runner, CurrentWeapon.FireCooldown);
			FireCount++;
		}

	private void FireHitscan(Vector3 firePosition, Vector3 fireDirection)
	{
		// Get spread and pellet count for hitscan
		float spread = CurrentWeapon.Spread;
		int pelletCount = CurrentWeapon.PelletCount;
		Vector3 lastHitPoint = firePosition + fireDirection * CurrentWeapon.Range;

		// Handle multiple pellets (shotgun)
		for (int i = 0; i < pelletCount; i++)
		{
			Vector3 direction = fireDirection;
			Vector3 lineStart = firePosition;
			Vector3 lineEnd = firePosition + direction * CurrentWeapon.Range;

			// Apply horizontal line spread (same logic as projectiles)
			if (spread > 0f && pelletCount > 1)
			{
				// Calculate angle for this pellet
				// spread = angle between consecutive pellets
				// For count = 5, spread = 5°: angles are -10°, -5°, 0°, +5°, +10°
				// Total spread = (pelletCount - 1) * spread
				// Center index = (pelletCount - 1) / 2
				
				int centerIndex = (pelletCount - 1) / 2; // Middle pellet index
				int offsetFromCenter = i - centerIndex; // How many steps from center
				
				// Calculate angle: center is 0°, each step is 'spread' degrees
				float horizontalAngle = offsetFromCenter * spread * Mathf.Deg2Rad;
				
				// Apply horizontal rotation (Y axis rotation)
				Quaternion horizontalRotation = Quaternion.Euler(0f, horizontalAngle, 0f);
				direction = horizontalRotation * fireDirection;
			}
			else if (spread > 0f && pelletCount == 1)
			{
				// Single pellet with random spread (circular)
				float spreadAngle = spread * Mathf.Deg2Rad;
				Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spreadAngle;
				Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0f);
				direction = spreadRotation * fireDirection;
			}

			// Raycast
			if (Physics.Raycast(firePosition, direction, out var hitInfo, CurrentWeapon.Range, HitMask))
			{
				// Deal damage
				var health = hitInfo.collider != null ? hitInfo.collider.GetComponentInParent<Health>() : null;
				if (health != null && health.IsAlive)
				{
					// Set up kill callback for scoring (same as legacy fire system)
					SetupKillCallback(health);

                    // FORCE ONE-SHOT KILL in Shooter (Scene 3) and always in TieBreaker
                    string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (currentScene.Contains("Shooter") || currentScene.Contains("03") || (GetComponentInParent<Player>() != null))
                    {
                        health.TakeHit(1000, true); // Massive damage for 1-shot kill
                    }
                    else
                    {
                        health.TakeHit(CurrentWeapon.Damage, true);
                    }
				}


				// Save hit point (only last hit for network sync)
				if (i == pelletCount - 1)
				{
					_hitPosition = hitInfo.point;
					_hitNormal = hitInfo.normal;
					lastHitPoint = hitInfo.point;
				}

				lineEnd = hitInfo.point;
			}

			// Ensure line end never exceeds weapon range
			float distanceToEnd = Vector3.Distance(lineStart, lineEnd);
			if (distanceToEnd > CurrentWeapon.Range)
			{
				lineEnd = lineStart + (lineEnd - lineStart).normalized * CurrentWeapon.Range;
			}

			// Store line positions for this pellet
			if (i < _hitscanLineStarts.Count && i < _hitscanLineEnds.Count)
			{
				_hitscanLineStarts[i] = lineStart;
				_hitscanLineEnds[i] = lineEnd;
			}
			else
			{
				// Expand lists if needed
				while (_hitscanLineStarts.Count <= i)
					_hitscanLineStarts.Add(Vector3.zero);
				while (_hitscanLineEnds.Count <= i)
					_hitscanLineEnds.Add(Vector3.zero);
				_hitscanLineStarts[i] = lineStart;
				_hitscanLineEnds[i] = lineEnd;
			}

			// Update line renderer for this pellet if it exists
			if (i < _hitscanLineRenderers.Count && _hitscanLineRenderers[i] != null)
			{
				var lineRenderer = _hitscanLineRenderers[i];
				lineRenderer.enabled = true;
				lineRenderer.SetPosition(0, lineStart);
				lineRenderer.SetPosition(1, lineEnd);
			}
		}

		// Show visual raycast lines when firing
		ShowHitscanLine(firePosition, lastHitPoint);
	}

		private void FireProjectile(Vector3 firePosition, Vector3 fireDirection)
		{
			if (CurrentWeapon.ProjectilePrefab == null)
			{
				Debug.LogWarning($"[WeaponSystem] Projectile prefab not set for weapon: {CurrentWeapon.WeaponName}");
				return;
			}

			// Check if prefab has NetworkObject component
			var networkObject = CurrentWeapon.ProjectilePrefab.GetComponent<NetworkObject>();
			if (networkObject == null)
			{
				Debug.LogError($"[WeaponSystem] Projectile prefab '{CurrentWeapon.ProjectilePrefab.name}' must have a NetworkObject component!");
				return;
			}

			// Get projectile spread and pellet count
			// For projectile weapons, always use ProjectileSpread if it's set (even if 0)
			// Use ProjectilePelletCount if > 1, otherwise use PelletCount
			float spread = CurrentWeapon.ProjectileSpread; // Always use ProjectileSpread for projectile weapons
			int pelletCount = CurrentWeapon.ProjectilePelletCount > 1 ? CurrentWeapon.ProjectilePelletCount : CurrentWeapon.PelletCount;
			
			Debug.Log($"[WeaponSystem] Projectile weapon - Using Spread: {spread}° (ProjectileSpread: {CurrentWeapon.ProjectileSpread}°), Pellet count: {pelletCount} (ProjectilePelletCount: {CurrentWeapon.ProjectilePelletCount}, PelletCount: {CurrentWeapon.PelletCount})");

			// Spawn multiple projectiles if pellet count > 1
			// All projectiles start from the same point (firePosition) and spread by angle only
			for (int i = 0; i < pelletCount; i++)
			{
				Vector3 direction = fireDirection;

				// Apply horizontal line spread to projectile direction
				if (spread > 0f && pelletCount > 1)
				{
					// Calculate angle for this projectile
					// spread = angle between consecutive projectiles
					// For count = 5, spread = 5°: angles are -10°, -5°, 0°, +5°, +10°
					// Total spread = (pelletCount - 1) * spread
					// Center index = (pelletCount - 1) / 2
					
					int centerIndex = (pelletCount - 1) / 2; // Middle projectile index
					int offsetFromCenter = i - centerIndex; // How many steps from center
					
					// Calculate angle: center is 0°, each step is 'spread' degrees
					float horizontalAngle = offsetFromCenter * spread * Mathf.Deg2Rad;
					
					// Apply horizontal rotation (Y axis rotation)
					Quaternion horizontalRotation = Quaternion.Euler(0f, horizontalAngle, 0f);
					direction = horizontalRotation * fireDirection;
				}
				else if (spread > 0f && pelletCount == 1)
				{
					// Single projectile with random spread (circular)
					float spreadAngle = spread * Mathf.Deg2Rad;
					Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spreadAngle;
					Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0f);
					direction = spreadRotation * fireDirection;
				}

				// All projectiles spawn from the same point (firePosition)
				// Only the direction differs based on spread angle
				Vector3 spawnOffset = direction * 0.5f; // Small forward offset to avoid player collision
				Vector3 spawnPosition = firePosition + spawnOffset;

				// Spawn projectile
				NetworkObject projectile = null;
				try
				{
					projectile = Runner.Spawn(
						CurrentWeapon.ProjectilePrefab,
						spawnPosition,
						Quaternion.LookRotation(direction)
					);
				}
				catch (System.Exception e)
				{
					Debug.LogError($"[WeaponSystem] Failed to spawn projectile {i + 1}/{pelletCount}: {e.Message}");
					continue;
				}

				if (projectile == null)
				{
					Debug.LogError($"[WeaponSystem] Runner.Spawn returned null for projectile {i + 1}/{pelletCount}!");
					continue;
				}

				// Initialize projectile
				var projectileComponent = projectile.GetComponent<Projectile>();
				if (projectileComponent == null)
				{
					Debug.LogError($"[WeaponSystem] Projectile prefab '{CurrentWeapon.ProjectilePrefab.name}' must have a Projectile component!");
					continue;
				}

				// Get the player who owns this weapon system
				Player ownerPlayer = GetComponentInParent<Player>();
				PlayerRef ownerPlayerRef = ownerPlayer != null && ownerPlayer.Object != null ? ownerPlayer.Object.InputAuthority : Runner.LocalPlayer;
				
				projectileComponent.Initialize(
					direction,
					CurrentWeapon.ProjectileSpeed,
					CurrentWeapon.ProjectileGravity,
					CurrentWeapon.Damage,
					CurrentWeapon.Range,
					CurrentWeapon.ProjectileLifetime,
					HitMask,
					ownerPlayerRef
				);
			}

			Debug.Log($"[WeaponSystem] Spawned {pelletCount} projectile(s) at {firePosition} with spread {spread}°");
		}

		/// <summary>
		/// Show visual lines for hitscan raycast for a short duration (one per pellet).
		/// </summary>
		private void ShowHitscanLine(Vector3 start, Vector3 end)
		{
			// This is called for the last pellet only, but we'll update all lines from FireHitscan
			_hitscanLineEndTime = Time.time + 0.15f; // Show lines for 0.15 seconds
		}

		/// <summary>
		/// Update hitscan line visibility - hide after duration.
		/// </summary>
		private void UpdateHitscanLine()
		{
			if (!AlwaysShowLaser && Time.time >= _hitscanLineEndTime)
			{
				// Hide all lines after duration (if not in always-show mode)
				foreach (var lineRenderer in _hitscanLineRenderers)
				{
					if (lineRenderer != null && lineRenderer.enabled)
					{
						lineRenderer.enabled = false;
					}
				}
			}
		}

		/// <summary>
		/// Set up the kill callback on health component for scoring.
		/// </summary>
		private void SetupKillCallback(Health health)
		{
			// Find the player who owns this weapon system
			Player ownerPlayer = GetComponentInParent<Player>();
			
			if (ownerPlayer != null)
			{
				health.Killed = ownerPlayer.OnEnemyKilled;
			}
		}

		private void ShowFireEffects()
		{
			if (CurrentWeapon == null)
				return;

			// Show fire effects when fire count increases
			if (_visibleFireCount < FireCount)
			{
				// Play sound
				if (CurrentWeapon.FireSoundClip != null && AudioSource != null)
				{
					AudioSource.PlayOneShot(CurrentWeapon.FireSoundClip, CurrentWeapon.FireSoundVolume);
				}

				// Play muzzle particle
				if (_currentMuzzleParticle != null)
				{
					_currentMuzzleParticle.Play();
				}

			// Show impact effect
			if (_hitPosition != Vector3.zero && CurrentWeapon.ImpactPrefab != null)
			{
				Instantiate(CurrentWeapon.ImpactPrefab, _hitPosition, Quaternion.LookRotation(_hitNormal));
			}
		}

		_visibleFireCount = FireCount;
		}

        /// <summary>
        /// Switch to a different weapon by index.
        /// </summary>
        [SerializeField]private GameObject _currentWeaponModel;
        public void SwitchWeapon(int index)
        {
            if (AvailableWeapons == null || index < 0 || index >= AvailableWeapons.Length)
                return;

            CurrentWeapon = AvailableWeapons[index];

            // Destroy old model
            if (_currentWeaponModel != null)
                Destroy(_currentWeaponModel);

            // Spawn new model
            if (CurrentWeapon.weaponPrefab != null && rightHandSlot != null)
            {
                _currentWeaponModel = Instantiate(CurrentWeapon.weaponPrefab, rightHandSlot);
                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.Euler(0, -90, 0);
                _currentWeaponModel.transform.localScale = Vector3.one;

                // Sync layer with parent (important for FirstPersonOverlay visibility)
                SetLayerRecursive(_currentWeaponModel, rightHandSlot.gameObject.layer);
            }

            // Reset ammo (state authority only)
            if (HasStateAuthority)
            {
                _currentAmmo = CurrentWeapon.MaxAmmo > 0
                    ? CurrentWeapon.ClipSize
                    : -1;
            }

            //SetupHitscanLineRenderers();
        }

        /// <summary>
        /// Create line renderers for hitscan weapons based on pellet count.
        /// </summary>
        private void SetupHitscanLineRenderers()
		{
			// Clean up old line renderers
			CleanupHitscanLineRenderers();

			// Only create line renderers for hitscan weapons
			if (CurrentWeapon == null || CurrentWeapon.Type != WeaponData.WeaponType.Hitscan)
				return;

			if (HitscanLineRendererPrefab == null)
			{
				Debug.LogWarning("[WeaponSystem] HitscanLineRendererPrefab is not assigned!");
				return;
			}

			// Get pellet count
			int pelletCount = CurrentWeapon.PelletCount;
			if (pelletCount < 1)
				pelletCount = 1;

			// Create line renderer for each pellet
			for (int i = 0; i < pelletCount; i++)
			{
				GameObject lineObj = Instantiate(HitscanLineRendererPrefab, FirePoint != null ? FirePoint : transform);
				lineObj.name = $"HitscanLine_{i}";
				
				LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();
				if (lineRenderer == null)
				{
					lineRenderer = lineObj.AddComponent<LineRenderer>();
				}

				// Ensure it's configured properly
				lineRenderer.useWorldSpace = true;
				lineRenderer.positionCount = 2;
				lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				lineRenderer.receiveShadows = false;
				lineRenderer.enabled = false;

				_hitscanLineRenderers.Add(lineRenderer);
			}

			// Initialize position lists
			_hitscanLineStarts.Clear();
			_hitscanLineEnds.Clear();
			for (int i = 0; i < pelletCount; i++)
			{
				_hitscanLineStarts.Add(Vector3.zero);
				_hitscanLineEnds.Add(Vector3.zero);
			}
		}

		/// <summary>
		/// Clean up all hitscan line renderers.
		/// </summary>
		private void CleanupHitscanLineRenderers()
		{
			foreach (var lineRenderer in _hitscanLineRenderers)
			{
				if (lineRenderer != null && lineRenderer.gameObject != null)
				{
					Destroy(lineRenderer.gameObject);
				}
			}
			_hitscanLineRenderers.Clear();
		}

		/// <summary>
		/// Start reloading the weapon.
		/// </summary>
		public void StartReload()
		{
			if (!NeedsReload || _isReloading)
				return;

			if (HasStateAuthority)
			{
				_isReloading = true;
				_reloadTimer = TickTimer.CreateFromSeconds(Runner, CurrentWeapon.ReloadTime);
			}
		}

		private void ReloadComplete()
		{
			_currentAmmo = CurrentWeapon.ClipSize;
			_isReloading = false;
		}

		/// <summary>
		/// Add ammo to the weapon.
		/// </summary>
		public void AddAmmo(int amount)
		{
			if (CurrentWeapon.MaxAmmo == 0)
				return; // Weapon doesn't use ammo

			if (HasStateAuthority)
			{
				_currentAmmo = Mathf.Min(CurrentWeapon.MaxAmmo, _currentAmmo + amount);
			}
		}
        public void SetWeaponIndex(int index)
        {
            if (!HasStateAuthority)
                return;

            CurrentWeaponIndex = index;
        }
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_SetWeaponIndex(int index)
        {
            CurrentWeaponIndex = index;
            SwitchWeapon(index);
        }

        private void SetLayerRecursive(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, newLayer);
            }
        }
    }
}