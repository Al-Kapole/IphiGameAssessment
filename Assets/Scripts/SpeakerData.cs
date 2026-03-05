using UnityEngine;

/// <summary>
/// ScriptableObject that holds all visual data for a single dialogue speaker:
/// <para>
/// Create via: Assets &gt; Create &gt; Dialogue &gt; Speaker Data
/// </para>
/// </summary>
[CreateAssetMenu(fileName = "SpeakerData", menuName = "Dialogue/Speaker Data")]
public class SpeakerData : ScriptableObject
{
    [Tooltip("The name displayed in the speaker name label.")]
    public string speakerName;

    [Tooltip("Portrait sprite shown in the left or right portrait image, depending on side.")]
    public Sprite portrait;

    [Tooltip("Color applied to the name bubble background. Defaults to white.")]
    public Color nameColor = Color.white;

    [Tooltip("Which side of the dialogue box this speaker appears on.")]
    public SpeakerSide side = SpeakerSide.Left;
}

public enum SpeakerSide
{
    Left,
    Right
}
