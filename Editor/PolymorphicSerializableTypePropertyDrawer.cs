#nullable enable
using UnityEngine;
using UnityEditor;
using PolymorphicEditorTools.Abstractions;

namespace PolymorphicEditorTools.Editor
{
    [CustomPropertyDrawer(typeof(PolymorphicSerializableTypeAttribute), useForChildren: false)]
    public class PolymorphicSerializableTypePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
        }
    }
}
