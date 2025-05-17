using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public class Controls : MonoBehaviour
{
	Gamepad Gamepad1;
	public GamepadButton P1JumpKey;
	public ButtonControl P1JumpControl;
	public Vector2 P1MovementInput;
	public Vector2 P1AimInput;
	public float P1AimDeadzone = 0.7f;

	Gamepad Gamepad2;
	public GamepadButton P2JumpKey;
	public ButtonControl P2JumpControl;
	public Vector2 P2MovementInput;
	public Vector2 P2AimInput;
	public float P2AimDeadzone = 0.7f;

	public enum DirectionSurface { LeftStick, RightStick, Dpad }

	private void InputListener(bool debug = false) {
		if (Gamepad1 != null) {
			if (debug) Debug.Log($"Scanning inputs for {Gamepad1.displayName}.");

			Vector2 dpadInput = Gamepad1.dpad.value;
			Vector2 leftStickInput = Gamepad1.leftStick.value;
			Vector2 rightStickInput = Gamepad1.rightStick.value;

			//Movement
			bool moveXOverride = Mathf.Abs(dpadInput.x) > Mathf.Abs(leftStickInput.x);
			bool moveYOverride = dpadInput.y > leftStickInput.y;
			float moveX = moveXOverride ? dpadInput.x : leftStickInput.x;
			float moveY = moveYOverride ? dpadInput.y : leftStickInput.y;
			if (P1JumpControl.isPressed) moveY = 1; //Button Override
			P1MovementInput = new(moveX, moveY);

			//Aim
			bool undeadzoneX = Mathf.Abs(rightStickInput.x) > P1AimDeadzone;
			bool undeadzoneY = Mathf.Abs(rightStickInput.y) > P1AimDeadzone;
			if (undeadzoneX || undeadzoneY) P1AimInput = rightStickInput.normalized;
		}

		if (Gamepad2 != null) {
			if (debug) Debug.Log($"Scanning inputs for {Gamepad2.displayName}.");

			Vector2 dpadInput = Gamepad2.dpad.value;
			Vector2 leftStickInput = Gamepad2.leftStick.value;
			Vector2 rightStickInput = Gamepad2.rightStick.value;

			//Movement
			bool moveXOverride = Mathf.Abs(dpadInput.x) > Mathf.Abs(leftStickInput.x);
			bool moveYOverride = dpadInput.y > leftStickInput.y;
			float moveX = moveXOverride ? dpadInput.x : leftStickInput.x;
			float moveY = moveYOverride ? dpadInput.y : leftStickInput.y;
			if (P2JumpControl.isPressed) moveY = 1; //Button Override
			P2MovementInput = new(moveX, moveY);

			//Aim
			bool undeadzoneX = Mathf.Abs(rightStickInput.x) > P2AimDeadzone;
			bool undeadzoneY = Mathf.Abs(rightStickInput.y) > P2AimDeadzone;
			if (undeadzoneX || undeadzoneY) P2AimInput = rightStickInput.normalized;
		}
	}

	private void OnEnable() {
		InputSystem.onDeviceChange += OnDeviceChange;

		foreach (Gamepad gamepad in Gamepad.all) {
			AutoAssignGamepad(gamepad);
		}
	}

	private void OnDisable() {
		InputSystem.onDeviceChange -= OnDeviceChange;
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange change) {
		if (device is Gamepad gamepad) {
			if (change == InputDeviceChange.Added) {
				AutoAssignGamepad(gamepad);
			}
		}
	}

	private void AutoAssignGamepad(Gamepad gamepad, bool debug = false) {
		if (Gamepad1 == null) {
			Gamepad1 = gamepad;
			P1JumpControl = MapButton(gamepad, P1JumpKey);
			if (debug) Debug.Log($"[Controls] {gamepad.name} added and connected as Gamepad 1.");
		}

		else if (Gamepad2 == null) {
			Gamepad2 = gamepad;
			P2JumpControl = MapButton(gamepad, P2JumpKey);
			if (debug) Debug.Log($"[Controls] {gamepad.name} added and connected as Gamepad 2.");
		}

		else if (debug) Debug.Log($"[Controls] {gamepad.name} added but not connected. 2 gamepads already connected.");
	}

	private void Update() {
		InputListener();
	}

	public static ButtonControl MapButton(Gamepad gamepad, GamepadButton button) {
		return button switch {
			GamepadButton.South => gamepad.buttonSouth,
			GamepadButton.North => gamepad.buttonNorth,
			GamepadButton.West => gamepad.buttonWest,
			GamepadButton.East => gamepad.buttonEast,
			GamepadButton.LeftShoulder => gamepad.leftShoulder,
			GamepadButton.RightShoulder => gamepad.rightShoulder,
			GamepadButton.Start => gamepad.startButton,
			GamepadButton.Select => gamepad.selectButton,
			GamepadButton.LeftStick => gamepad.leftStickButton,
			GamepadButton.RightStick => gamepad.rightStickButton,
			GamepadButton.DpadUp => gamepad.dpad.up,
			GamepadButton.DpadDown => gamepad.dpad.down,
			GamepadButton.DpadLeft => gamepad.dpad.left,
			GamepadButton.DpadRight => gamepad.dpad.right,
			_ => null
		};
	}
}
