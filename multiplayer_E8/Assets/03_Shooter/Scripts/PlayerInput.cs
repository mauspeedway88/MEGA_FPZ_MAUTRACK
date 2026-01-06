using UnityEngine;

namespace Starter.Shooter
{
	/// <summary>
	/// Structure holding player input.
	/// </summary>
	public struct GameplayInput
	{
		public Vector2 LookRotation;
		public Vector2 MoveDirection;
		public bool Jump;
		public bool Fire;
		public bool Reload;
		public int SwitchWeapon; // -1 = previous, 0 = none, 1 = next
	}

	/// <summary>
	/// PlayerInput handles accumulating player input from Unity.
	/// Supports both desktop (keyboard/mouse) and mobile (touch) controls.
	/// </summary>
	public sealed class PlayerInput : MonoBehaviour
	{
		[Header("Mobile Touch Settings")]
		[SerializeField] private float lookSensitivity = 2f;
		[SerializeField] private float moveSensitivity = 1f;

		public GameplayInput CurrentInput => _input;
		private GameplayInput _input;

		// Mobile touch tracking
		private int _moveTouchId = -1;
		private int _lookTouchId = -1;
		private Vector2 _moveTouchStartPosition; // Starting position for move joystick
		private Vector2 _lastLookTouchPosition;
		private bool _isMobilePlatform;
		private const float MAX_JOYSTICK_DISTANCE = 100f; // Max distance for joystick movement

		private void Awake()
		{
			// Detect mobile platform
			_isMobilePlatform = Application.isMobilePlatform || 
			                   #if UNITY_ANDROID || UNITY_IOS
			                   true;
			                   #else
			                   false;
			                   #endif
		}

		public void ResetInput()
		{
			// Reset input after it was used to detect changes correctly again
			_input.MoveDirection = default;
			_input.Jump = false;
			_input.Fire = false;
			_input.Reload = false;
			_input.SwitchWeapon = 0;
		}

		/// <summary>
		/// Sets the initial look rotation (called on spawn to match spawn point orientation).
		/// This is crucial so the player looks in the correct direction when spawning.
		/// </summary>
		/// <param name="rotation">Initial rotation as Vector2 (Pitch, Yaw)</param>
		public void SetInitialLookRotation(Vector2 rotation)
		{
			_input.LookRotation = rotation;
		}

		private void Update()
		{
			if (_isMobilePlatform)
			{
				ProcessMobileInput();
			}
			else
			{
				ProcessDesktopInput();
			}
		}

		/// <summary>
		/// Process desktop input (keyboard/mouse)
		/// </summary>
		private void ProcessDesktopInput()
		{
			// Accumulate input only if the cursor is locked.
			if (Cursor.lockState != CursorLockMode.Locked)
				return;

			// Accumulate input from Keyboard/Mouse. Input accumulation is mandatory (at least for look rotation here) as Update can be
			// called multiple times before next FixedUpdateNetwork is called - common if rendering speed is faster than Fusion simulation.

			_input.LookRotation += new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"));

			var moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
			_input.MoveDirection = moveDirection.normalized;

			_input.Fire |= Input.GetButtonDown("Fire1");
			_input.Jump |= Input.GetButtonDown("Jump");
			_input.Reload |= Input.GetKeyDown(KeyCode.R);

			// Weapon switching with mouse wheel or number keys
			if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
				_input.SwitchWeapon = 1;
			else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
				_input.SwitchWeapon = -1;
			
			// Number keys 1-9 for direct weapon selection
			for (int i = 1; i <= 9; i++)
			{
				if (Input.GetKeyDown(KeyCode.Alpha0 + i))
				{
					_input.SwitchWeapon = i - 1; // Will be handled differently in Player.cs
				}
			}
		}

		/// <summary>
		/// Process mobile touch input
		/// Left half: Move | Right half: Rotate/Look | Bottom 20%: Shoot
		/// </summary>
		private void ProcessMobileInput()
		{
			float screenWidth = Screen.width;
			float screenHeight = Screen.height;
			float screenCenterX = screenWidth * 0.5f;
			float bottomZoneHeight = screenHeight * 0.2f; // Bottom 20% for shoot button

			// Reset fire for this frame (will be set if touch in bottom zone)
			_input.Fire = false;

			// Process all touches
			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch touch = Input.GetTouch(i);
				Vector2 touchPos = touch.position;
				bool isInBottomZone = touchPos.y < bottomZoneHeight;

				// Bottom 20% zone: Shoot button
				if (isInBottomZone)
				{
					if (touch.phase == TouchPhase.Began)
					{
						_input.Fire = true;
					}
					continue; // Don't process move/look for touches in shoot zone
				}

				// Left half: Move (Virtual Joystick)
				if (touchPos.x < screenCenterX)
				{
					if (touch.phase == TouchPhase.Began)
					{
						_moveTouchId = touch.fingerId;
						_moveTouchStartPosition = touchPos; // Store initial touch position as joystick center
					}
					else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && _moveTouchId == touch.fingerId)
					{
						// Calculate move direction from joystick center to current position
						Vector2 delta = touchPos - _moveTouchStartPosition;
						float distance = delta.magnitude;
						
						// Normalize and clamp to max joystick distance
						if (distance > MAX_JOYSTICK_DISTANCE)
						{
							delta = delta.normalized * MAX_JOYSTICK_DISTANCE;
						}
						
						// Convert to move direction (normalized)
						_input.MoveDirection = (delta / MAX_JOYSTICK_DISTANCE).normalized;
					}
					else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
					{
						if (_moveTouchId == touch.fingerId)
						{
							_moveTouchId = -1;
							_input.MoveDirection = Vector2.zero;
						}
					}
				}
				// Right half: Rotate/Look
				else
				{
					if (touch.phase == TouchPhase.Began)
					{
						_lookTouchId = touch.fingerId;
						_lastLookTouchPosition = touchPos;
					}
					else if (touch.phase == TouchPhase.Moved && _lookTouchId == touch.fingerId)
					{
						// Calculate look rotation delta
						Vector2 delta = touch.deltaPosition * lookSensitivity;
						_input.LookRotation += new Vector2(-delta.y, delta.x);
					}
					else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
					{
						if (_lookTouchId == touch.fingerId)
						{
							_lookTouchId = -1;
						}
					}
				}
			}

			// Reset move direction if touch ended
			if (_moveTouchId != -1)
			{
				bool touchStillActive = false;
				for (int i = 0; i < Input.touchCount; i++)
				{
					if (Input.GetTouch(i).fingerId == _moveTouchId)
					{
						touchStillActive = true;
						break;
					}
				}
				if (!touchStillActive)
				{
					_moveTouchId = -1;
					_input.MoveDirection = Vector2.zero;
				}
			}

			// Handle case when no touches are active
			if (Input.touchCount == 0)
			{
				_moveTouchId = -1;
				_lookTouchId = -1;
				_input.MoveDirection = Vector2.zero;
			}
		}
	}
}
