using Kyle.Flooding;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Provides bake controls, stale-data diagnostics, and selected sample
    /// visualization without placing mesh analysis in the runtime assembly.
    /// </summary>
    [CustomEditor(typeof(FloodVolumeAuthoring))]
    public sealed class FloodVolumeAuthoringEditor : UnityEditor.Editor
    {
        private const int MaximumVisualizedCells = 10000;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var authoring = (FloodVolumeAuthoring)target;
            EditorGUILayout.Space();

            if (FloodVolumeBaker.TryGetStatus(
                    authoring,
                    out var stale,
                    out var status))
            {
                EditorGUILayout.HelpBox(
                    status,
                    stale ? MessageType.Warning : MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(status, MessageType.Error);
            }

            if (authoring.BakedData != null
                && authoring.BakedData.IsUsable)
            {
                var data = authoring.BakedData;
                EditorGUILayout.LabelField(
                    "Sample Count",
                    data.SampleCount.ToString("N0"));
                EditorGUILayout.LabelField(
                    "Actual Sample Resolution",
                    $"{data.SampleResolution.x:0.###} × "
                    + $"{data.SampleResolution.y:0.###} × "
                    + $"{data.SampleResolution.z:0.###} m");
                EditorGUILayout.LabelField(
                    "Baked Capacity",
                    $"{data.Capacity:0.###} m³");
                EditorGUILayout.LabelField(
                    "Approximation Indicator",
                    $"{data.EstimatedApproximationVolume:0.###} m³");
            }

            if (!GUILayout.Button("Bake Closed Mesh To Flood Volume Data"))
                return;

            if (!FloodVolumeBaker.TryBake(
                    authoring,
                    out _,
                    out var message))
            {
                Debug.LogError(message, authoring);
                EditorUtility.DisplayDialog(
                    "Flood Volume Bake Failed",
                    message,
                    "OK");
                return;
            }

            Debug.Log(message, authoring);
        }

        private void OnSceneGUI()
        {
            var authoring = (FloodVolumeAuthoring)target;
            var data = authoring.BakedData;

            if (!authoring.VisualizeBake
                || data == null
                || !data.IsUsable
                || authoring.TargetVolume == null)
            {
                return;
            }

            Handles.matrix =
                authoring.TargetVolume.transform.localToWorldMatrix;
            Handles.color = new Color(0f, 0.65f, 1f, 0.22f);
            var count = Mathf.Min(
                data.SampleCount,
                MaximumVisualizedCells);

            for (var index = 0; index < count; index++)
            {
                Handles.DrawWireCube(
                    data.GetCellCenter(data.OccupiedCellIndices[index]),
                    data.CellSize);
            }

            Handles.color = new Color(1f, 0.75f, 0f, 1f);
            Handles.DrawWireCube(
                data.LocalBounds.center,
                data.LocalBounds.size);
            Handles.matrix = Matrix4x4.identity;
        }
    }
}
