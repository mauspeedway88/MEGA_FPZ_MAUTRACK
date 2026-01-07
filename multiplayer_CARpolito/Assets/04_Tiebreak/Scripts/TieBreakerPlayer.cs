using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TieBreakerPlayer : NetworkBehaviour
{
    [Header("Setup")]
    public Transform CameraHandle;
    public Transform CameraPivot;
    public LayerMask TargetMask;

    [Header("Visuals")]
    public LineRenderer AimLine; // Optional visual, maybe for laser sight if wanted, or just disabled

    [Header("Movement")]
    public float WalkSpeed = 5f;
    public float JumpForce = 5f;
    public float Gravity = 20f;
    public float LookSensitivityX = 1.5f;
    public float LookSensitivityY = 1.2f;

    [Networked] public NetworkBool IsDead { get; set; }

    private CharacterController _cc;
    private Camera _cam;
    
    private float _accumulatedYaw;
    private float _accumulatedPitch;
    private Vector3 _velocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_cc == null)
        {
            _cc = gameObject.AddComponent<CharacterController>();
            // Optional: Adjust default settings if needed
            _cc.center = new Vector3(0, 1f, 0); // Standard midpoint
            _cc.height = 2f;
            _cc.radius = 0.3f;
        }
    }

    public override void Spawned()
    {
        // _cc is already assigned in Awake, but good to double check or re-fetch if Fusion does something with components
        if(_cc == null) _cc = GetComponent<CharacterController>();
        
        if (HasInputAuthority)
        {
            _cam = Camera.main;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (CameraPivot != null)
            {
               _accumulatedYaw = CameraPivot.localEulerAngles.y;
               _accumulatedPitch = CameraPivot.localEulerAngles.x;
            }
                
            if (_accumulatedPitch > 180) _accumulatedPitch -= 360;
        }

        if (AimLine != null) AimLine.enabled = true; // Enable aiming line
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority || IsDead) return;

        // 1. Movement (WASD + Gravity)
        Vector3 moveDir = Vector3.zero;
        if (_cc != null && _cc.enabled)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Direction relative to camera look
            // Calculate movement direction based on Camera Look (Yaw)
            Quaternion yawRotation = Quaternion.Euler(0f, _accumulatedYaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            
            moveDir = (forward * vertical + right * horizontal).normalized;
            
            // Apply Gravity
            if (_cc.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Stick to ground
            }

            // Jump
            if (Input.GetButton("Jump") && _cc.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(JumpForce * 2f * Gravity);
            }

            _velocity.y -= Gravity * Runner.DeltaTime;

            // Move
            _cc.Move((moveDir * WalkSpeed + _velocity) * Runner.DeltaTime);
        }

        // 2. Look Rotation & Raycast Shoot
        // (Input gathering in Update for smoothness, logic here for safety or straight mapping)
    }

    private void Update()
    {
        if (HasInputAuthority && !IsDead)
        {
            // Mouse Look
            float mouseX = Input.GetAxis("Mouse X") * LookSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * LookSensitivityY;

            _accumulatedYaw += mouseX;
            _accumulatedPitch -= mouseY;
            _accumulatedPitch = Mathf.Clamp(_accumulatedPitch, -89f, 89f);

            // Rotate CameraPivot (Both Yaw and Pitch) - Body stays stationary
            if (CameraPivot != null)
            {
                CameraPivot.localRotation = Quaternion.Euler(_accumulatedPitch, _accumulatedYaw, 0f);
            }

            // Update AimLine Visuals
            if (AimLine != null && CameraHandle != null)
            {
                AimLine.SetPosition(0, CameraHandle.position);
                AimLine.SetPosition(1, CameraHandle.position + CameraHandle.forward * 50f);
            }

            // Shooting
            if (Input.GetButtonDown("Fire1"))
            {
                if (CameraHandle != null)
                {
                    RPC_FireRaycast(CameraHandle.position, CameraHandle.forward);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (HasInputAuthority && _cam != null && CameraHandle != null)
        {
            _cam.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_FireRaycast(Vector3 origin, Vector3 direction)
    {
        // Server side raycast check
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 500f, TargetMask))
        {
            Debug.Log($"[TieBreaker] Hit {hit.collider.name}");
            
            var hitPlayer = hit.collider.GetComponentInParent<TieBreakerPlayer>();
            if (hitPlayer != null && hitPlayer != this)
            {
                hitPlayer.RPC_Die();
            }
        }
        
        // Optional: Trigger visuals RPC here
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Die()
    {
        if (IsDead) return;
        
        IsDead = true;
        Debug.Log($"[TieBreaker] Player {Object.InputAuthority.PlayerId} DIED");
        
        // Disable visuals, controls, etc.
        gameObject.SetActive(false);
        
        // Trigger generic Despawn or Game Over logic
        // Runner.Despawn(Object); // Keeping it active but hidden might be safer for "Round End" logic
    }
}