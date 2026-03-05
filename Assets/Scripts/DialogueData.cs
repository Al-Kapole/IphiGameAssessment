using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds the full cast and all dialogue entries for a conversation.
/// Create via: Assets > Create > Dialogue > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("All speakers who can appear in this dialogue. Add SpeakerData assets here first,"+
             " then select them per entry via the dropdown in the custom inspector.")]
    public List<SpeakerData> speakers = new List<SpeakerData>();

    public List<DialogueEntry> entries = new List<DialogueEntry>();

    /// <summary>
    /// Returns the SpeakerData at the given index, or null if out of range.
    /// </summary>
    public SpeakerData GetSpeaker(int index)
    {
        if (index >= 0 && index < speakers.Count)
            return speakers[index];
        return null;
    }
}
