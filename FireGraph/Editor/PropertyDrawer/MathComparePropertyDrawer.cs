using FireGraph.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireGraph.Editor.PropertyDrawer
{
    [CustomPropertyDrawer(typeof(MathCompareAttribute))]
    public class MathComparePropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [MathOperationDropdown] on a string.");
                return;
            }

            string[] options = MathNode.Compare;

            int currentIndex = System.Array.IndexOf(options, property.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);

            property.stringValue = options[newIndex];
        }
    }
}