using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodRegionTests
    {
        [UnityTest]
        public IEnumerator OneMemberRegion_MatchesStandaloneVolumeBehavior()
        {
            var standaloneRoot = new GameObject("Standalone");
            var standaloneManager =
                standaloneRoot.AddComponent<FloodSimulationManager>();
            standaloneManager.SimulateAutomatically = false;
            var standalone = standaloneRoot.AddComponent<FloodVolume>();
            standalone.ConfigureRectangularGeometry(4f, 3f, 2f);
            standalone.ConfigureFluidDensity(1000f);

            // Force Awake path with initial volume via AddWater after enable.
            standalone.AddWater(6f);
            standaloneManager.SimulateTick(0.1d);

            var regionRoot = new GameObject("RegionRoot");
            regionRoot.SetActive(false);
            var regionManager =
                regionRoot.AddComponent<FloodSimulationManager>();
            regionManager.SimulateAutomatically = false;
            var region = regionRoot.AddComponent<FloodRegion>();
            var member = regionRoot.AddComponent<FloodVolume>();
            member.ConfigureRectangularGeometry(4f, 3f, 2f);
            member.ConfigureFluidDensity(1000f);
            region.SetMembers(new List<FloodVolume> { member });
            region.ConfigureInitialVolume(6f);
            regionRoot.SetActive(true);
            yield return null;

            Assert.That(member.IsRegionMember, Is.True);
            Assert.That(member.OwningRegion, Is.EqualTo(region));
            Assert.That(region.CurrentVolume, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(member.CurrentVolume, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(
                region.MaximumVolume,
                Is.EqualTo(standalone.MaximumVolume).Within(1e-4f));
            Assert.That(
                member.CurrentState.Volume,
                Is.EqualTo(standalone.CurrentState.Volume).Within(1e-4d));
            Assert.That(
                member.CurrentState.Height,
                Is.EqualTo(standalone.CurrentState.Height).Within(1e-4d));

            member.AddWater(2f);
            regionManager.SimulateTick(0.1d);
            standalone.AddWater(2f);
            standaloneManager.SimulateTick(0.1d);

            Assert.That(
                member.CurrentVolume,
                Is.EqualTo(standalone.CurrentVolume).Within(1e-4f));
            Assert.That(
                region.CurrentVolume,
                Is.EqualTo(standalone.CurrentVolume).Within(1e-4f));

            Object.Destroy(standaloneRoot);
            Object.Destroy(regionRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoMemberRegion_OwnsSharedVolumeAndPlane()
        {
            var root = new GameObject("TwoMemberRegion");
            root.SetActive(false);
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

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
            root.SetActive(true);
            yield return null;

            Assert.That(region.MaximumVolume, Is.EqualTo(12f).Within(1e-3f));
            Assert.That(region.CurrentVolume, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(roomA.CurrentVolume, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(roomB.CurrentVolume, Is.EqualTo(6f).Within(1e-3f));

            var planeA = roomA.SurfacePlane;
            var planeB = roomB.SurfacePlane;
            Assert.That(
                planeA.normal.normalized,
                Is.EqualTo(planeB.normal.normalized).Using(
                    new Vector3EqualityComparer(1e-4f)));
            Assert.That(
                planeA.distance,
                Is.EqualTo(planeB.distance).Within(1e-4f));

            Assert.That(
                roomA.ContainsPoint(new Vector3(-1f, 0.5f, 0f)),
                Is.True);
            Assert.That(
                roomA.ContainsPoint(new Vector3(1f, 0.5f, 0f)),
                Is.False);
            Assert.That(
                region.ContainsPoint(new Vector3(1f, 0.5f, 0f)),
                Is.True);

            var queryA = roomA.QueryPoint(new Vector3(-1f, 0.25f, 0f));
            Assert.That(queryA.IsInsideVolume, Is.True);
            Assert.That(queryA.IsSubmerged, Is.True);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoMemberRegion_QueryPoint_CoversBothMemberInteriors()
        {
            var root = new GameObject("TwoMemberQueryRegion");
            root.SetActive(false);
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

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
            root.SetActive(true);
            yield return null;

            var pointInA = new Vector3(-1f, 0.25f, 0f);
            var pointInB = new Vector3(1f, 0.25f, 0f);
            var pointOutside = new Vector3(0f, 0.25f, 3f);

            Assert.That(roomA.ContainsPoint(pointInA), Is.True);
            Assert.That(roomA.ContainsPoint(pointInB), Is.False);
            Assert.That(roomB.ContainsPoint(pointInB), Is.True);
            Assert.That(roomB.ContainsPoint(pointInA), Is.False);

            var regionQueryA = region.QueryPoint(pointInA);
            var regionQueryB = region.QueryPoint(pointInB);
            var regionQueryOutside = region.QueryPoint(pointOutside);

            Assert.That(regionQueryA.IsInsideVolume, Is.True);
            Assert.That(regionQueryA.IsSubmerged, Is.True);
            Assert.That(regionQueryB.IsInsideVolume, Is.True);
            Assert.That(regionQueryB.IsSubmerged, Is.True);
            Assert.That(regionQueryOutside.IsInsideVolume, Is.False);
            Assert.That(regionQueryOutside.IsSubmerged, Is.False);

            Assert.That(
                regionQueryA.SubmersionDepthMeters,
                Is.EqualTo(regionQueryB.SubmersionDepthMeters).Within(1e-4f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameRegionConnection_IsAuthoringError()
        {
            var root = new GameObject("SameRegionConnection");
            root.SetActive(false);
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
            root.SetActive(true);
            yield return null;

            var connectionGo = new GameObject("WatertightDoor");
            connectionGo.transform.SetParent(root.transform, false);
            var connection = connectionGo.AddComponent<FloodConnection>();
            connection.VolumeA = roomA;
            connection.VolumeB = roomB;

            Assert.That(connection.TryValidateEndpoints(out var message), Is.False);
            Assert.That(message, Does.Contain("FloodRegion"));
            Assert.That(message, Does.Contain("independently simulated"));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TiltedRegion_KeepsGravityAlignedSurface()
        {
            var root = new GameObject("TiltedRegion");
            root.SetActive(false);
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);

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
            root.SetActive(true);
            yield return null;

            root.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
            manager.SimulateTick(0.1d);

            var plane = region.SurfacePlane;
            Assert.That(
                Vector3.Dot(plane.normal.normalized, Vector3.up),
                Is.GreaterThan(0.99f));
            Assert.That(region.CurrentVolume, Is.EqualTo(6f).Within(1e-3f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OccupancyBake_TiltedRegion_KeepsGravityAlignedSurface()
        {
            var root = new GameObject("TiltedOccupancyRegion");
            root.SetActive(false);
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);

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
            var roomC = CreateChildVolume(
                root.transform,
                "RoomC",
                new Vector3(1.5f, 0f, 0f),
                2f,
                2f,
                2f);

            var bake = CreateSimpleRegionOccupancy(
                new Bounds(new Vector3(0.5f, 0f, 0f), new Vector3(5f, 2f, 2f)),
                new Vector3Int(10, 4, 4));
            region.AssignBakedRegionData(bake);
            region.SetMembers(
                new List<FloodVolume> { roomA, roomB, roomC });
            region.ConfigureInitialVolume(8f);
            root.SetActive(true);
            yield return null;

            Assert.That(region.Geometry, Is.TypeOf<BakedFloodGeometry>());
            Assert.That(region.CurrentVolume, Is.EqualTo(8f).Within(1e-3f));

            root.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
            manager.SimulateTick(0.1d);

            var plane = region.SurfacePlane;
            Assert.That(
                Vector3.Dot(plane.normal.normalized, Vector3.up),
                Is.GreaterThan(0.99f));
            Assert.That(region.CurrentVolume, Is.EqualTo(8f).Within(1e-3f));

            Object.Destroy(bake);
            Object.Destroy(root);
            yield return null;
        }

        private static FloodRegionData CreateSimpleRegionOccupancy(
            Bounds localBounds,
            Vector3Int gridSize)
        {
            var data = ScriptableObject.CreateInstance<FloodRegionData>();
            var occupied = new int[gridSize.x * gridSize.y * gridSize.z];
            for (var index = 0; index < occupied.Length; index++)
                occupied[index] = index;

            var cellSize = new Vector3(
                localBounds.size.x / gridSize.x,
                localBounds.size.y / gridSize.y,
                localBounds.size.z / gridSize.z);
            data.Initialize(
                localBounds,
                cellSize,
                gridSize,
                occupied,
                newBoundaryCellCount: 0,
                newSourceFingerprint: "playmode-occupancy");
            return data;
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

        private sealed class Vector3EqualityComparer :
            System.Collections.Generic.IEqualityComparer<Vector3>
        {
            private readonly float tolerance;

            public Vector3EqualityComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(Vector3 x, Vector3 y)
            {
                return (x - y).sqrMagnitude <= tolerance * tolerance;
            }

            public int GetHashCode(Vector3 obj)
            {
                return 0;
            }
        }
    }
}
