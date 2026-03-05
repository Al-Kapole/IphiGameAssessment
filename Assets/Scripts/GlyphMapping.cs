using System;
using UnityEngine;

// Maps a single inline token (e.g. "interact") to sprite names per device type.
// Configure instances of this in the GlyphConfig ScriptableObject.
[Serializable]
public class GlyphMapping
{
    [Tooltip("Token string used inside dialogue text, without brackets.\n"+
            "Example: \"interact\" matches the token [interact] in text.")]
    public string token;

    public string keyboardSpriteName;
    public string gamepadSpriteName;
}
