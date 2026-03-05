using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all visual and audio presentation of a single dialogue line:
/// typewriter reveal, speaker info (name, portrait, side, color), and typing sounds.
/// Controlled externally by <see cref="DialogueController"/>.
/// </summary>
public class DialogueViewer : MonoBehaviour
{
    public TMP_Text DialogueText { get { return _dialogueText; } }
    public bool IsTyping { get { return _isTyping; } }

    private enum SoundEvery { Letter, Word }

    [Header("Typewriter")]
    [Tooltip("Characters revealed per second.")]
    [SerializeField] private float _charsPerSecond = 30f;
    [Tooltip("Extra pause in seconds after a punctuation character (. , ! ?).")]
    [SerializeField] private float _punctuationPause = 0.12f;

    [Header("UI References")]
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private TMP_Text _speakerNameText;
    [SerializeField] private Image _leftPortraitImage;
    [SerializeField] private Image _rightPortraitImage;
    [SerializeField] private RectTransform _arrow;
    [SerializeField] private Image _bubbleColorImage;

    [Header("Audio")]
    [Tooltip("Whether to play a sound on every letter or only at the start of each word.\n\nNOTE: This value is read once in Initialize() and cached. Changing it at runtime will have no effect.")]
    [SerializeField] private SoundEvery playSoundEvery;
    [Tooltip("Pool of clips to pick from randomly on each trigger. Leave empty to disable typing sounds.")]
    [SerializeField] private AudioClip[] typingSounds;
    [SerializeField] private AudioSource audioSource;

    [Header("Portrait Impulse")]
    [Tooltip("Peak scale multiplier applied to the portrait on each impulse.")]
    [SerializeField] private float _impulseScale = 1.08f;
    [Tooltip("Duration in seconds to scale back down to the original size.")]
    [SerializeField] private float _impulseDownDuration = 0.1f;

    private int _currentVisibleChars;
    private bool _isTyping;
    private Coroutine _typewriterCoroutine;
    private Action _updateBtnTextCallback;
    private Action _typingAudio;

    //Impulsing Portrait
    private Image _activePortrait;
    private CancellationTokenSource _impulseTokenSource;
    private bool _impulsing;
    private float _currentImpulsingTime;
    private Coroutine _impulsingCoroutine;
    //------------------

    /// <summary>
    /// Must be called once before any dialogue is shown.
    /// Caches the button-text update callback and locks in the audio mode.
    /// </summary>
    /// <param name="updateBtnTextCallback">Callback invoked whenever the Next button label needs to refresh.</param>
    public void Initialize(Action updateBtnTextCallback)
    {
        _updateBtnTextCallback = updateBtnTextCallback;
        _typingAudio = playSoundEvery == SoundEvery.Letter ? PlayTypingAudio : PlayTypingAudioOnWord;
    }

    /// <summary>
    /// Applies the speaker visuals and starts the typewriter for a new dialogue line.
    /// </summary>
    /// <param name="processedText">Dialogue text with tokens already replaced by TMP sprite tags.</param>
    /// <param name="speaker">Speaker whose name, portrait, color, and side are applied to the UI.</param>
    public void UpdateDialogue(string processedText, SpeakerData speaker)
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _impulsing = false;
        ApplySpeaker(speaker);

        _dialogueText.text = processedText;
        _dialogueText.ForceMeshUpdate();

        _currentVisibleChars = 0;
        _dialogueText.maxVisibleCharacters = 0;
        _typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    /// <summary>
    /// Updates speaker name, bubble color, and portrait side from the given <see cref="SpeakerData"/>.
    /// Resets to defaults when <paramref name="speaker"/> is <c>null</c>.
    /// </summary>
    private void ApplySpeaker(SpeakerData speaker)
    {
        if (speaker == null)
        {
            _speakerNameText.text = string.Empty;
            _bubbleColorImage.color = Color.white;
            SetPortrait(null, SpeakerSide.Left);
            return;
        }

        _speakerNameText.text = speaker.speakerName;
        _bubbleColorImage.color = speaker.nameColor;
        SetPortrait(speaker.portrait, speaker.side);
    }

    /// <summary>
    /// Positions the correct portrait image and repositions the dialogue arrow
    /// based on whether the speaker is on the left or right <paramref name="side"/>.
    /// Also updates <c>_activePortrait</c> so the impulse effect knows which image to animate.
    /// </summary>
    private void SetPortrait(Sprite portrait, SpeakerSide side)
    {
        if (side == SpeakerSide.Left)
        {
            _arrow.anchoredPosition = new Vector2(-450, 101.35f);
            _leftPortraitImage.transform.SetAsLastSibling();
            _rightPortraitImage.transform.SetAsFirstSibling();
            _leftPortraitImage.sprite = portrait;
            _activePortrait = _leftPortraitImage;
        }
        else
        {
            _arrow.anchoredPosition = new Vector2(450, 101.35f);
            _leftPortraitImage.transform.SetAsFirstSibling();
            _rightPortraitImage.transform.SetAsLastSibling();
            _rightPortraitImage.sprite = portrait;
            _activePortrait = _rightPortraitImage;
        }
    }

