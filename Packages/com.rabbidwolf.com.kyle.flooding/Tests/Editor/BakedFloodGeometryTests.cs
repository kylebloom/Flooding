using System;
using Kyle.Flooding.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class BakedFloodGeometryTests
    {
        [Test]
        public void GeometryMode_AppendsBakeWithoutChangingExistingValues()
        {
            Assert.That((int)FloodGeometryMode.RectangularPrism, Is.EqualTo(0));
            Assert.That((int)FloodGeometryMode.ExtrudedPolygon, Is.EqualTo(1));
            Assert.That((int)FloodGeometryMode.BakedData, Is.EqualTo(2));
            Assert.That(
                Enum.GetName(typeof(FloodGeometryMode), 2),
                Is.EqualTo(nameof(FloodGeometryMode.BakedData)));
            Assert.That(
                Enum.GetValues(typeof(FloodGeometryMode)).Length,
                Is.EqualTo(3));
        }

        [Test]
        public void BakedRuntimeImplementation_IsNotPublic()
        {
            Assert.That(typeof(BakedFloodGeometry).IsNotPublic, Is.True);
        }

        [Test]
        public void BakedData_EvaluateArbitraryPlaneDeterministically()
        {
            var data = CreateFilledData(new Vector3Int(2, 2, 2));
            var geometry = new BakedFloodGeometry(data);
            var plane = new Plane(
                new Vector3(0.4f, 1f, -0.2f).normalized,
                Vector3.zero);

            var first = geometry.EvaluateSubmersion(plane);
            var second = geometry.EvaluateSubmersion(plane);

            Assert.That(geometry.Capacity, Is.EqualTo(8d).Within(0.000001d));
            Assert.That(first.Volume, Is.EqualTo(4d).Within(0.00001d));
            Assert.That(second.Volume, Is.EqualTo(first.Volume));
            Assert.That(second.Centroid, Is.EqualTo(first.Centroid));
            Assert.That(first.SurfaceIntersection.HasSurface, Is.True);

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void BakedData_ClonesInputAndReportsApproximationIndicator()
        {
            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            var occupied = new[] { 0, 1 };
            data.Initialize(
                new Bounds(Vector3.zero, new Vector3(2f, 1f, 1f)),
                Vector3.one,
                new Vector3Int(2, 1, 1),
                occupied,
                newBoundaryCellCount: 2,
                newSourceFingerprint: "test");

            occupied[0] = 99;

            Assert.That(data.OccupiedCellIndices[0], Is.EqualTo(0));
            Assert.That(data.SampleCount, Is.EqualTo(2));
            Assert.That(data.SampleResolution, Is.EqualTo(Vector3.one));
            Assert.That(data.Capacity, Is.EqualTo(2d));
            Assert.That(data.EstimatedApproximationVolume, Is.EqualTo(2d));
            Assert.That(data.IsUsable, Is.True);
            Assert.That(
                data.FormatVersion,
                Is.EqualTo(FloodVolumeData.LegacyFormatVersion));
            Assert.That(data.HasPresentationBoundary, Is.False);

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void BakedData_WithPresentationBoundary_UsesBoundarySurfaceNotVoxelPatches()
        {
            var box = CreateUnitBoxBoundary();
            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            data.Initialize(
                new Bounds(Vector3.zero, Vector3.one * 2f),
                Vector3.one,
                new Vector3Int(2, 2, 2),
                new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                newBoundaryCellCount: 8,
                newSourceFingerprint: "boundary-test",
                box.vertices,
                box.triangles);

            Assert.That(
                data.FormatVersion,
                Is.EqualTo(FloodVolumeData.CurrentFormatVersion));
            Assert.That(data.HasPresentationBoundary, Is.True);

            var geometry = new BakedFloodGeometry(data);
            var plane = new Plane(Vector3.up, new Vector3(0f, 0f, 0f));
            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(result.Volume, Is.EqualTo(4d).Within(0.00001d));
            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(result.SurfaceIntersection.Contours.Count, Is.EqualTo(1));
            Assert.That(
                result.SurfaceIntersection.Contours[0].Vertices.Count,
                Is.EqualTo(4));

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void BakedData_WithoutPresentationBoundary_FallsBackToVoxelContours()
        {
            var data = CreateFilledData(new Vector3Int(2, 2, 2));
            var geometry = new BakedFloodGeometry(data);
            var plane = new Plane(Vector3.up, Vector3.zero);
            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(data.HasPresentationBoundary, Is.False);
            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(
                result.SurfaceIntersection.Contours.Count,
                Is.GreaterThan(1));

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void SurfaceSolver_HasDeterministicWorkGuardFor512Cells()
        {
            var data = CreateFilledData(new Vector3Int(8, 8, 8));
            var geometry = new BakedFloodGeometry(data);
            var normal = new Vector3(0.3f, 1f, 0.17f).normalized;

            var first = FloodSurfaceSolver.Solve(
                geometry,
                normal,
                targetVolume: 173.25d);
            var second = FloodSurfaceSolver.Solve(
                geometry,
                normal,
                targetVolume: 173.25d);

            Assert.That(data.SampleCount, Is.EqualTo(512));
            Assert.That(
                first.Iterations,
                Is.LessThanOrEqualTo(
                    FloodGeometryTolerances.SolverMaximumIterations));
            Assert.That(second.Iterations, Is.EqualTo(first.Iterations));
            Assert.That(second.Submersion.Volume, Is.EqualTo(first.Submersion.Volume));
            Assert.That(
                Math.Abs(first.VolumeError),
                Is.LessThanOrEqualTo(0.001d));

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void OpenSourceMesh_IsRejectedBeforeBake()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up,
                },
                triangles = new[] { 0, 1, 2 },
            };

            var valid = FloodVolumeBaker.TryValidateClosedMesh(
                mesh,
                out var message);

            Assert.That(valid, Is.False);
            Assert.That(message, Does.Contain("closed"));
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ClosedMesh_WithDuplicatedSeamVertices_IsAccepted()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            var valid = FloodVolumeBaker.TryValidateClosedMesh(
                mesh,
                out var message);

            Assert.That(valid, Is.True, message);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Authoring_DetectsStaleFingerprint()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var volume = gameObject.AddComponent<FloodVolume>();
            var authoring = gameObject.AddComponent<FloodVolumeAuthoring>();
            var data = CreateFilledData(Vector3Int.one);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("targetVolume").objectReferenceValue =
                volume;
            serialized.FindProperty("sourceMeshFilter").objectReferenceValue =
                gameObject.GetComponent<MeshFilter>();
            serialized.FindProperty("bakedData").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var hasStatus = FloodVolumeBaker.TryGetStatus(
                authoring,
                out var stale,
                out var message);

            Assert.That(hasStatus, Is.True);
            Assert.That(stale, Is.True);
            Assert.That(message, Does.Contain("stale"));

            UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private static FloodVolumeData CreateFilledData(Vector3Int gridSize)
        {
            var count = gridSize.x * gridSize.y * gridSize.z;
            var occupied = new int[count];
            for (var index = 0; index < count; index++)
                occupied[index] = index;

            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            data.Initialize(
                new Bounds(Vector3.zero, (Vector3)gridSize),
                Vector3.one,
                gridSize,
                occupied,
                newBoundaryCellCount: 0,
                newSourceFingerprint: "test");
            return data;
        }

        private static (Vector3[] vertices, int[] triangles) CreateUnitBoxBoundary()
        {
            var vertices = new[]
            {
                new Vector3(-1f, -1f, -1f),
                new Vector3(1f, -1f, -1f),
                new Vector3(1f, 1f, -1f),
                new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, -1f, 1f),
                new Vector3(1f, -1f, 1f),
                new Vector3(1f, 1f, 1f),
                new Vector3(-1f, 1f, 1f),
            };
            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
            };
            return (vertices, triangles);
        }
    }
}
