using UnityEditor;
using UnityEngine;

// Custom inspector for DialogueData.
// Draws the speakers list normally, then for each entry renders a dropdown
// populated from the speakers list instead of a raw integer field.
[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    private SerializedProperty _speakersProperty;
    private SerializedProperty _entriesProperty;

    private void OnEnable()
    {
        _speakersProperty = serializedObject.FindProperty("speakers");
        _entriesProperty = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSpeakersList();
        EditorGUILayout.Space(12);
        DrawEntriesList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpeakersList()
    {
        EditorGUILayout.LabelField("Speakers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_speakersProperty, true);
    }

    private void DrawEntriesList()
    {
        EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);

        string[] speakerNames = BuildSpeakerNameArray();

        for (int i = 0; i < _entriesProperty.arraySize; i++)
        {
            bool removed = DrawEntry(i, speakerNames);
            if (removed)
            {
                // Bail out immediately — array indices are now stale.
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("+ Add Entry"))
            _entriesProperty.InsertArrayElementAtIndex(_entriesProperty.arraySize);
    }

    // Returns true if the entry was removed (caller must stop iterating).
    private bool DrawEntry(int index, string[] speakerNames)
    {
        SerializedProperty entry = _entriesProperty.GetArrayElementAtIndex(index);
        SerializedProperty speakerIndexProp = entry.FindPropertyRelative("speakerIndex");
        SerializedProperty textProp = entry.FindPropertyRelative("text");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Header row: entry label + remove button.
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Entry " + index, EditorStyles.boldLabel);
        if (GUILayout.Button("Remove", GUILayout.Width(64)))
        {
            _entriesProperty.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        EditorGUILayout.EndHorizontal();

        // Clamp index so it never goes out of range when speakers are removed.
        int clampedIndex = Mathf.Clamp(speakerIndexProp.intValue, 0, Mathf.Max(0, speakerNames.Length - 1));
        speakerIndexProp.intValue = EditorGUILayout.Popup("Speaker", clampedIndex, speakerNames);

        EditorGUILayout.PropertyField(textProp, new GUIContent("Text"));

        EditorGUILayout.EndVertical();
        return false;
    }

    // Builds the string array used by EditorGUILayout.Popup.
    // Falls back to a placeholder when the speakers list is empty.
    private string[] BuildSpeakerNameArray()
    {
        DialogueData data = (DialogueData)target;

        if (data.speakers == null || data.speakers.Count == 0)
            return new string[] { "(No speakers — add one above)" };

        string[] names = new string[data.speakers.Count];
        for (int i = 0; i < data.speakers.Count; i++)
        {
            SpeakerData speaker = data.speakers[i];
            names[i] = speaker != null ? speaker.speakerName : "(null)";
        }
        return names;
    }
}
