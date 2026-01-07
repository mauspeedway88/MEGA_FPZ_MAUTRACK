using Fusion;
using Fusion.Addons.SimpleKCC;
using Starter.PlayFabIntegration;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Starter.Shooter
{
	/// <summary>
	/// Main player scrip - controls player movement and animations.
	/// </summary>
	public sealed class Player : NetworkBehaviour
	{
	[Header("References")]
    public UnityStandardAssets.Vehicles.Car.CarController CarController; // [NEW] Car Reference
	public Health Health;
	public SimpleKCC KCC;
	public PlayerInput PlayerInput;
	public Animator Animator;
	public Transform CameraPivot;
	public Transform CameraHandle;
	public Transform ScalingRoot;
	public UINameplate Nameplate;
	public Collider Hitbox;
	public Renderer[] HeadRenderers;
	public GameObject[] FirstPersonOverlayObjects;
	public WeaponSystem WeaponSystem;

		[Header("Movement Setup")]
		public float WalkSpeed = 2f;
		public float JumpImpulse = 10f;
		public float UpGravity = 25f;
		public float DownGravity = 40f;

		[Header("Movement Accelerations")]
		public float GroundAcceleration = 55f;
		public float GroundDeceleration = 25f;
		public float AirAcceleration = 25f;
		public float AirDeceleration = 1.3f;

	[Header("Fire Setup")]
	[Tooltip("Legacy fire setup - only used if WeaponSystem is not assigned")]
	public LayerMask HitMask;
	[Tooltip("Legacy fire setup - only used if WeaponSystem is not assigned")]
	public GameObject ImpactPrefab;
	[Tooltip("Legacy fire setup - only used if WeaponSystem is not assigned")]
	public ParticleSystem MuzzleParticle;

		[Header("Animation Setup")]
		public Transform ChestTargetPosition;
		public Transform ChestBone;

	[Header("Sounds")]
	[Tooltip("Legacy fire sound - only used if WeaponSystem is not assigned")]
	public AudioSource FireSound;
	public AudioSource FootstepSound;
	public AudioClip JumpAudioClip;
	public AudioClip LandAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		[Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
		public string Nickname { get; set; }
		[Networked, HideInInspector]
		//public int ChickenKills { get; set; }
		public int Kills { get; set; }
		[Networked, OnChangedRender(nameof(OnJumpingChanged))]
		private NetworkBool _isJumping { get; set; }
		[Networked]
		private Vector3 _hitPosition { get; set; }
		[Networked]
		private Vector3 _hitNormal { get; set; }
		[Networked]
		private int _fireCount { get; set; }

		// Animation IDs
		private int _animIDSpeedX;
		private int _animIDSpeedZ;
		private int _animIDMoveSpeedZ;
		private int _animIDGrounded;
		private int _animIDPitch;
		private int _animIDShoot;

		private Vector3 _moveVelocity;
		private int _visibleFireCount;

		private GameManager _gameManager;
        private List<int> _ownedWeaponIDs = new List<int>();
		public override void Spawned()
		{
			//ChickenKills = 0;
            if (HasStateAuthority)
            {
                _gameManager = FindObjectOfType<GameManager>();

                // Set player nickname
                Nickname = PlayerPrefs.GetString("PlayerName");

                // Fetch owned weapons from PlayFab
                if (PlayFabManager.Instance != null)
                {
                    _ownedWeaponIDs = PlayFabManager.Instance.CurrentPlayerData.OwnedWeapons;
                }

                if (WeaponSystem != null && WeaponSystem.AvailableWeapons != null && WeaponSystem.AvailableWeapons.Length > 0)
                {
                    int selectedWeaponId = PlayerPrefs.GetInt("SelectedWeaponId", 0);

                    for (int i = 0; i < WeaponSystem.AvailableWeapons.Length; i++)
                    {
                        if (WeaponSystem.AvailableWeapons[i].WeaponID == selectedWeaponId
                            && _ownedWeaponIDs.Contains(selectedWeaponId)) // ✅ check ownership
                        {
                            WeaponSystem.SwitchWeapon(i);
                            break;
                        }
                    }
                }
            }

            OnNicknameChanged();
            _visibleFireCount = _fireCount;
            if (HasStateAuthority)
			{
				// For input authority deactivate head renderers so they are not obstructing the view
				for (int i = 0; i < HeadRenderers.Length; i++)
				{
					HeadRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
				}

				// Some objects (e.g. weapon) are renderer with secondary Overlay camera.
				// This prevents weapon clipping into the wall when close to the wall.
				int overlayLayer = LayerMask.NameToLayer("FirstPersonOverlay");
				for (int i = 0; i < FirstPersonOverlayObjects.Length; i++)
				{
					FirstPersonOverlayObjects[i].layer = overlayLayer;
				}

				// Look rotation interpolation is skipped for local player.
				// Look rotation is set manually in Render.
                if (KCC != null) // Check null
				    KCC.Settings.ForcePredictedLookRotation = true;
                
                // FIX: Force KCC AND PlayerInput to respect the spawn rotation immediately
                // SimpleKCC uses (Pitch, Yaw). We only care about Yaw (horizontal) for spawn direction.
                float spawnYaw = transform.rotation.eulerAngles.y;
                Vector2 initialRotation = new Vector2(0f, spawnYaw); // Pitch=0, Yaw from spawn
                
                // Set KCC look rotation
                if (KCC != null) KCC.SetLookRotation(initialRotation);
                
                // CRITICAL: Also initialize PlayerInput's internal LookRotation
                // Otherwise the input system starts at (0,0) and immediately overrides our spawn rotation!
                if (PlayerInput != null)
                {
                    PlayerInput.SetInitialLookRotation(initialRotation);
                }
			}
		}

		public override void FixedUpdateNetwork()
		{
            // [CAR MODE]
            if (CarController != null)
            {
                var carInput = Health.IsAlive ? PlayerInput.CurrentInput : default;
                
                // Map Input: X = Steering, Y = Accel/Brake
                float h = carInput.MoveDirection.x;
                float v = carInput.MoveDirection.y;
                float handbrake = carInput.Jump ? 1f : 0f;
                
                // Move Car
                CarController.Move(h, v, v, handbrake);

                // Handle Respawn/Death for Car?
                // For now, infinite health or handled by Health component if attached.
                
                PlayerInput.ResetInput();
                return; // SKIP SOLDIER LOGIC
            }

			// Check if player fell below map (but give some grace time after spawn)
			if (KCC.Position.y < -15f)
			{
				// Player fell, let's kill him
				Health.TakeHit(1000);
			}

			// Debug health status
			if (HasStateAuthority && !Health.IsAlive && Health.CurrentHealth <= 0)
			{
				Debug.LogWarning($"[Player] Player is dead! Health: {Health.CurrentHealth}, Position: {KCC.Position}");
			}

			if (Health.IsFinished)
			{
                // Find BattleTimer to check if game is over
                var timer = FindObjectOfType<BattleTimer>();
                bool gameEnded = (timer != null && timer.Object != null && timer.Object.IsValid && timer.BattleEnded);

                // Prevent Respawn if TieBreaker OR GameEnded
                if ((_gameManager != null && _gameManager.IsTieBreaker) || gameEnded)
                {
                    // Logic handled by GameManager or Event. Do NOT respawn self.
                }
                else
                {
				    // Player is dead and death timer is finished, let's respawn the player
                    var (spawnPos, spawnRot) = _gameManager.GetSpawnPositionAndRotation();
				    Respawn(spawnPos, spawnRot);
                }
			}

			var input = Health.IsAlive ? PlayerInput.CurrentInput : default;
			ProcessInput(input);

			if (KCC.IsGrounded)
			{
				// Stop jumping
				_isJumping = false;
			}

			if(KCC != null) KCC.SetActive(Health.IsAlive);

			PlayerInput.ResetInput();
		}

		public override void Render()
		{
			if (HasStateAuthority)
			{
				// Set look rotation for Render.
				if(KCC != null) KCC.SetLookRotation(PlayerInput.CurrentInput.LookRotation, -90f, 90f);
			}

            if (CarController != null) return; // Skip Soldier Animation/Scaling

			// Transform velocity vector to local space.
			var moveSpeed = transform.InverseTransformVector(KCC.RealVelocity);

			Animator.SetFloat(_animIDSpeedX, moveSpeed.x, 0.1f, Time.deltaTime);
			Animator.SetFloat(_animIDSpeedZ, moveSpeed.z, 0.1f, Time.deltaTime);
			Animator.SetBool(_animIDGrounded, KCC.IsGrounded);
			Animator.SetFloat(_animIDPitch, KCC.GetLookRotation(true, false).x, 0.02f, Time.deltaTime);

			FootstepSound.enabled = KCC.IsGrounded && KCC.RealSpeed > 1f;
			ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, Time.deltaTime * 8f);

			var emission = DustParticles.emission;
			emission.enabled = KCC.IsGrounded && KCC.RealSpeed > 1f;

			ShowFireEffects();

			// Disable hits when player is dead
			Hitbox.enabled = Health.IsAlive;
		}

		private void Awake()
		{
			AssignAnimationIDs();
		}

		private void LateUpdate()
		{
			if (Health.IsAlive == false)
				return;

			// Update camera pivot (influences ChestIK)
			// (KCC look rotation is set earlier in Render)
			var pitchRotation = (KCC != null) ? KCC.GetLookRotation(true, false) : Vector2.zero;
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			// Dummy IK solution, we are snapping chest bone to prepared ChestTargetPosition position
			// Lerping blends the fixed position with little bit of animation position.
			float blendAmount = HasStateAuthority ? 0.05f : 0.2f;
			ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
			ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);

			// Only local player needs to update the camera
			if (HasStateAuthority)
			{
				// Transfer properties from camera handle to Main Camera.
				Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
			}
		}

		private void ProcessInput(GameplayInput input)
		{
			KCC.SetLookRotation(input.LookRotation, -90f, 90f);

			// It feels better when player falls quicker
			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? UpGravity : DownGravity);

			// Calculate correct move direction from input (rotated based on latest KCC rotation)
			var moveDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			var desiredMoveVelocity = moveDirection * WalkSpeed;

			float acceleration;
			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping.
				acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
			float jumpImpulse = 0f;

			// Comparing current input buttons to previous input buttons - this prevents glitches when input is lost
			if (KCC.IsGrounded && input.Jump)
			{
				// Set world space jump vector
				jumpImpulse = JumpImpulse;
				_isJumping = true;
			}

			KCC.Move(_moveVelocity, jumpImpulse);

			// Update camera pivot so fire transform (CameraHandle) is correct
			var pitchRotation = KCC.GetLookRotation(true, false);
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

            // Handle weapon switching
            if (input.SwitchWeapon != 0 && WeaponSystem != null && WeaponSystem.AvailableWeapons != null)
            {
                int currentIndex = WeaponSystem.CurrentWeaponIndex;
                int newIndex = currentIndex;

                if (input.SwitchWeapon > 1)
                {
                    // Number keys
                    newIndex = input.SwitchWeapon - 1;
                }
                else
                {
                    // Scroll wheel
                    newIndex = (currentIndex + input.SwitchWeapon + WeaponSystem.AvailableWeapons.Length) % WeaponSystem.AvailableWeapons.Length;
                }

                int weaponID = WeaponSystem.AvailableWeapons[newIndex].WeaponID;

                // ✅ Only allow switching if player owns the weapon
                if (_ownedWeaponIDs.Contains(weaponID))
                {
                    if (WeaponSystem.HasStateAuthority)
                        WeaponSystem.SetWeaponIndex(newIndex);
                }
            }


            // Handle reload
            if (input.Reload && WeaponSystem != null)
			{
				WeaponSystem.StartReload();
			}

			// Handle firing
			if (input.Fire)
			{
				if (WeaponSystem != null)
				{
					// Use new weapon system
					WeaponSystem.TryFire(CameraHandle.position, CameraHandle.forward);
				}
				else
				{
					// Fallback to legacy fire system
					Fire();
				}
			}
		}

		private void Fire()
		{
			// Clear hit position in case nothing will be hit
			_hitPosition = Vector3.zero;

			// Whole projectile path and effects are immediately processed (= hitscan projectile)
			if (Physics.Raycast(CameraHandle.position, CameraHandle.forward, out var hitInfo, 200f, HitMask))
			{
				// Deal damage
				var health = hitInfo.collider != null ? hitInfo.collider.GetComponentInParent<Health>() : null;
				if (health != null)
				{
					health.Killed = OnEnemyKilled;
                    
                    // FORCE ONE-SHOT KILL in Shooter (Scene 3) and always in TieBreaker
                    string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (currentScene.Contains("Shooter") || currentScene.Contains("03") || (_gameManager != null && _gameManager.IsTieBreaker))
                    {
                        health.TakeHit(1000, true); // Massive damage for 1-shot kill
                    }
                    else
                    {
					    health.TakeHit(1, true);
                    }
				}


				// Save hit point to correctly show bullet path on all clients.
				// This however works only for single projectile per FUN and with higher fire cadence
				// some projectiles might not be fired on proxies because we save only the position
				// of the LAST hit.
				_hitPosition = hitInfo.point;
				_hitNormal = hitInfo.normal;
			}

			// In this example projectile count property (fire count) is used not only for weapon fire effects
			// but to spawn the projectile visuals themselves.
			_fireCount++;
		}

		private void Respawn(Vector3 position, Quaternion rotation)
		{
			Health.Revive();

			if(KCC != null) KCC.SetPosition(position);
            else transform.position = position; // Fallback for Car
            
            // FIX: Apply spawn rotation properly (Yaw only, Pitch = 0)
            float spawnYaw = rotation.eulerAngles.y;
            Vector2 lookRotation = new Vector2(0f, spawnYaw);
            
			if(KCC != null) KCC.SetLookRotation(lookRotation);
            else transform.rotation = rotation; // Fallback for Car
            
            // Also update PlayerInput so the input system starts from the correct angle
            if (HasStateAuthority && PlayerInput != null)
            {
                PlayerInput.SetInitialLookRotation(lookRotation);
            }

			_moveVelocity = Vector3.zero;
		}

        public void OnEnemyKilled(Health enemyHealth)
        {
            if (!HasStateAuthority)
                return;

            if (enemyHealth.GetComponent<Player>() != null)
                Kills++;
        }


        private void ShowFireEffects()
		{
			// If using weapon system, effects are handled there
			if (WeaponSystem != null)
			{
				// Trigger shoot animation when weapon fires
				if (WeaponSystem.FireCount > _visibleFireCount)
				{
					Animator.SetTrigger(_animIDShoot);
					_visibleFireCount = WeaponSystem.FireCount;
				}
				return;
			}

			// Legacy fire effects (fallback)
			// Notice we are not using OnChangedRender for fireCount property but instead
			// we are checking against a local variable and show fire effects only when visible
			// fire count is SMALLER. This prevents triggering false fire effects when
			// local player mispredicted fire (e.g. input got lost) and fireCount property got decreased.
			if (_visibleFireCount < _fireCount)
			{
				if (FireSound != null && FireSound.clip != null)
					FireSound.PlayOneShot(FireSound.clip);
				if (MuzzleParticle != null)
					MuzzleParticle.Play();
				Animator.SetTrigger(_animIDShoot);

				if (_hitPosition != Vector3.zero && ImpactPrefab != null)
				{
					// Impact gets destroyed automatically with DestroyAfter script
					Instantiate(ImpactPrefab, _hitPosition, Quaternion.LookRotation(_hitNormal));
				}
			}

			_visibleFireCount = _fireCount;
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeedX = Animator.StringToHash("SpeedX");
			_animIDSpeedZ = Animator.StringToHash("SpeedZ");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDPitch = Animator.StringToHash("Pitch");
			_animIDShoot = Animator.StringToHash("Shoot");
		}

		private void OnJumpingChanged()
		{
			if (_isJumping)
			{
				AudioSource.PlayClipAtPoint(JumpAudioClip, KCC.Position, 0.5f);
			}
			else
			{
				AudioSource.PlayClipAtPoint(LandAudioClip, KCC.Position, 1f);
			}

			if (HasStateAuthority == false)
			{
				ScalingRoot.localScale = _isJumping ? new Vector3(0.5f, 1.5f, 0.5f) : new Vector3(1.25f, 0.75f, 1.25f);
			}
		}

		private void OnNicknameChanged()
		{
			if (HasStateAuthority)
				return; // Do not show nickname for local player

			Nameplate.SetNickname(Nickname);
		}
	}
}
