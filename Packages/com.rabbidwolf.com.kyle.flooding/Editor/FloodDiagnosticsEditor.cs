using Kyle.Flooding;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Draws read-only flooding diagnostics for a selected diagnostic root.
    /// </summary>
    [CustomEditor(typeof(FloodDiagnostics))]
    public sealed class FloodDiagnosticsEditor : UnityEditor.Editor
    {
        private static GUIStyle labelStyle;

        private static GUIStyle LabelStyle
        {
            get
            {
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        richText = false,
                    };
                }

                return labelStyle;
            }
        }

        private void OnSceneGUI()
        {
            var diagnostics = (FloodDiagnostics)target;
            var snapshot = diagnostics.CaptureSnapshot();

            if (diagnostics.ShowCentersOfMass)
                DrawCentersOfMass(diagnostics, snapshot);

            if (diagnostics.ShowGravity && snapshot.HasGravity)
                DrawGravity(diagnostics, snapshot);

            if (diagnostics.ShowSurfacePlanes)
                DrawSurfacePlanes(diagnostics, snapshot);

            if (diagnostics.ShowConnections)
                DrawConnections(diagnostics, snapshot);
        }

        private static void DrawCentersOfMass(
            FloodDiagnostics diagnostics,
            FloodDiagnosticSnapshot snapshot)
        {
            if (snapshot.Water.Mass > 0d)
            {
                DrawCenterMarker(
                    snapshot.Water.CenterOfMassWorld,
                    diagnostics.CenterOfMassMarkerRadius,
                    diagnostics.WaterCenterColor,
                    $"Water COM ({snapshot.Water.Mass:0.###} kg)");
            }

            if (snapshot.HasDryCenter)
            {
                DrawCenterMarker(
                    snapshot.DryCenterWorld,
                    diagnostics.CenterOfMassMarkerRadius,
                    diagnostics.DryCenterColor,
                    $"Dry COM ({snapshot.DryMass:0.###} kg)");
            }

            if (snapshot.HasCombinedCenter)
            {
                DrawCenterMarker(
                    snapshot.CombinedCenterWorld,
                    diagnostics.CenterOfMassMarkerRadius,
                    diagnostics.CombinedCenterColor,
                    $"Rigidbody COM ({snapshot.CombinedMass:0.###} kg)");
            }
        }

        private static void DrawCenterMarker(
            Vector3 position,
            float radius,
            Color color,
            string label)
        {
            Handles.color = color;
            Handles.SphereHandleCap(
                0,
                position,
                Quaternion.identity,
                radius * 2f,
                EventType.Repaint);
            Handles.Label(position + (Vector3.up * radius), label, LabelStyle);
        }

        private static void DrawGravity(
            FloodDiagnostics diagnostics,
            FloodDiagnosticSnapshot snapshot)
        {
            var gravity = snapshot.ActiveGravityWorld;
            Handles.color = diagnostics.GravityColor;

            if (gravity.sqrMagnitude > Mathf.Epsilon)
            {
                Handles.ArrowHandleCap(
                    0,
                    snapshot.OriginWorld,
                    Quaternion.LookRotation(gravity.normalized),
                    diagnostics.GravityArrowLength,
                    EventType.Repaint);
            }

            Handles.Label(
                snapshot.OriginWorld,
                $"Gravity {gravity.magnitude:0.###} m/s²\n"
                + $"({gravity.x:0.###}, {gravity.y:0.###}, "
                + $"{gravity.z:0.###})",
                LabelStyle);
        }

        private static void DrawSurfacePlanes(
            FloodDiagnostics diagnostics,
            FloodDiagnosticSnapshot snapshot)
        {
            Handles.color = diagnostics.SurfacePlaneColor;

            foreach (var volume in snapshot.Volumes)
            {
                if (volume.Source == null)
                    continue;

                var state = volume.State;
                var plane = state.SurfacePlane;
                var center = plane.ClosestPointOnPlane(
                    volume.Source.transform.position);
                var tangent = Vector3.Cross(plane.normal, Vector3.up);

                if (tangent.sqrMagnitude <= Mathf.Epsilon)
                    tangent = Vector3.Cross(plane.normal, Vector3.right);

                tangent.Normalize();
                var bitangent =
                    Vector3.Cross(plane.normal, tangent).normalized;
                var halfSize = diagnostics.SurfacePlaneSize * 0.5f;
                var cornerA =
                    center + ((tangent + bitangent) * halfSize);
                var cornerB =
                    center + ((tangent - bitangent) * halfSize);
                var cornerC =
                    center + ((-tangent - bitangent) * halfSize);
                var cornerD =
                    center + ((-tangent + bitangent) * halfSize);

                Handles.DrawAAPolyLine(
                    2f,
                    cornerA,
                    cornerB,
                    cornerC,
                    cornerD,
                    cornerA);
                Handles.DrawLine(
                    center,
                    center + (plane.normal * halfSize),
                    2f);
                Handles.Label(
                    center,
                    $"{volume.Source.name} surface\n"
                    + $"{state.Volume:0.###}/{state.Capacity:0.###} m³",
                    LabelStyle);
            }
        }

        private static void DrawConnections(
            FloodDiagnostics diagnostics,
            FloodDiagnosticSnapshot snapshot)
        {
            Handles.color = diagnostics.ConnectionColor;

            foreach (var connection in snapshot.Connections)
            {
                if (connection.Source == null)
                    continue;

                if (connection.DirectionWorld.sqrMagnitude > Mathf.Epsilon)
                {
                    Handles.ArrowHandleCap(
                        0,
                        connection.PositionWorld,
                        Quaternion.LookRotation(connection.DirectionWorld),
                        diagnostics.FlowArrowLength,
                        EventType.Repaint);
                }

                Handles.Label(
                    connection.PositionWorld,
                    $"{connection.Source.name}\n"
                    + $"head {connection.PressureHeadDifference:+0.###;-0.###;0} m\n"
                    + $"requested {connection.RequestedFlowRate:+0.###;-0.###;0} m³/s\n"
                    + $"applied {connection.AppliedFlowRate:+0.###;-0.###;0} m³/s",
                    LabelStyle);
            }
        }
    }
}
