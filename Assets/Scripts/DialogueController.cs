using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates a full dialogue sequence: loading entries from <see cref="DialogueData"/>,
/// driving <see cref="DialogueViewer"/> for visual and audio presentation, handling
/// Next / Skip / Replay button interactions, and swapping input-device glyphs on the fly.
/// </summary>
public class DialogueController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DialogueData _dialogueData;
    [SerializeField] private GlyphConfig _glyphConfig;


    [Header("Glyph Tokens for Buttons")]
    [Tooltip("Token name (without brackets) used for the Next/Continue button glyph.")]
    [SerializeField] private string _nextToken = "next";
    [Tooltip("Token name (without brackets) used for the Skip button glyph.")]
    [SerializeField] private string _skipToken = "skip";

    [Header("UI References")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private GameObject _dialogueEndedPanel;
    [SerializeField] private DialogueViewer _dialogueViewer;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _replayButton;

    private TMP_Text _nextButtonText;
    private TMP_Text _skipButtonText;

    private int _currentEntryIndex;
    private InputDeviceType _currentDevice;

    // Initialises button references, wires up listeners, detects the current input device,
    // and starts the dialogue from the first entry.
    private void Start()
    {
        _nextButtonText = _nextButton.GetComponentInChildren<TMP_Text>();
        _skipButtonText = _skipButton.GetComponentInChildren<TMP_Text>();
        _dialogueViewer.Initialize(UpdateButtonText);

        _nextButton.onClick.AddListener(OnNextButtonClicked);
        _skipButton.onClick.AddListener(OnSkipButtonClicked);
        _replayButton.onClick.AddListener(Repplay);

        if (InputDeviceDetector.Instance != null)
        {
            _currentDevice = InputDeviceDetector.Instance.CurrentDevice;
            InputDeviceDetector.Instance.OnDeviceChanged += OnDeviceChanged;
        }

        _currentEntryIndex = 0;
        ShowEntry(_currentEntryIndex);
    }

    /// <summary>
    /// Restarts the dialogue from the first entry and shows the dialogue panel again.
    /// </summary>
    private void Repplay()
    {
        ShowDialoguePanel(true);
        _currentEntryIndex = 0;
        ShowEntry(_currentEntryIndex);
    }

    /// <summary>
    /// Removes all button listeners and unsubscribes from device-change events
    /// to prevent stale callbacks after this object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _nextButton.onClick.RemoveListener(OnNextButtonClicked);
        _skipButton.onClick.RemoveListener(OnSkipButtonClicked);

        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= OnDeviceChanged;
    }

    /// <summary>
    /// Loads and displays the dialogue entry at <paramref name="index"/>.
    /// Hides the dialogue panel and ends the sequence if the index is out of range.
    /// </summary>
    /// <param name="index">Zero-based index into <see cref="DialogueData.entries"/>.</param>
    private void ShowEntry(int index)
    {
        if (_dialogueData == null || index >= _dialogueData.entries.Count)
        {
            ShowDialoguePanel(false);
            return;
        }

        DialogueEntry entry = _dialogueData.entries[index];
        SpeakerData speaker = _dialogueData.GetSpeaker(entry.speakerIndex);
        string processedText = _glyphConfig.ProcessText(entry.text, _currentDevice);
        _dialogueViewer.UpdateDialogue(processedText, speaker);
        UpdateButtonText();
    }

    /// <summary>
    /// Toggles between the dialogue panel and the dialogue-ended panel.
    /// </summary>
    /// <param name="value"><c>true</c> to show the dialogue panel; <c>false</c> to show the ended panel.</param>
    private void ShowDialoguePanel(bool value)
    {
        _dialoguePanel.SetActive(value);
        _dialogueEndedPanel.SetActive(!value);
    }

    /// <summary>
    /// Instantly reveals all remaining characters on the current line and refreshes the button label.
    /// </summary>
    private void CompleteCurrentLine()
    {
        _dialogueViewer.CompleteCurrentLine();
        UpdateButtonText();
    }

    /// <summary>
    /// Handles the Next button.
    /// <para>
    /// While the typewriter is running, completes the current line instantly.
    /// Otherwise advances to the next entry, or closes the dialogue if this was the last one.
    /// </para>
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (_dialogueViewer.IsTyping)
        {
            // First press while typing: reveal all text instantly.
            CompleteCurrentLine();
            return;
        }

        // Advance to next entry, or close if this was the last.
        _currentEntryIndex++;
        if (_currentEntryIndex < _dialogueData.entries.Count)
            ShowEntry(_currentEntryIndex);
        else
            ShowDialoguePanel(false);
    }

    /// <summary>
    /// Handles the Skip button. Aborts the typewriter and closes the dialogue immediately.
    /// </summary>
    private void OnSkipButtonClicked()
    {
        _dialogueViewer.SkipDialogue();
        ShowDialoguePanel(false);
    }

    /// <summary>
    /// Called when the player switches input device. Stops the typewriter, re-processes
    /// sprite tags for the new device, and resumes the typewriter from where it was interrupted.
    /// </summary>
    /// <param name="newDevice">The newly active input device type.</param>
    private void OnDeviceChanged(InputDeviceType newDevice)
    {
        _currentDevice = newDevice;

        bool wasTyping = _dialogueViewer.IsTyping;

        // Stop the running coroutine to safely rewrite the text.
        _dialogueViewer.StopTypewriter();

        // Re-process text with the new device's sprite tags, preserving progress.
        RefreshGlyphsInCurrentEntry();
        UpdateButtonText();

        // Resume the typewriter from where it was interrupted.
        if (wasTyping)
            _dialogueViewer.ResumeTypewriter();
    }

    /// <summary>
    /// Re-processes the current entry's text with the active device's sprite tags
    /// and passes it to <see cref="DialogueViewer.RefreshGlyphs"/> without resetting
    /// the typewriter position.
    /// </summary>
    private void RefreshGlyphsInCurrentEntry()
    {
        if (_dialogueData == null || _currentEntryIndex >= _dialogueData.entries.Count)
            return;

        DialogueEntry entry = _dialogueData.entries[_currentEntryIndex];
        string processedText = _glyphConfig.ProcessText(entry.text, _currentDevice);
        _dialogueViewer.RefreshGlyphs(processedText);
    }

    /// <summary>
    /// Rebuilds the Next and Skip button labels with the correct input-device glyphs.
    /// <para>
    /// The Next label cycles through <c>Complete</c> (while typing), <c>Close</c> (last entry),
    /// and <c>Continue</c> (any other entry).
    /// </para>
    /// </summary>
    private void UpdateButtonText()
    {
        if (_dialogueData == null) return;

        string nextGlyph = _glyphConfig.BuildSpriteTag(_nextToken, _currentDevice);
        string skipGlyph = _glyphConfig.BuildSpriteTag(_skipToken, _currentDevice);

        string nextLabel;
        if (_dialogueViewer.IsTyping)
            nextLabel = "Complete";
        else if (_currentEntryIndex >= _dialogueData.entries.Count - 1)
            nextLabel = "Close";
        else
            nextLabel = "Continue";

        _nextButtonText.text = nextGlyph + " " + nextLabel;
        _skipButtonText.text = skipGlyph + " Skip";
    }
}
