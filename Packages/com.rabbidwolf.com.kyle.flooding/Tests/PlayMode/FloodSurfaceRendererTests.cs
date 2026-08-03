using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodSurfaceRendererTests
    {
        [UnityTest]
        public IEnumerator CubeRenderer_AppliesPublishedState()
        {
            var root = new GameObject("Flood renderer test");
            var visual = new GameObject("Water visual");
            visual.transform.SetParent(root.transform, false);

            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = root.AddComponent<FloodVolume>();
            var renderer = root.AddComponent<FloodCubeSurfaceRenderer>();
            renderer.InterpolationDuration = 0f;
            renderer.WaterVisual = visual.transform;

            floodVolume.AddWater(5f);
            manager.SimulateTick(0.1d);

            Assert.That(visual.activeSelf, Is.True);
            Assert.That(
                visual.transform.localScale,
                Is.EqualTo(new Vector3(5f, 0.2f, 5f)));
            Assert.That(
                visual.transform.localPosition,
                Is.EqualTo(new Vector3(0f, 0.1f, 0f)));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Renderer_DoesNotMutateSimulation()
        {
            var root = new GameObject("Presentation separation test");
            var visual = new GameObject("Water visual");
            visual.transform.SetParent(root.transform, false);

            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = root.AddComponent<FloodVolume>();
            var renderer = root.AddComponent<FloodCubeSurfaceRenderer>();
            renderer.InterpolationDuration = 0.1f;
            renderer.WaterVisual = visual.transform;

            floodVolume.AddWater(7f);
            manager.SimulateTick(0.1d);
            var expectedVolume = floodVolume.CurrentVolume;

            yield return new WaitForSeconds(0.2f);

            Assert.That(
                floodVolume.CurrentVolume,
                Is.EqualTo(expectedVolume).Within(0.000001f));

            renderer.enabled = false;
            floodVolume.AddWater(3f);
            manager.SimulateTick(0.1d);

            yield return null;

            Assert.That(
                floodVolume.CurrentVolume,
                Is.EqualTo(10f).Within(0.000001f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Renderer_InterpolatesAndCanSnapToCurrentState()
        {
            var root = new GameObject("Flood interpolation test");
            var visual = new GameObject("Water visual");
            visual.transform.SetParent(root.transform, false);

            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = root.AddComponent<FloodVolume>();
            var renderer = root.AddComponent<FloodCubeSurfaceRenderer>();
            renderer.InterpolationDuration = 10f;
            renderer.WaterVisual = visual.transform;

            yield return null;

            floodVolume.AddWater(5f);
            manager.SimulateTick(0.1d);

            yield return new WaitForSeconds(0.05f);

            Assert.That(renderer.DisplayedState.Height, Is.GreaterThan(0d));
            Assert.That(
                renderer.DisplayedState.Height,
                Is.LessThan(floodVolume.CurrentState.Height));

            renderer.SnapToCurrentState();

            Assert.That(
                renderer.DisplayedState,
                Is.EqualTo(floodVolume.CurrentState));
            Assert.That(
                visual.transform.localScale.y,
                Is.EqualTo(floodVolume.CurrentHeight).Within(0.000001f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PolygonRenderer_GeneratesConcaveWaterMesh()
        {
            var root = new GameObject("Polygon renderer test");
            var visual = new GameObject("Polygon water visual");
            visual.transform.SetParent(root.transform, false);
            var meshFilter = visual.AddComponent<MeshFilter>();
            visual.AddComponent<MeshRenderer>();

            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = root.AddComponent<FloodVolume>();
            floodVolume.ConfigurePolygonGeometry(
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(2f, 0f),
                    new Vector2(2f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 2f),
                    new Vector2(0f, 2f),
                },
                4f);

            var renderer = root.AddComponent<FloodPolygonSurfaceRenderer>();
            renderer.InterpolationDuration = 0f;
            renderer.WaterMeshFilter = meshFilter;

            floodVolume.AddWater(1.5f);
            manager.SimulateTick(0.1d);

            Assert.That(meshFilter.sharedMesh, Is.Not.Null);
            Assert.That(meshFilter.sharedMesh.vertexCount, Is.GreaterThan(6));
            Assert.That(meshFilter.sharedMesh.triangles.Length, Is.GreaterThan(0));

            var maximumVertexHeight = 0f;

            foreach (var vertex in meshFilter.sharedMesh.vertices)
                maximumVertexHeight = Mathf.Max(maximumVertexHeight, vertex.y);

            Assert.That(
                maximumVertexHeight,
                Is.EqualTo(0.5f).Within(0.000001f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PolygonRenderer_ClipsMeshToGravityAlignedPlane()
        {
            var root = new GameObject("Tilted polygon renderer test");
            var visual = new GameObject("Tilted water visual");
            visual.transform.SetParent(root.transform, false);
            var meshFilter = visual.AddComponent<MeshFilter>();
            visual.AddComponent<MeshRenderer>();

            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);
            var floodVolume = root.AddComponent<FloodVolume>();
            var renderer = root.AddComponent<FloodPolygonSurfaceRenderer>();
            renderer.InterpolationDuration = 0f;
            renderer.WaterMeshFilter = meshFilter;
            floodVolume.AddWater(30f);

            root.transform.rotation = Quaternion.Euler(20f, 10f, 35f);
            manager.SimulateTick(0.1d);

            var state = floodVolume.CurrentState;
            var mesh = meshFilter.sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.triangles.Length, Is.GreaterThan(0));

            foreach (var vertex in mesh.vertices)
            {
                var worldVertex = root.transform.TransformPoint(vertex);
                Assert.That(
                    state.SurfacePlane.GetDistanceToPoint(worldVertex),
                    Is.LessThanOrEqualTo(0.00002f));
            }

            Object.Destroy(root);
            yield return null;
        }
    }
}
