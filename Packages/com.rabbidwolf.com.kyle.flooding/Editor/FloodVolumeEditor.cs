using Kyle.Flooding;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Provides conditional geometry authoring, validation, and polygon handles.
    /// </summary>
    [CustomEditor(typeof(FloodVolume))]
    public sealed class FloodVolumeEditor : UnityEditor.Editor
    {
        private SerializedProperty simulationManager;
        private SerializedProperty geometryMode;
        private SerializedProperty width;
        private SerializedProperty length;
        private SerializedProperty polygonFootprint;
        private SerializedProperty maximumHeight;
        private SerializedProperty bakedVolumeData;
        private SerializedProperty waterDensity;
        private SerializedProperty initialVolume;

        private void OnEnable()
        {
            simulationManager =
                serializedObject.FindProperty("simulationManager");
            geometryMode = serializedObject.FindProperty("geometryMode");
            width = serializedObject.FindProperty("width");
            length = serializedObject.FindProperty("length");
            polygonFootprint =
                serializedObject.FindProperty("polygonFootprint");
            maximumHeight = serializedObject.FindProperty("maximumHeight");
            bakedVolumeData =
                serializedObject.FindProperty("bakedVolumeData");
            waterDensity = serializedObject.FindProperty("waterDensity");
            initialVolume = serializedObject.FindProperty("initialVolume");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((FloodVolume)target),
                    typeof(FloodVolume),
                    false);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(simulationManager);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Floodable Space",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(geometryMode);

            var selectedMode =
                (FloodGeometryMode)geometryMode.enumValueIndex;

            if (selectedMode == FloodGeometryMode.RectangularPrism)
            {
                EditorGUILayout.PropertyField(width);
                EditorGUILayout.PropertyField(length);
            }
            else if (selectedMode == FloodGeometryMode.ExtrudedPolygon)
            {
                EditorGUILayout.HelpBox(
                    "Points form one closed local XZ perimeter in list order. "
                    + "Concave outlines are supported. Holes, duplicate points, "
                    + "and crossing edges are not.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(
                    polygonFootprint,
                    includeChildren: true);

                if (GUILayout.Button("Reset To 5 m Rectangle"))
                    ResetPolygon();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assign immutable data produced by a Flood Volume Authoring "
                    + "component. Source meshes are never analyzed at runtime.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(bakedVolumeData);
            }

            if (selectedMode != FloodGeometryMode.BakedData)
                EditorGUILayout.PropertyField(maximumHeight);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fluid", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(waterDensity);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Initial State",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(initialVolume);

            serializedObject.ApplyModifiedProperties();
            DrawValidation();
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();

            if ((FloodGeometryMode)geometryMode.enumValueIndex
                != FloodGeometryMode.ExtrudedPolygon)
            {
                return;
            }

            var volume = (FloodVolume)target;
            var volumeTransform = volume.transform;
            var pointCount = polygonFootprint.arraySize;

            if (pointCount < 1)
                return;

            Handles.color = new Color(0f, 0.65f, 1f, 1f);

            for (var index = 0; index < pointCount; index++)
            {
                var pointProperty =
                    polygonFootprint.GetArrayElementAtIndex(index);
                var localPoint = pointProperty.vector2Value;
                var worldPoint = volumeTransform.TransformPoint(
                    new Vector3(localPoint.x, 0f, localPoint.y));
                var nextPoint =
                    polygonFootprint.GetArrayElementAtIndex(
                        (index + 1) % pointCount).vector2Value;
                var worldNext = volumeTransform.TransformPoint(
                    new Vector3(nextPoint.x, 0f, nextPoint.y));

                Handles.DrawLine(worldPoint, worldNext, 2f);
                Handles.Label(worldPoint, index.ToString());

                EditorGUI.BeginChangeCheck();
                var movedWorldPoint = Handles.PositionHandle(
                    worldPoint,
                    volumeTransform.rotation);

                if (!EditorGUI.EndChangeCheck())
                    continue;

                Undo.RecordObject(volume, "Move Flood Polygon Point");
                var movedLocalPoint =
                    volumeTransform.InverseTransformPoint(movedWorldPoint);
                pointProperty.vector2Value =
                    new Vector2(movedLocalPoint.x, movedLocalPoint.z);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawValidation()
        {
            var volume = (FloodVolume)target;

            if (!volume.TryValidateGeometry(out var message))
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
                return;
            }

            var geometry = volume.Geometry;

            if (geometry == null)
                return;

            EditorGUILayout.HelpBox(
                $"Valid geometry — capacity {geometry.Capacity:0.###} m³",
                MessageType.None);
        }

        private void ResetPolygon()
        {
            polygonFootprint.arraySize = 4;
            polygonFootprint.GetArrayElementAtIndex(0).vector2Value =
                new Vector2(-2.5f, -2.5f);
            polygonFootprint.GetArrayElementAtIndex(1).vector2Value =
                new Vector2(2.5f, -2.5f);
            polygonFootprint.GetArrayElementAtIndex(2).vector2Value =
                new Vector2(2.5f, 2.5f);
            polygonFootprint.GetArrayElementAtIndex(3).vector2Value =
                new Vector2(-2.5f, 2.5f);
        }
    }
}
