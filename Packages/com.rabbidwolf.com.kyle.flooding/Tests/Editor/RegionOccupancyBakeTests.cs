using System.Collections.Generic;
using Kyle.Flooding.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class RegionOccupancyBakeTests
    {
        [Test]
        public void ThreeOverlappingMembers_CountOverlapCellsOnce()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-1f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            // Exact IE union is 16 m³; occupancy bake is approximate.
            Assert.That(fixture.Data.Capacity, Is.LessThan(24d));
            Assert.That(fixture.Data.Capacity, Is.GreaterThan(14d));
            Assert.That(fixture.Data.Capacity, Is.EqualTo(16d).Within(2d));
        }

        [Test]
        public void FaceTouchingMembers_SumCapacityApproximately()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-1f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.2f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            Assert.That(fixture.Data.Capacity, Is.EqualTo(16d).Within(2d));
        }

        [Test]
        public void PartialFill_DoesNotDoubleCountOverlap()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);

            var geometry = new BakedFloodGeometry(fixture.Data);
            var plane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
            var filled = geometry.CalculateSubmergedVolume(plane);

            Assert.That(filled, Is.EqualTo(geometry.Capacity * 0.5d).Within(1.5d));
            Assert.That(filled, Is.LessThan(geometry.Capacity));
        }

        [Test]
        public void DisconnectedMembers_FailBake()
        {
            var regionRoot = new GameObject("DisconnectedRegion");
            regionRoot.SetActive(false);
            var region = regionRoot.AddComponent<FloodRegion>();
            var a = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(-3f, 0f, 0f),
                2f,
                2f,
                2f);
            var b = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(3f, 0f, 0f),
                2f,
                2f,
                2f);
            region.SetMembers(new List<FloodVolume> { a, b });
            region.ConfigureBakeSettings(0.25f, 1000000);

            var baked = FloodRegionBaker.TryBake(
                region,
                out _,
                out var message,
                promptForAssetPath: false);

            Assert.That(baked, Is.False);
            Assert.That(message, Does.Contain("disconnected").IgnoreCase);

            Object.DestroyImmediate(regionRoot);
        }

        [Test]
        public void MixedModeBake_RectangularPlusBakedMember_Unions()
        {
            var regionRoot = new GameObject("MixedRegion");
            regionRoot.SetActive(false);
            var region = regionRoot.AddComponent<FloodRegion>();

            var rectangular = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(-0.5f, 0f, 0f),
                2f,
                2f,
                2f);

            var bakedMember = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(0.5f, 0f, 0f),
                2f,
                2f,
                2f);
            // Match rectangular prism frame: XZ centered, Y from 0..2.
            var volumeData = CreateFilledVolumeData(
                new Vector3Int(4, 4, 4),
                new Bounds(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 2f)));
            bakedMember.ConfigureBakedGeometry(volumeData);

            region.SetMembers(
                new List<FloodVolume> { rectangular, bakedMember });
            region.ConfigureBakeSettings(0.25f, 1000000);

            var baked = FloodRegionBaker.TryBake(
                region,
                out var data,
                out var message,
                promptForAssetPath: false);

            Assert.That(baked, Is.True, message);
            Assert.That(data.IsUsable, Is.True);
            Assert.That(data.SampleCount, Is.GreaterThan(0));
            Assert.That(data.Capacity, Is.GreaterThan(volumeData.Capacity));

            Object.DestroyImmediate(volumeData);
            Object.DestroyImmediate(regionRoot);
        }

        [Test]
        public void CompositeFloodGeometry_PrefersUsableBakeOverTwoBox()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            fixture.Region.AssignBake(fixture.Data);

            var created = CompositeFloodGeometry.TryCreate(
                fixture.Region,
                fixture.Region.Members,
                out var geometry,
                out var message);

            Assert.That(created, Is.True, message);
            Assert.That(geometry, Is.TypeOf<BakedFloodGeometry>());
            Assert.That(
                geometry.ContainmentPrecision,
                Is.EqualTo(FloodContainmentPrecision.BakeApproximation));
        }

        [Test]
        public void TwoBoxAnalyticPath_StillWorksWithoutBake()
        {
            var regionRoot = new GameObject("TwoBoxRegion");
            regionRoot.SetActive(false);
            var region = regionRoot.AddComponent<FloodRegion>();
            var a = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(-0.5f, 0f, 0f),
                2f,
                2f,
                2f);
            var b = CreateRectangularMember(
                regionRoot.transform,
                new Vector3(0.5f, 0f, 0f),
                2f,
                2f,
                2f);
            region.SetMembers(new List<FloodVolume> { a, b });

            var created = CompositeFloodGeometry.TryCreate(
                region,
                region.Members,
                out var geometry,
                out var message);

            Assert.That(created, Is.True, message);
            Assert.That(geometry.Capacity, Is.EqualTo(12d).Within(1e-6d));
            Assert.That(
                geometry.ContainmentPrecision,
                Is.EqualTo(FloodContainmentPrecision.Exact));

            Object.DestroyImmediate(regionRoot);
        }

        [Test]
        public void StaleFingerprint_DetectedWhenResolutionChanges()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            fixture.Region.AssignBake(fixture.Data);
            fixture.Region.ConfigureBakeSettings(0.1f, 1000000);

            Assert.That(
                FloodRegionBaker.TryGetStatus(
                    fixture.Region,
                    out var stale,
                    out _),
                Is.True);
            Assert.That(stale, Is.True);
        }

        [Test]
        public void FloodRegionData_DeduplicatesOccupiedIndices()
        {
            var data = ScriptableObject.CreateInstance<FloodRegionData>();
            data.Initialize(
                new Bounds(Vector3.zero, new Vector3(2f, 1f, 1f)),
                Vector3.one,
                new Vector3Int(2, 1, 1),
                new[] { 0, 0, 1, 1 },
                newBoundaryCellCount: 0,
                newSourceFingerprint: "dedupe");

            Assert.That(data.SampleCount, Is.EqualTo(2));
            Assert.That(data.OccupiedCellIndices[0], Is.EqualTo(0));
            Assert.That(data.OccupiedCellIndices[1], Is.EqualTo(1));
            Assert.That(data.Capacity, Is.EqualTo(2d));

            Object.DestroyImmediate(data);
        }

        [Test]
        public void BakeRegion_WritesFormat2OccupancyPresentationBoundary()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            Assert.That(fixture.Data.HasPresentationBoundary, Is.True);
            Assert.That(
                fixture.Data.FormatVersion,
                Is.EqualTo(FloodRegionData.CurrentFormatVersion));
            Assert.That(
                fixture.Data.PresentationBoundaryTriangleCount,
                Is.GreaterThan(0));
            Assert.That(
                fixture.BakeMessage,
                Does.Contain("Presentation boundary").IgnoreCase);
        }

        [Test]
        public void BakeRegion_PresentationBoundary_OmitsInternalSharedFaces()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-1f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.5f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);
            Assert.That(fixture.Data.HasPresentationBoundary, Is.True);

            // Two face-adjacent solid boxes: exterior faces only — no shared
            // mid-plane waterline when a horizontal plane cuts the union.
            var geometry = new BakedFloodGeometry(fixture.Data);
            var plane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(
                result.SurfaceIntersection.Contours.Count,
                Is.EqualTo(1));

            // Format-1 voxel fallback would emit many per-cell patches.
            var occupancyOnly = ScriptableObject.CreateInstance<FloodRegionData>();
            occupancyOnly.Initialize(
                fixture.Data.LocalBounds,
                fixture.Data.CellSize,
                fixture.Data.GridSize,
                CopyOccupied(fixture.Data),
                fixture.Data.BoundaryCellCount,
                "occupancy-only");
            var fallback = new BakedFloodGeometry(occupancyOnly)
                .EvaluateSubmersion(plane);
            Assert.That(occupancyOnly.HasPresentationBoundary, Is.False);
            Assert.That(
                fallback.SurfaceIntersection.Contours.Count,
                Is.GreaterThan(result.SurfaceIntersection.Contours.Count));

            Object.DestroyImmediate(occupancyOnly);
        }

        [Test]
        public void BakeRegion_LShape_ConcaveContourTriangulatesInsideFootprint()
        {
            // L made of three boxes: two along X, one extending in +Z from the
            // left box — produces a concave union footprint.
            var regionRoot = new GameObject("LRegion");
            regionRoot.SetActive(false);
            var region = regionRoot.AddComponent<FloodRegion>();
            var members = new List<FloodVolume>
            {
                CreateRectangularMember(
                    regionRoot.transform,
                    new Vector3(-1f, 0f, 0f),
                    2f,
                    2f,
                    2f),
                CreateRectangularMember(
                    regionRoot.transform,
                    new Vector3(1f, 0f, 0f),
                    2f,
                    2f,
                    2f),
                CreateRectangularMember(
                    regionRoot.transform,
                    new Vector3(-1f, 0f, 2f),
                    2f,
                    2f,
                    2f),
            };
            region.SetMembers(members);
            region.ConfigureBakeSettings(0.25f, 1000000);

            Assert.That(
                FloodRegionBaker.TryBake(
                    region,
                    out var data,
                    out var message,
                    promptForAssetPath: false),
                Is.True,
                message);
            Assert.That(data.HasPresentationBoundary, Is.True);

            var geometry = new BakedFloodGeometry(data);
            var plane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(
                result.SurfaceIntersection.Contours.Count,
                Is.EqualTo(1));
            Assert.That(
                result.SurfaceIntersection.Contours[0].Vertices.Count,
                Is.GreaterThan(4));

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            FloodPlanarPolygonTriangulation.AppendContour(
                result.SurfaceIntersection.Contours[0].Vertices,
                Vector3.up,
                vertices,
                triangles);

            Assert.That(triangles.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(
                AllTriangleCentroidsNearUnion(vertices, triangles, data),
                Is.True);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(regionRoot);
        }

        [Test]
        public void BakeRegion_CapacityUnchangedByPresentationBoundary()
        {
            using var fixture = new RegionBakeFixture(
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                null,
                width: 2f,
                length: 2f,
                height: 2f,
                cellResolution: 0.25f);

            Assert.That(fixture.BakeSucceeded, Is.True, fixture.BakeMessage);

            var withoutBoundary =
                ScriptableObject.CreateInstance<FloodRegionData>();
            withoutBoundary.Initialize(
                fixture.Data.LocalBounds,
                fixture.Data.CellSize,
                fixture.Data.GridSize,
                CopyOccupied(fixture.Data),
                fixture.Data.BoundaryCellCount,
                "no-boundary");

            Assert.That(
                fixture.Data.Capacity,
                Is.EqualTo(withoutBoundary.Capacity));
            Assert.That(
                new BakedFloodGeometry(fixture.Data).Capacity,
                Is.EqualTo(new BakedFloodGeometry(withoutBoundary).Capacity));

            Object.DestroyImmediate(withoutBoundary);
        }

        private static int[] CopyOccupied(FloodRegionData data)
        {
            var copy = new int[data.OccupiedCellIndices.Count];
            for (var index = 0; index < copy.Length; index++)
                copy[index] = data.OccupiedCellIndices[index];
            return copy;
        }

        private static bool AllTriangleCentroidsNearUnion(
            List<Vector3> vertices,
            List<int> triangles,
            FloodRegionData data)
        {
            var geometry = new BakedFloodGeometry(data);
            var pad = Mathf.Max(
                data.CellSize.x,
                data.CellSize.y,
                data.CellSize.z) * 0.75f;

            for (var index = 0; index < triangles.Count; index += 3)
            {
                var centroid =
                    (vertices[triangles[index]]
                        + vertices[triangles[index + 1]]
                        + vertices[triangles[index + 2]])
                    / 3f;
                if (!geometry.ContainsLocalPoint(centroid)
                    && !IsWithinOccupiedCellPad(data, centroid, pad))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWithinOccupiedCellPad(
            FloodRegionData data,
            Vector3 point,
            float pad)
        {
            foreach (var flattened in data.OccupiedCellIndices)
            {
                var center = data.GetCellCenter(flattened);
                var half = data.CellSize * 0.5f;
                var min = center - half - Vector3.one * pad;
                var max = center + half + Vector3.one * pad;
                if (point.x >= min.x
                    && point.x <= max.x
                    && point.y >= min.y
                    && point.y <= max.y
                    && point.z >= min.z
                    && point.z <= max.z)
                {
                    return true;
                }
            }

            return false;
        }

        private static FloodVolume CreateRectangularMember(
            Transform region,
            Vector3 localPosition,
            float width,
            float length,
            float height)
        {
            var go = new GameObject("Member");
            go.transform.SetParent(region, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var volume = go.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(width, length, height);
            return volume;
        }

        private static FloodVolumeData CreateFilledVolumeData(
            Vector3Int gridSize,
            Bounds localBounds)
        {
            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
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
                newSourceFingerprint: "mixed-mode-member");
            return data;
        }

        private sealed class RegionBakeFixture : System.IDisposable
        {
            public FloodRegion Region { get; }

            public FloodRegionData Data { get; }

            public bool BakeSucceeded { get; }

            public string BakeMessage { get; }

            private readonly GameObject regionRoot;
            private readonly FloodRegionData ownedData;

            public RegionBakeFixture(
                Vector3 positionA,
                Vector3 positionB,
                Vector3? positionC,
                float width,
                float length,
                float height,
                float cellResolution)
            {
                // Keep inactive so SetMembers does not Rebuild/log before bake.
                regionRoot = new GameObject("BakeRegion");
                regionRoot.SetActive(false);
                Region = regionRoot.AddComponent<FloodRegion>();

                var members = new List<FloodVolume>
                {
                    CreateRectangularMember(
                        regionRoot.transform,
                        positionA,
                        width,
                        length,
                        height),
                    CreateRectangularMember(
                        regionRoot.transform,
                        positionB,
                        width,
                        length,
                        height),
                };

                if (positionC.HasValue)
                {
                    members.Add(
                        CreateRectangularMember(
                            regionRoot.transform,
                            positionC.Value,
                            width,
                            length,
                            height));
                }

                Region.SetMembers(members);
                Region.ConfigureBakeSettings(cellResolution, 1000000);

                BakeSucceeded = FloodRegionBaker.TryBake(
                    Region,
                    out var data,
                    out var message,
                    promptForAssetPath: false);
                BakeMessage = message;
                Data = data;
                ownedData = data;
                if (BakeSucceeded)
                    Region.AssignBake(data);
            }

            public void Dispose()
            {
                if (ownedData != null)
                    Object.DestroyImmediate(ownedData);
                Object.DestroyImmediate(regionRoot);
            }
        }
    }
}
