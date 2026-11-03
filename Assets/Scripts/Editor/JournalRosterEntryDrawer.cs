using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(JournalRosterEntry))]
public class JournalRosterEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty kind = property.FindPropertyRelative("kind");
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;

        Rect kindRect = new Rect(position.x, position.y, position.width, line);
        EditorGUI.PropertyField(kindRect, kind);

        Rect valueRect = new Rect(position.x, position.y + line + gap, position.width, line);
        if (kind.enumValueIndex == (int)JournalRosterEntryKind.Location)
        {
            EditorGUI.PropertyField(
                valueRect,
                property.FindPropertyRelative("locationPage"),
                new GUIContent("Location Page")
            );
        }
        else
        {
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("poi"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}
