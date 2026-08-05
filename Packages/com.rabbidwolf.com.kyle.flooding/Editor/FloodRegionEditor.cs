using Kyle.Flooding;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// FloodRegion Inspector with Bake Region controls, stale diagnostics, and
    /// selected occupancy visualization.
    /// </summary>
    [CustomEditor(typeof(FloodRegion))]
    public sealed class FloodRegionEditor : UnityEditor.Editor
    {
        private const int MaximumVisualizedCells = 10000;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var region = (FloodRegion)target;
            EditorGUILayout.Space();

            if (!region.TryValidateMembers(out var memberMessage))
            {
                EditorGUILayout.HelpBox(memberMessage, MessageType.Error);
            }
            else if (region.Members.Count >= 2)
            {
                if (FloodRegionBaker.TryGetStatus(
                        region,
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
            }

            if (region.BakedRegionData != null
                && region.BakedRegionData.IsUsable)
            {
                var data = region.BakedRegionData;
                EditorGUILayout.LabelField(
                    "Region Sample Count",
                    data.SampleCount.ToString("N0"));
                EditorGUILayout.LabelField(
                    "Actual Sample Resolution",
                    $"{data.SampleResolution.x:0.###} × "
                    + $"{data.SampleResolution.y:0.###} × "
                    + $"{data.SampleResolution.z:0.###} m");
                EditorGUILayout.LabelField(
                    "Baked Union Capacity",
                    $"{data.Capacity:0.###} m³");
                EditorGUILayout.LabelField(
                    "Approximation Indicator",
                    $"{data.EstimatedApproximationVolume:0.###} m³");
                EditorGUILayout.LabelField(
                    "Bake Format",
                    data.FormatVersion.ToString());
                EditorGUILayout.LabelField(
                    "Presentation Boundary",
                    data.HasPresentationBoundary
                        ? $"{data.PresentationBoundaryVertexCount:N0} vertices, "
                            + $"{data.PresentationBoundaryTriangleCount:N0} triangles"
                        : "None (voxel free-surface fallback)");
            }

            using (new EditorGUI.DisabledScope(region.Members.Count < 2))
            {
                if (GUILayout.Button("Bake Region"))
                {
                    if (!FloodRegionBaker.TryBake(
                            region,
                            out _,
                            out var message))
                    {
                        Debug.LogError(message, region);
                        EditorUtility.DisplayDialog(
                            "Flood Region Bake Failed",
                            message,
                            "OK");
                    }
                    else
                    {
                        Debug.Log(message, region);
                    }
                }
            }

            if (region.Members.Count >= 2
                && region.BakedRegionData != null
                && GUILayout.Button("Clear Baked Region Data"))
            {
                Undo.RecordObject(region, "Clear Flood Region Bake");
                region.AssignBake(null);
                EditorUtility.SetDirty(region);
            }
        }

        private void OnSceneGUI()
        {
            var region = (FloodRegion)target;
            var data = region.BakedRegionData;

            if (!region.VisualizeBake
                || data == null
                || !data.IsUsable)
            {
                return;
            }

            Handles.matrix = region.transform.localToWorldMatrix;
            Handles.color = new Color(0.15f, 0.75f, 0.95f, 0.22f);
            var count = Mathf.Min(
                data.SampleCount,
                MaximumVisualizedCells);

            for (var index = 0; index < count; index++)
            {
                Handles.DrawWireCube(
                    data.GetCellCenter(data.OccupiedCellIndices[index]),
                    data.CellSize);
            }

            Handles.color = new Color(1f, 0.55f, 0.1f, 1f);
            Handles.DrawWireCube(
                data.LocalBounds.center,
                data.LocalBounds.size);
            Handles.matrix = Matrix4x4.identity;
        }
    }
}
