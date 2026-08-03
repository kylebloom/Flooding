using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    [CustomEditor(typeof(FloodConnection))]
    internal sealed class FloodConnectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var connection = (FloodConnection)target;

            if (!connection.TryValidateEndpoints(out var message))
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else if (
                connection.SideA is ExternalFluidBoundary
                || connection.SideB is ExternalFluidBoundary)
            {
                EditorGUILayout.HelpBox(
                    "External Fluid Body endpoints are infinite. Only the "
                    + "finite FloodVolume side receives committed volume "
                    + "changes. Densities must match.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
