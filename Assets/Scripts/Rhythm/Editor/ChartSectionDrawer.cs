using UnityEditor;
using UnityEngine;

namespace Brewing.Rhythm.Editor
{
    /// <summary>
    /// Shows a chart section on one line. The start beat is greyed out because the chart works it out from
    /// the lengths of the sections before it.
    /// </summary>
    [CustomPropertyDrawer(typeof(ChartSection))]
    public class ChartSectionDrawer : PropertyDrawer
    {
        const float Gap = 6f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 44f;

            float width = (position.width - Gap * 2f) / 3f;
            var start = new Rect(position.x, position.y, width, position.height);
            var length = new Rect(start.xMax + Gap, position.y, width, position.height);
            var mode = new Rect(length.xMax + Gap, position.y, width, position.height);

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(start, property.FindPropertyRelative("startBeat"), new GUIContent("Start", "Worked out from the lengths of the sections before it."));
            EditorGUI.PropertyField(length, property.FindPropertyRelative("lengthBeats"), new GUIContent("Length", "Length of the section, in beats."));
            EditorGUI.PropertyField(mode, property.FindPropertyRelative("mode"), GUIContent.none);

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
