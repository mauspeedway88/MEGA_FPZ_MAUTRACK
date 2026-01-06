using UnityEngine;
using Fusion;

namespace Starter.Shooter
{
	/// <summary>
	/// Networked projectile that travels through space and deals damage on impact.
	/// Used for projectile-based weapons.
	/// </summary>
	public class Projectile : NetworkBehaviour
	{
		[Header("References")]
		public Rigidbody Rigidbody;
		public Collider Collider;
		public GameObject VisualObject;
		public ParticleSystem TrailParticle;
		public GameObject ImpactEffect;

		[Networked]
		private Vector3 _direction { get; set; }
		[Networked]
		private float _speed { get; set; }
		[Networked]
		private float _gravity { get; set; }
		[Networked]
		private int _damage { get; set; }
		[Networked]
		private float _maxDistance { get; set; }
		[Networked]
		private float _lifetime { get; set; }
		[Networked]
		private Vector3 _startPosition { get; set; }
		[Networked]
		private TickTimer _lifetimeTimer { get; set; }
		[Networked]
		private int _hitMaskValue { get; set; }
		[Networked]
		private PlayerRef _ownerPlayerRef { get; set; }

		private LayerMask _hitMask;
		private bool _hasHit;
		private bool _isInitialized;
		private float _spawnTime;
		private const float COLLISION_DELAY = 0.1f; // Delay before collision detection to avoid hitting player

		/// <summary>
		/// Initialize the projectile with parameters from weapon.
		/// Called immediately after spawning - parameters are stored and applied in Spawned().
		/// </summary>
		public void Initialize(Vector3 direction, float speed, float gravity, int damage, 
		                      float maxDistance, float lifetime, LayerMask hitMask, PlayerRef ownerPlayerRef = default)
		{
			if (HasStateAuthority)
			{
				_direction = direction.normalized;
				_speed = speed;
				_gravity = gravity;
				_damage = damage;
				_maxDistance = maxDistance;
				_lifetime = lifetime;
				_startPosition = transform.position;
				_hitMaskValue = hitMask.value;
				_hitMask = hitMask;
				_ownerPlayerRef = ownerPlayerRef;

				_lifetimeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
				_isInitialized = true;

				// Set initial velocity immediately if possible
				ApplyInitialVelocity();
			}
			else
			{
				// Store for later initialization in Spawned()
				_hitMask = hitMask;
			}
		}

		private void ApplyInitialVelocity()
		{
			if (Rigidbody != null && _isInitialized)
			{
				Rigidbody.linearVelocity = _direction * _speed;
			}
		}

		public override void Spawned()
		{
			// Record spawn time to delay collision detection
			_spawnTime = Time.time;

			// Restore hit mask from networked value
			if (_hitMaskValue != 0)
			{
				_hitMask.value = _hitMaskValue;
			}

			// Disable collision with other projectiles to prevent inter-projectile collisions
			if (Collider != null)
			{
				// Ignore collisions with other projectiles
				// Find all projectiles and ignore collisions with them
				var allProjectiles = FindObjectsOfType<Projectile>();
				foreach (var otherProjectile in allProjectiles)
				{
					if (otherProjectile != this && otherProjectile.Collider != null)
					{
						Physics.IgnoreCollision(Collider, otherProjectile.Collider, true);
					}
				}
			}

			// Initialize if we have state authority and parameters are set
			if (HasStateAuthority && _speed > 0 && !_isInitialized)
			{
				_startPosition = transform.position;
				_lifetimeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
				_isInitialized = true;
				ApplyInitialVelocity();
			}
			else if (HasStateAuthority && _isInitialized)
			{
				// Already initialized, just apply velocity
				ApplyInitialVelocity();
			}

			// Enable trail particle
			if (TrailParticle != null)
			{
				var emission = TrailParticle.emission;
				emission.enabled = true;
			}

			Debug.Log($"[Projectile] Spawned at {transform.position}, HasStateAuthority: {HasStateAuthority}, Speed: {_speed}");
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority == false)
				return;

