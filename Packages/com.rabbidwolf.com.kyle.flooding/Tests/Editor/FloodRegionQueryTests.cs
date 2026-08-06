using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodRegionQueryTests
    {
        [Test]
        public void QueryPoint_LazilyInitializesCompositeGeometry()
        {
            var root = new GameObject("LazyRegionQuery");
            try
            {
                root.AddComponent<FloodSimulationManager>();
                var region = root.AddComponent<FloodRegion>();
                var roomA = CreateChildVolume(
                    root.transform,
                    "RoomA",
                    new Vector3(-0.5f, 0f, 0f),
                    2f,
                    2f,
                    2f);
                var roomB = CreateChildVolume(
                    root.transform,
                    "RoomB",
                    new Vector3(0.5f, 0f, 0f),
                    2f,
                    2f,
                    2f);

                region.SetMembers(new List<FloodVolume> { roomA, roomB });
                region.ConfigureInitialVolume(6f);
                Assert.That(region.Rebuild(), Is.True);
                Assert.That(region.Geometry, Is.Not.Null);

                region.ClearRuntimeStateForTests();
                Assert.That(region.Geometry, Is.Null);

                var pointInA = new Vector3(-1f, 0.25f, 0f);
                var pointInB = new Vector3(1f, 0.25f, 0f);

                var queryA = region.QueryPoint(pointInA);
                Assert.That(region.Geometry, Is.Not.Null);
                Assert.That(queryA.IsInsideVolume, Is.True);
                Assert.That(queryA.IsSubmerged, Is.True);

                var queryB = region.QueryPoint(pointInB);
                Assert.That(queryB.IsInsideVolume, Is.True);
                Assert.That(queryB.IsSubmerged, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QueryPoint_AfterFailedInit_DoesNotThrowAndReportsOutside()
        {
            var root = new GameObject("FailedRegionQuery");
            try
            {
                root.SetActive(false);
                root.AddComponent<FloodSimulationManager>();
                var region = root.AddComponent<FloodRegion>();
                var roomA = CreateChildVolume(
                    root.transform,
                    "RoomA",
                    new Vector3(-5f, 0f, 0f),
                    2f,
                    2f,
                    2f);
                var roomB = CreateChildVolume(
                    root.transform,
                    "RoomB",
                    new Vector3(5f, 0f, 0f),
                    2f,
                    2f,
                    2f);

                region.SetMembers(new List<FloodVolume> { roomA, roomB });

                LogAssert.Expect(
                    LogType.Error,
                    new System.Text.RegularExpressions.Regex(
                        "geometry failed|disconnected"));

                root.SetActive(true);

                Assert.That(region.Geometry, Is.Null);

                var result = region.QueryPoint(roomA.transform.position);
                Assert.That(result.IsInsideVolume, Is.False);
                Assert.That(result.IsSubmerged, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static FloodVolume CreateChildVolume(
            Transform parent,
            string name,
            Vector3 localPosition,
            float width,
            float length,
            float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            var volume = go.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(width, length, height);
            volume.ConfigureFluidDensity(1000f);
            return volume;
        }
    }
}