    /// <summary>
    /// Reveals characters one by one at <c>_charsPerSecond</c>.
    /// Pauses briefly after punctuation characters.
    /// Fires a portrait impulse on every revealed character.
    /// </summary>
    private IEnumerator TypewriterCoroutine()
    {
        _isTyping = true;
        _updateBtnTextCallback();

        float charDelay = 1f / Mathf.Max(1f, _charsPerSecond);
        int totalChars = _dialogueText.textInfo.characterCount;

        while (_currentVisibleChars < totalChars)
        {
            yield return new WaitForSeconds(charDelay);

            _currentVisibleChars++;
            _dialogueText.maxVisibleCharacters = _currentVisibleChars;
            _typingAudio?.Invoke();

            //Extra pause on panctuation!
            char revealed = _dialogueText.textInfo.characterInfo[_currentVisibleChars - 1].character;
            if (IsPunctuation(revealed))
                yield return new WaitForSeconds(_punctuationPause);
        }

        _isTyping = false;
        _updateBtnTextCallback();
    }

    /// <summary>
    /// Triggers a portrait scale impulse. If no impulse is currently running, starts
    /// <see cref="PortraitImpulsing"/>. If one is already in progress, resets
    /// <c>_currentImpulsingTime</c> to restart the lerp from the peak scale,
    /// effectively retriggering the effect without creating a new coroutine.
    /// </summary>
    private void PortraitImpulse()//Called from PlayTypingAudio() to play when sound is playing. Added at the end of the project.
    {
        if (!_impulsing)
            _impulsingCoroutine = StartCoroutine(PortraitImpulsing());
        else
            _currentImpulsingTime = 0;
    }

    /// <summary>
    /// Lerps the active portrait's scale from <c>_impulseScale</c> back down to its
    /// original size over <c>_impulseDownDuration</c> seconds.
    /// <para>
    /// The loop exits early when <c>_impulsing</c> is set to <c>false</c> externally
    /// (e.g. on skip or dialogue end), restoring the original scale immediately.
    /// </para>
    /// </summary>
    private IEnumerator PortraitImpulsing()
    {
        _impulsing = true;
        Transform portraitTransform = _activePortrait.transform;
        Vector3 baseScale = portraitTransform.localScale;
        Vector3 impulseScale = new Vector3(_impulseScale, _impulseScale, _impulseScale);
        _currentImpulsingTime = 0;
        while (_impulsing && _currentImpulsingTime < 1)
        {
            _currentImpulsingTime += Time.deltaTime / _impulseDownDuration;
            portraitTransform.localScale = Vector3.Lerp(impulseScale, baseScale, _currentImpulsingTime);
            yield return null;
        }
        portraitTransform.localScale = baseScale;
        _impulsing = false;
    }

    /// <summary>
    /// Stops the typewriter coroutine without marking the line as complete.
    /// Used when the device changes mid-type so the coroutine can be restarted cleanly.
    /// </summary>
    public void StopTypewriter()
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _impulsing = false;
    }

    /// <summary>
    /// Resumes the typewriter from the current visible character position.
    /// </summary>
    public void ResumeTypewriter()
    {
        _typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    /// <summary>
    /// Instantly reveals all remaining characters in the current line.
    /// </summary>
    public void CompleteCurrentLine()
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _impulsing = false;

        _dialogueText.ForceMeshUpdate();
        _currentVisibleChars = _dialogueText.textInfo.characterCount;
        _dialogueText.maxVisibleCharacters = _currentVisibleChars;
        _isTyping = false;
    }

    /// <summary>
    /// Re-assigns processed text (with updated sprite tags) without resetting the
    /// typewriter position. Called when the input device changes mid-dialogue.
    /// </summary>
    /// <param name="processedText">Dialogue text with tokens replaced by the new device's TMP sprite tags.</param>
    public void RefreshGlyphs(string processedText)
    {
        _dialogueText.text = processedText;
        _dialogueText.ForceMeshUpdate();
        _dialogueText.maxVisibleCharacters = _currentVisibleChars;
    }

    /// <summary>
    /// Stops the typewriter and clears the typing flag.
    /// Called by the Skip button to abort the entire dialogue sequence.
    /// </summary>
    public void SkipDialogue()
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _impulsing = false;
        _isTyping = false;
    }

    private bool IsPunctuation(char character)
    {
        //Thought of having dynamic characters, getting them from a string exposed in inspector, but it was too much in my opinion so I made it simpler.
        return character == '.' || character == ',' || character == '!' || character == '?';
    }

    /// <summary>
    /// Plays a sound only at the start of each word — when the previous character was
    /// a space, or at the very first character of the line.
    /// </summary>
    private void PlayTypingAudioOnWord()
    {
        bool isWordStart = _currentVisibleChars == 1
            || _dialogueText.textInfo.characterInfo[_currentVisibleChars - 2].character == ' ';
        if (!isWordStart)
            return;
        PlayTypingAudio();
    }

    /// <summary>
    /// Picks a random clip from <c>typingSounds</c> and plays it as a one-shot.
    /// </summary>
    private void PlayTypingAudio()
    {
        PortraitImpulse();
        audioSource.PlayOneShot(typingSounds[UnityEngine.Random.Range(0, typingSounds.Length)]);
    }
}
