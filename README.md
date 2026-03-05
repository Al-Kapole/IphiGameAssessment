# Dialogue System — IPHIGAMES Assessment

A Unity dialogue system built as a technical assessment. It features a typewriter-style text reveal, per-speaker visuals (portrait, name colour, side), inline input-device glyphs that swap automatically when the player switches between keyboard and gamepad, and a portrait impulse animation tied to the typing audio.

---

## How to Run the Demo Scene

1. Open the project in Unity.
2. Navigate to **Assets → Scenes** and open the **Main** scene.
3. Hit **Play**.

### Inspector Notes

| GameObject | What to configure |
|---|---|
| `InputDeviceDetector` (root of Hierarchy) | Uncheck **Auto Detect** to disable real hardware detection. The device can then be forced using the toggles at the top of the screen and will remain fixed even when the mouse is moved. |
| `Canvas → DialoguePanel → DialogueViewer` | The **Play Sound Every** enum lets you choose between triggering the typing sound (and portrait impulse) on every **letter** or only at the start of each **word**. |

---

## Known Limitations & What I Would Do Differently

**Portrait impulse placement**
The portrait impulse effect is currently called from inside `PlayTypingAudio()`. Ideally it would be triggered independently — decoupled from the audio path — so that the impulse and sound could be configured and toggled separately. Because the typing audio already offered two modes (per-letter and per-word), hooking the impulse into that same method was the pragmatic choice within the time available.

---

## Unity Version

**6000.3.9f1** (Unity 6)
