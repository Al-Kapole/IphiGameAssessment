using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Singleton <see cref="MonoBehaviour"/> that listens to Unity's Input System event stream
/// and fires <see cref="OnDeviceChanged"/> whenever the player switches between
/// keyboard/mouse and a gamepad (with PS4 / Xbox differentiation).
/// </summary>

// DefaultExecutionOrder  is set to -100 to guarantee
// "Instance" is assigned before any other script's Start or OnEnable.
[DefaultExecutionOrder(-100)]
public class InputDeviceDetector : MonoBehaviour
{
    [Tooltip("When enabled, the detector automatically classifies real hardware input events. " +
             "Disable to rely solely on ForceDevice calls (e.g. for UI-driven debug helpers).")]
    [SerializeField] private bool autoDetect = true;

    public static InputDeviceDetector Instance { get; private set; }

    /// <summary>Fired on the main thread whenever the active input device type changes.</summary>
    public event Action<InputDeviceType> OnDeviceChanged;

    /// <summary>The input device type currently considered active. Defaults to <see cref="InputDeviceType.KeyboardMouse"/>.</summary>
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;

    private void Awake()
    {
        // Enforces the singleton contract. Destroys this object if another instance already exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (autoDetect)
            InputSystem.onEvent += HandleInputEvent;
    }

    private void OnDisable()
    {
        if (autoDetect)
            InputSystem.onEvent -= HandleInputEvent;
    }

    /// <summary>
    /// Forces a specific device type, bypassing real input detection.
    /// Fires <see cref="OnDeviceChanged"/> only when the device actually changes.
    /// Intended for use by debug helpers such as <see cref="DevicesDebugHelper"/>.
    /// </summary>
    /// <param name="deviceType">The device type to set as the active device.</param>
    public void ForceDevice(InputDeviceType deviceType)
    {
        if (deviceType == CurrentDevice)
            return;

        CurrentDevice = deviceType;
        OnDeviceChanged?.Invoke(CurrentDevice);
    }

    /// <summary>
    /// Receives every raw Input System event. Ignores non-state events (e.g. device-added)
    /// and forwards state-carrying events to <see cref="ClassifyDevice"/>.
    /// </summary>
    /// <param name="eventPtr">Pointer to the raw input event.</param>
    /// <param name="device">The device that produced the event.</param>
    private void HandleInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        // Only process events that carry actual control state (ignore device-added, etc.)
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;
        ForceDevice(ClassifyDevice(device));
    }

    /// <summary>
    /// Maps a raw <see cref="InputDevice"/> to one of the three <see cref="InputDeviceType"/> values.
    /// Gamepads are further differentiated by <see cref="IsPlayStationGamepad"/>.
    /// Unrecognised devices default to <see cref="InputDeviceType.KeyboardMouse"/>.
    /// </summary>
    /// <param name="device">The device to classify.</param>
    /// <returns>The <see cref="InputDeviceType"/> that best represents the device.</returns>
    private InputDeviceType ClassifyDevice(InputDevice device)
    {
        if (device is Gamepad gamepad)
            return IsPlayStationGamepad(gamepad) ? InputDeviceType.Gamepad_PS4 : InputDeviceType.Gamepad_Xbox;

        if (device is Keyboard || device is Mouse)
            return InputDeviceType.KeyboardMouse;

        return InputDeviceType.KeyboardMouse;
    }

    /// <summary>
    /// Determines whether a gamepad is a PlayStation controller by checking its
    /// product name and device name for known PlayStation identifiers.
    /// </summary>
    /// <param name="gamepad">The gamepad to inspect.</param>
    /// <returns><c>true</c> if the gamepad is identified as a PlayStation controller; otherwise <c>false</c>.</returns>
    private bool IsPlayStationGamepad(Gamepad gamepad)
    {
        string product = gamepad.description.product;
        if (string.IsNullOrEmpty(product))
            product = gamepad.name;

        return ContainsIgnoreCase(product, "DualShock")
            || ContainsIgnoreCase(product, "DualSense")
            || ContainsIgnoreCase(product, "PS4")
            || ContainsIgnoreCase(product, "PS5");
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="source"/> contains <paramref name="value"/>
    /// using a case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="source">The string to search within.</param>
    /// <param name="value">The substring to look for.</param>
    /// <returns><c>true</c> if the substring is found; otherwise <c>false</c>.</returns>
    private bool ContainsIgnoreCase(string source, string value)
    {
        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