			if (_hasHit)
				return;

			// Delay collision detection to avoid hitting player immediately
			if (Time.time - _spawnTime < COLLISION_DELAY)
				return;

			// Check lifetime
			if (_lifetimeTimer.Expired(Runner))
			{
				DestroyProjectile();
				return;
			}

			// Check max distance
			float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
			if (distanceTraveled > _maxDistance)
			{
				DestroyProjectile();
				return;
			}

			// Apply gravity
			if (_gravity != 0f && Rigidbody != null)
			{
				Rigidbody.linearVelocity += Vector3.down * _gravity * Runner.DeltaTime;
			}

			// Check for collisions
			CheckCollisions();
		}

		private void CheckCollisions()
		{
			// Use sphere cast to check for collisions
			Vector3 currentPos = transform.position;
			Vector3 nextPos = currentPos + (Rigidbody != null ? Rigidbody.linearVelocity : _direction * _speed) * Runner.DeltaTime;
			Vector3 direction = (nextPos - currentPos).normalized;
			float distance = Vector3.Distance(nextPos, currentPos);

			// Use original hit mask - we'll filter players by component check instead
			// This ensures chickens (which may be on same layer as players) are still detected
			LayerMask hitMask = _hitMask;

			// SphereCast with trigger interaction enabled to hit both trigger and non-trigger colliders
			// This ensures we can hit chickens which have both types of colliders
			if (Physics.SphereCast(currentPos, 0.2f, direction, out var hit, distance, hitMask, QueryTriggerInteraction.Collide))
			{
				// Hit ANY valid health target (Player or Chicken)
                var health = hit.collider.GetComponentInParent<Health>();
				if (health != null)
				{
					Debug.Log($"[Projectile] Hit target: {hit.collider.name}");
					OnHit(hit);
					return;
				}
				else
				{
					Debug.Log($"[Projectile] Hit non-health object: {hit.collider.name}");
                    OnHit(hit); // Still hit the wall/ground
                    return;
				}
			}


			// Additional check: Use OverlapSphere at current position as backup
			// This catches cases where the projectile is already inside a collider
			Collider[] overlapping = Physics.OverlapSphere(currentPos, 0.3f, hitMask, QueryTriggerInteraction.Collide);
			if (overlapping.Length > 0)
			{
				// Find the closest collider that is a chicken
				Collider closest = null;
				float closestDistance = float.MaxValue;
				foreach (var col in overlapping)
				{
					// Only process chickens
					if (!IsChicken(col))
						continue;

					float dist = Vector3.Distance(currentPos, col.ClosestPoint(currentPos));
					if (dist < closestDistance)
					{
						closestDistance = dist;
						closest = col;
					}
				}

				if (closest != null)
				{
					// Handle collision directly from collider
					OnColliderHit(closest, currentPos, direction);
				}
			}
		}

