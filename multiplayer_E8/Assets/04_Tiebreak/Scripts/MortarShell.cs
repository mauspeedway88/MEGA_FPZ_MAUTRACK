using Fusion;
using UnityEngine;
using System.Collections;

public class MortarShell : NetworkBehaviour
{
    [Header("Movement")]
    public float Speed = 80f;
    public float Gravity = 0f;
    public float MaxDistance = 300f;
    public float Lifetime = 5f;

    [Header("Collision")]
    public float Radius = 0.2f;
    public LayerMask HitMask;

    [Header("Visuals")]
    public GameObject ImpactEffect;

    [Networked] private Vector3 Direction { get; set; }
    [Networked] private Vector3 StartPosition { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private PlayerRef Owner { get; set; }
    [Networked] private NetworkBool HasCollided { get; set; }
    [Networked] private NetworkBool IsInitialized { get; set; }
    [Networked] private Vector3 Velocity { get; set; }

    private Rigidbody _rb;
    private BullseyeTarget _target;

    // =======================
    // INITIALIZATION (SERVER ONLY)
    // =======================
    public void Initialize(Vector3 direction, PlayerRef owner)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[MortarShell] Only server should initialize shell");
            return;
        }

        Debug.Log($"[MortarShell] Server initializing shell for Player {owner.PlayerId}");

        Direction = direction.normalized;
        Owner = owner;
        StartPosition = transform.position;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, Lifetime);
        HasCollided = false;
        IsInitialized = true;

        // Calculate initial velocity
        Velocity = Direction * Speed;

        _target = FindObjectOfType<BullseyeTarget>();

        if (_target == null)
            Debug.LogError("[MortarShell] BullseyeTarget not found!");
            
        // Ensure RB state on server
         if (_rb) _rb.useGravity = false;
    }

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb)
        {
            _rb.useGravity = false; // Disable Unity gravity to rely on custom gravity
            _rb.isKinematic = false; // We move it via velocity
        }

        if (_target == null)
            _target = FindObjectOfType<BullseyeTarget>();
    }

    // =======================
    // SERVER SIMULATION
    // =======================
    public override void FixedUpdateNetwork()
    {
        // Only server simulates the shell
        if (!HasStateAuthority || !IsInitialized || HasCollided)
            return;

        // Lifetime / distance check
        if (LifeTimer.Expired(Runner) ||
            Vector3.Distance(StartPosition, transform.position) > MaxDistance)
        {
            Debug.Log($"[MortarShell] Server: Player {Owner.PlayerId} MISSED");
            HasCollided = true;

            if (_target != null)
            {
                _target.RegisterMissServer(Owner);
            }

            RPC_HideShell();
            Despawn();
            return;
        }

        // Apply gravity to velocity
        Velocity += Vector3.down * Gravity * Runner.DeltaTime;

        // Update position using velocity
        if (_rb != null)
        {
            _rb.linearVelocity = Velocity;
        }

        CheckCollision();
    }

    private void CheckCollision()
    {
        Vector3 pos = transform.position;
        Vector3 nextPos = pos + Velocity * Runner.DeltaTime;
        Vector3 dir = (nextPos - pos).normalized;
        float dist = Vector3.Distance(pos, nextPos);

        // Backup raycast for high speed
        if (Physics.SphereCast(pos, Radius, dir, out RaycastHit hit, dist, HitMask))
        {
            Debug.Log($"[MortarShell] Server: Player {Owner.PlayerId} HIT at {hit.point}");
            HasCollided = true;

            // Check if we hit a player
            var hitPlayer = hit.collider.GetComponentInParent<TieBreakerPlayer>();
            if (hitPlayer != null)
            {
                 // It's a kill!
                 hitPlayer.RPC_Die();
            }

            if (_target != null)
            {
                _target.RegisterHitServer(Owner, hit.point);
            }

            RPC_HideShell();
            Despawn();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideShell()
    {
        // Hide the shell on all clients
        gameObject.SetActive(false);
    }

    private void Despawn()
    {
        if (HasStateAuthority && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}