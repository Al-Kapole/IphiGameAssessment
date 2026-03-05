using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ScriptableObject that maps inline dialogue tokens to TMP sprite tags per input device.
/// Create via: Assets &gt; Create &gt; Dialogue &gt; Glyph Config
/// <para>
/// Sprite assets must be configured in the Inspector:
/// <list type="bullet">
/// <item>Open Keyboard.asset and add Ps4.asset + Xbox.asset to its Fallback Sprite Assets list.</item>
/// <item>Assign Keyboard.asset as the Sprite Asset on every TMP_Text that displays glyphs.</item>
/// </list>
/// </para>
/// <para>
/// Default token setup (sprite names that exist in the provided assets):
/// <list type="bullet">
/// <item>token "interact" → Keyboard: e    | PS4: buttonSouth   | Xbox: buttonSouth</item>
/// <item>token "next"     → Keyboard: space | PS4: buttonSouth   | Xbox: buttonSouth</item>
/// <item>token "skip"     → Keyboard: tab  | PS4: rightShoulder | Xbox: rightShoulder</item>
/// </list>
/// </para>
/// </summary>
[CreateAssetMenu(fileName = "GlyphConfig", menuName = "Dialogue/Glyph Config")]
public class GlyphConfig : ScriptableObject
{
    public TMP_SpriteAsset keyboardSpriteAsset;
    public TMP_SpriteAsset ps4SpriteAsset;
    public TMP_SpriteAsset xboxSpriteAsset;

    [SerializeField, Tooltip("Maps a single inline token (e.g. \"interact\") to sprite names per device type.")]
    private List<GlyphMapping> _mappings = new();

    /// <summary>
    /// Replaces all [token] placeholders in <paramref name="rawText"/> with the correct
    /// TMP sprite tag for the given <paramref name="deviceType"/>.
    /// </summary>
    /// <param name="rawText">The raw dialogue string containing [token] placeholders.</param>
    /// <param name="deviceType">The active input device that determines which sprite is used.</param>
    /// <returns>The processed string with all recognised tokens replaced by TMP sprite tags.</returns>
    public string ProcessText(string rawText, InputDeviceType deviceType)
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        string result = rawText;
        foreach (GlyphMapping mapping in _mappings)
        {
            string placeholder = "[" + mapping.token + "]";
            string tag = BuildSpriteTag(mapping, deviceType);
            result = result.Replace(placeholder, tag);
        }
        return result;
    }

    /// <summary>
    /// Builds a single TMP sprite tag for the given <paramref name="token"/> and
    /// <paramref name="deviceType"/>. Returns the raw token string as fallback if the
    /// token is not found in the mappings list.
    /// </summary>
    /// <param name="token">The token name without brackets, e.g. "interact".</param>
    /// <param name="deviceType">The active input device that determines which sprite is used.</param>
    /// <returns>A TMP sprite tag string, or the bracketed token as a fallback.</returns>
    public string BuildSpriteTag(string token, InputDeviceType deviceType)
    {
        foreach (GlyphMapping mapping in _mappings)
        {
            if (mapping.token == token)
                return BuildSpriteTag(mapping, deviceType);
        }
        return "[" + token + "]";
    }

    private string BuildSpriteTag(GlyphMapping mapping, InputDeviceType deviceType)
    {
        string assetName;
        string spriteName;

        switch (deviceType)
        {
            case InputDeviceType.Gamepad_PS4:
                assetName = ps4SpriteAsset != null ? ps4SpriteAsset.name : "Ps4";
                spriteName = mapping.gamepadSpriteName;
                break;
            case InputDeviceType.Gamepad_Xbox:
                assetName = xboxSpriteAsset != null ? xboxSpriteAsset.name : "Xbox";
                spriteName = mapping.gamepadSpriteName;
                break;
            default:
                assetName = keyboardSpriteAsset != null ? keyboardSpriteAsset.name : "Keyboard";
                spriteName = mapping.keyboardSpriteName;
                break;
        }

        return "<sprite=\"" + assetName + "\" name=\"" + spriteName + "\">";
    }
}