		private void OnHit(RaycastHit hit)
		{
			if (_hasHit)
				return;

			_hasHit = true;

			// Deal damage
			var health = hit.collider != null ? hit.collider.GetComponentInParent<Health>() : null;
			if (health != null && health.IsAlive)
			{
				// Set up kill callback for scoring (same as legacy fire system)
				SetupKillCallback(health);
				
				Debug.Log($"[Projectile] Dealing {_damage} damage to {hit.collider.name}, Health: {health.CurrentHealth}");
				health.TakeHit(_damage, true);
			}
			else
			{
				Debug.LogWarning($"[Projectile] Hit {hit.collider.name} but no valid Health component found or entity is dead");
			}

			// Show impact effect
			if (ImpactEffect != null)
			{
				Instantiate(ImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
			}

			// Destroy projectile
			DestroyProjectile();
		}

		/// <summary>
		/// Handle collision when we have a collider directly (from OverlapSphere or OnTriggerEnter).
		/// </summary>
		private void OnColliderHit(Collider collider, Vector3 projectilePosition, Vector3 direction)
		{
			if (_hasHit || collider == null)
				return;

			_hasHit = true;

			// Calculate hit point and normal
			Vector3 hitPoint = collider.ClosestPoint(projectilePosition);
			Vector3 hitNormal = (projectilePosition - hitPoint).normalized;
			if (hitNormal == Vector3.zero)
				hitNormal = -direction;

			// Deal damage
			var health = collider.GetComponentInParent<Health>();
			if (health != null && health.IsAlive)
			{
				// Set up kill callback for scoring (same as legacy fire system)
				SetupKillCallback(health);
				
				health.TakeHit(_damage, true);
			}

			// Show impact effect
			if (ImpactEffect != null)
			{
				Instantiate(ImpactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
			}

			// Destroy projectile
			DestroyProjectile();
		}

		private void DestroyProjectile()
		{
			if (HasStateAuthority)
			{
				Runner.Despawn(Object);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			// Cleanup
			if (TrailParticle != null)
			{
				var emission = TrailParticle.emission;
				emission.enabled = false;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			// Backup collision detection (works if projectile collider is set as trigger)
			if (HasStateAuthority == false || _hasHit)
				return;

			// Only process chickens
			if (!IsChicken(other))
			{
				Debug.Log($"[Projectile] Trigger hit non-chicken, ignoring: {other.name}");
				return;
			}

			// Check if collider is in hit mask
			if (((1 << other.gameObject.layer) & _hitMask.value) == 0)
				return;

			Debug.Log($"[Projectile] Trigger hit chicken: {other.name}");

			// Try to get a proper raycast hit first
			RaycastHit hit;
			Vector3 direction = (other.ClosestPoint(transform.position) - transform.position).normalized;
			if (direction == Vector3.zero)
				direction = transform.forward;

			if (Physics.Raycast(transform.position, direction, out hit, 2f, _hitMask, QueryTriggerInteraction.Collide))
			{
				// Double-check it's a chicken
				if (IsChicken(hit.collider))
				{
					OnHit(hit);
				}
			}
			else
			{
				// Fallback to direct collider hit (already checked it's a chicken above)
				OnColliderHit(other, transform.position, transform.forward);
			}
		}

		/// <summary>
		/// Check if a collider belongs to a chicken.
		/// </summary>
		private bool IsChicken(Collider collider)
		{
			if (collider == null)
				return false;

			// Check if collider has Chicken component
			var chicken = collider.GetComponentInParent<Chicken>();
			return chicken != null;
		}

		/// <summary>
		/// Set up the kill callback on health component for scoring.
		/// Finds the player who fired this projectile and sets up the callback.
		/// </summary>
		private void SetupKillCallback(Health health)
		{
			// Find the player who owns this projectile
			Player ownerPlayer = null;
			
			// Try to get player from owner reference
			if (_ownerPlayerRef != PlayerRef.None)
			{
				var playerObject = Runner.GetPlayerObject(_ownerPlayerRef);
				if (playerObject != null)
				{
					ownerPlayer = playerObject.GetComponent<Player>();
				}
			}
			
			// Fallback: find player by checking who has WeaponSystem that could have spawned this
			if (ownerPlayer == null)
			{
				var allPlayers = FindObjectsOfType<Player>();
				foreach (var player in allPlayers)
				{
					if (player.HasStateAuthority && player.WeaponSystem != null)
					{
						ownerPlayer = player;
						break;
					}
				}
			}
			
			// Set up the kill callback (same as legacy fire system)
			if (ownerPlayer != null)
			{
				health.Killed = ownerPlayer.OnEnemyKilled;
				Debug.Log($"[Projectile] Set up kill callback for player: {ownerPlayer.Nickname}");
			}
			else
			{
				Debug.LogWarning("[Projectile] Could not find player owner for kill callback setup");
			}
		}

		/// <summary>
		/// Check if a collider belongs to a player.
		/// </summary>
		private bool IsPlayer(Collider collider)
		{
			if (collider == null)
				return false;

			// Check if collider has Player component
			var player = collider.GetComponentInParent<Player>();
			return player != null;
		}
	}
}

