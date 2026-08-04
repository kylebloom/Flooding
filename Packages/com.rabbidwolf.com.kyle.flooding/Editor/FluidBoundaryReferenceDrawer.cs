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
                typeof(Object),
                true);

            if (newValue != assigned)
            {
                if (FluidBoundaryReference.TryResolveComponent(
                        newValue,
                        out var resolved))
                {
                    componentProperty.objectReferenceValue = resolved;
                }
                else
                {
                    Debug.LogWarning(
                        "FloodConnection endpoints must reference a GameObject "
                        + "containing a FloodVolume or External Fluid Body, "
                        + "or assign the boundary component directly.",
                        newValue);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
