#nullable enable
using UnityEngine;

namespace Polymorphism4Unity.Editor
{
    public interface IWidget
    {
        public virtual void OnGUI(Rect position, SerializedProperty property, GUIContent label);
    }
}