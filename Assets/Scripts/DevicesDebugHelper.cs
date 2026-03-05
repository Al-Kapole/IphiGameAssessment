using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only debug helper that lets you force the active input device
/// by selecting a UI toggle, bypassing real hardware detection.
/// <para>
/// Also listens to <see cref="InputDeviceDetector.OnDeviceChanged"/> so the
/// toggles stay in sync when the device is switched programmatically or by
/// actual hardware input.
/// </para>
/// </summary>
public class DevicesDebugHelper : MonoBehaviour
{
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private Toggle _keyboardToggle;
    [SerializeField] private Toggle _xboxToggle;
    [SerializeField] private Toggle _psToggle;

    // Subscribes all toggle listeners and registers for device-change events
    // so the UI stays in sync with <see cref="InputDeviceDetector"/>.
    private void OnEnable()
    {
        _keyboardToggle.onValueChanged.AddListener(OnAnyToggleChanged);
        _xboxToggle.onValueChanged.AddListener(OnAnyToggleChanged);
        _psToggle.onValueChanged.AddListener(OnAnyToggleChanged);

        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged += ChangeToggle;
    }

    // Unsubscribes all listeners to prevent stale callbacks after this object is disabled.
    private void OnDisable()
    {
        _keyboardToggle.onValueChanged.RemoveListener(OnAnyToggleChanged);
        _xboxToggle.onValueChanged.RemoveListener(OnAnyToggleChanged);
        _psToggle.onValueChanged.RemoveListener(OnAnyToggleChanged);

        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= ChangeToggle;
    }

    /// <summary>
    /// Shared handler for all three toggles. Ignores the <c>false</c> callback
    /// that fires when a toggle is deselected, then reads the currently active
    /// toggle from the group and calls <see cref="InputDeviceDetector.ForceDevice"/>.
    /// </summary>
    /// <param name="isOn"><c>true</c> when a toggle becomes selected; <c>false</c> when deselected (ignored).</param>
    private void OnAnyToggleChanged(bool isOn)
    {
        if (!isOn) return;

        Toggle active = null;
        foreach (Toggle toggle in _toggleGroup.ActiveToggles())
        {
            active = toggle;
            break;
        }

        InputDeviceType device;
        if (active == _keyboardToggle)
            device = InputDeviceType.KeyboardMouse;
        else if (active == _xboxToggle)
            device = InputDeviceType.Gamepad_Xbox;
        else
            device = InputDeviceType.Gamepad_PS4;

        InputDeviceDetector.Instance.ForceDevice(device);
    }

    /// <summary>
    /// Keeps the toggle UI in sync when the device changes from outside this helper
    /// (e.g. real hardware input or another script calling <see cref="InputDeviceDetector.ForceDevice"/>).
    /// Uses <c>SetIsOnWithoutNotify</c> to avoid triggering <see cref="OnAnyToggleChanged"/> again.
    /// </summary>
    /// <param name="device">The newly active input device type.</param>
    private void ChangeToggle(InputDeviceType device)
    {
        if (device == InputDeviceType.KeyboardMouse)
            _keyboardToggle.SetIsOnWithoutNotify(true);
        else if (device == InputDeviceType.Gamepad_Xbox)
            _xboxToggle.SetIsOnWithoutNotify(true);
        else
            _psToggle.SetIsOnWithoutNotify(true);
    }
}
