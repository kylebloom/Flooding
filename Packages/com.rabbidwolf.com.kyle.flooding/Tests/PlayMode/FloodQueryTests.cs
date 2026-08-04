using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodQueryTests
    {
        [UnityTest]
        public IEnumerator QueryPoint_ReportsSubmersionInsideRectangularVolume()
        {
            var gameObject = new GameObject("FloodQuery rectangular");
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.ConfigureRectangularGeometry(4f, 4f, 2f);
            floodVolume.AddWater(8f); // fill height 0.5 m

            var submerged = floodVolume.QueryPoint(new Vector3(0f, 0.25f, 0f));
            var aboveSurface = floodVolume.QueryPoint(new Vector3(0f, 1f, 0f));
            var outside = floodVolume.QueryPoint(new Vector3(3f, 0.1f, 0f));

            Assert.That(submerged.IsInsideVolume, Is.True);
            Assert.That(submerged.IsSubmerged, Is.True);
            Assert.That(
                submerged.SubmersionDepthMeters,
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                submerged.SurfacePoint.y,
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                submerged.SurfaceNormal,
                Is.EqualTo(Vector3.up).Using(
                    new Vector3EqualityComparer(0.0001f)));

            Assert.That(aboveSurface.IsInsideVolume, Is.True);
            Assert.That(aboveSurface.IsSubmerged, Is.False);
            Assert.That(aboveSurface.SubmersionDepthMeters, Is.Zero);

            Assert.That(outside.IsInsideVolume, Is.False);
            Assert.That(outside.IsSubmerged, Is.False);
            Assert.That(outside.SubmersionDepthMeters, Is.Zero);
            Assert.That(
                floodVolume.ContainsPoint(new Vector3(0f, 0.25f, 0f)),
                Is.True);
            Assert.That(
                floodVolume.IsPointSubmerged(new Vector3(0f, 0.25f, 0f)),
                Is.True);
            Assert.That(
                floodVolume.IsPointSubmerged(new Vector3(3f, 0.1f, 0f)),
                Is.False);

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator QueryPoint_OutsideVolumeBelowPlane_IsNotSubmerged()
        {
            var gameObject = new GameObject("FloodQuery outside below plane");
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.ConfigureRectangularGeometry(2f, 2f, 2f);
            floodVolume.AddWater(2f); // fill height 0.5 m

            var result = floodVolume.QueryPoint(new Vector3(5f, 0.1f, 0f));

            Assert.That(result.IsInsideVolume, Is.False);
            Assert.That(result.IsSubmerged, Is.False);
            Assert.That(result.SubmersionDepthMeters, Is.Zero);
            Assert.That(
                result.SurfacePoint.y,
                Is.EqualTo(0.5f).Within(0.0001f));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator QueryPoint_RespectsWorldTransform()
        {
            var gameObject = new GameObject("FloodQuery transform");
            gameObject.transform.position = new Vector3(10f, 5f, -2f);
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.ConfigureRectangularGeometry(2f, 2f, 2f);
            floodVolume.AddWater(2f); // local fill height 0.5 m

            var localSample = new Vector3(0f, 0.25f, 0f);
            var worldSample = gameObject.transform.TransformPoint(localSample);
            var result = floodVolume.QueryPoint(worldSample);

            Assert.That(result.IsInsideVolume, Is.True);
            Assert.That(result.IsSubmerged, Is.True);
            Assert.That(
                result.SubmersionDepthMeters,
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                result.SurfacePoint.y,
                Is.EqualTo(5.5f).Within(0.0001f));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator QueryPoint_UsesTiltedSurfacePlaneDepth()
        {
            var root = new GameObject("FloodQuery tilted root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -1f, -1f).normalized * 9.81f;

            var gameObject = new GameObject("FloodQuery tilted volume");
            gameObject.transform.SetParent(root.transform, false);
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.ConfigureRectangularGeometry(4f, 4f, 2f);
            floodVolume.AddWater(8f);
            manager.SimulateTick(0.02d);

            var surfacePlane = floodVolume.SurfacePlane;
            Assert.That(
                Vector3.Dot(surfacePlane.normal.normalized, Vector3.up),
                Is.LessThan(0.999f));

            var below = floodVolume.WaterCenterOfMassWorld;
            var result = floodVolume.QueryPoint(below);

            Assert.That(result.IsInsideVolume, Is.True);
            Assert.That(result.IsSubmerged, Is.True);
            Assert.That(result.SubmersionDepthMeters, Is.GreaterThan(0f));
            Assert.That(
                result.SurfaceNormal.normalized,
                Is.EqualTo(surfacePlane.normal.normalized)
                    .Using(new Vector3EqualityComparer(0.0001f)));

            Object.Destroy(root);
            yield return null;
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            private readonly float tolerance;

            public Vector3EqualityComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(Vector3 x, Vector3 y)
            {
                return (x - y).sqrMagnitude
                    <= tolerance * tolerance;
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
