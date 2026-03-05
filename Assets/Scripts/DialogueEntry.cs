using System;
using UnityEngine;

[Serializable]
public class DialogueEntry
{
    // Index into DialogueData.speakers. The custom inspector renders this as a dropdown.
    public int speakerIndex;

    [TextArea(2, 6)]
    public string text;
}
