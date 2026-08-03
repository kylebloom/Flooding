using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    [CustomPropertyDrawer(typeof(FluidBoundaryReference))]
    internal sealed class FluidBoundaryReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            var componentProperty = property.FindPropertyRelative("component");
            EditorGUI.BeginProperty(position, label, property);

            var assigned = componentProperty.objectReferenceValue;
            var newValue = EditorGUI.ObjectField(
                position,
                label,
                assigned,
                typeof(Component),
                true);

            if (newValue != assigned)
            {
                if (
                    newValue == null
                    || newValue is IFluidBoundary)
                {
                    componentProperty.objectReferenceValue = newValue;
                }
                else
                {
                    Debug.LogWarning(
                        "FloodConnection endpoints must be a FloodVolume or External Fluid Body.",
                        newValue);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
