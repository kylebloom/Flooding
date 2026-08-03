using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodVolumeTests
    {
        [UnityTest]
        public IEnumerator VolumeMutations_ArePublishedOncePerFrame()
        {
            var gameObject = new GameObject("FloodVolume test");
            var manager = gameObject.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            var stateEventCount = 0;
            var volumeEventCount = 0;
            var heightEventCount = 0;
            FloodState publishedState = default;

            floodVolume.StateChanged += state =>
            {
                stateEventCount++;
                publishedState = state;
            };
            floodVolume.VolumeChanged += _ => volumeEventCount++;
            floodVolume.WaterHeightChanged += _ => heightEventCount++;

            floodVolume.AddWater(2f);
            floodVolume.AddWater(3f);

            manager.SimulateTick(0.1d);

            Assert.That(stateEventCount, Is.EqualTo(1));
            Assert.That(volumeEventCount, Is.EqualTo(1));
            Assert.That(heightEventCount, Is.EqualTo(1));
            Assert.That(publishedState.Volume, Is.EqualTo(5d).Within(0.000001d));
            Assert.That(publishedState.Height, Is.EqualTo(0.2d).Within(0.000001d));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TransformChange_PublishesStateWithoutVolumeEvent()
        {
            var gameObject = new GameObject("FloodVolume transform test");
            var manager = gameObject.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            var stateEventCount = 0;
            var volumeEventCount = 0;

            floodVolume.StateChanged += _ => stateEventCount++;
            floodVolume.VolumeChanged += _ => volumeEventCount++;

            gameObject.transform.position = new Vector3(3f, 2f, 1f);

            manager.SimulateTick(0.1d);

            Assert.That(stateEventCount, Is.EqualTo(1));
            Assert.That(volumeEventCount, Is.Zero);
            Assert.That(
                floodVolume.CurrentState.WaterCenterOfMassWorld,
                Is.EqualTo(gameObject.transform.position));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CurrentState_ContainsDerivedRectangularValues()
        {
            var gameObject = new GameObject("FloodVolume state test");
            var floodVolume = gameObject.AddComponent<FloodVolume>();

            floodVolume.AddWater(10f);

            var state = floodVolume.CurrentState;

            Assert.That(state.Volume, Is.EqualTo(10d).Within(0.000001d));
            Assert.That(state.Capacity, Is.EqualTo(75d).Within(0.000001d));
            Assert.That(state.Height, Is.EqualTo(0.4d).Within(0.000001d));
            Assert.That(
                state.FillPercentage,
                Is.EqualTo(10d / 75d).Within(0.000001d));
            Assert.That(state.WaterMass, Is.EqualTo(10000d).Within(0.001d));
            Assert.That(state.IsEmpty, Is.False);
            Assert.That(state.IsFull, Is.False);
            Assert.That(
                state.SurfacePlane.GetDistanceToPoint(
                    new Vector3(0f, 0.4f, 0f)),
                Is.Zero.Within(0.000001f));
            Assert.That(
                state.WaterCenterOfMassWorld.x,
                Is.Zero.Within(0.000005f));
            Assert.That(
                state.WaterCenterOfMassWorld.y,
                Is.EqualTo(0.2f).Within(0.000005f));
            Assert.That(
                state.WaterCenterOfMassWorld.z,
                Is.Zero.Within(0.000005f));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfiguredDensity_ScalesWaterMass()
        {
            var gameObject = new GameObject("FloodVolume density test");
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.ConfigureFluidDensity(1025f);
            floodVolume.AddWater(4f);

            Assert.That(floodVolume.WaterDensity, Is.EqualTo(1025f));
            Assert.That(
                floodVolume.CurrentState.WaterMass,
                Is.EqualTo(4100d).Within(0.001d));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PolygonGeometry_DrivesCapacityHeightAndCentroid()
        {
            var gameObject = new GameObject("Polygon FloodVolume test");
            var floodVolume = gameObject.AddComponent<FloodVolume>();
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

            floodVolume.AddWater(6f);
            var state = floodVolume.CurrentState;

            Assert.That(state.Capacity, Is.EqualTo(12d).Within(0.000001d));
            Assert.That(state.Height, Is.EqualTo(2d).Within(0.000001d));
            Assert.That(
                state.WaterCenterOfMassWorld.x,
                Is.EqualTo(5f / 6f).Within(0.000001f));
            Assert.That(
                state.WaterCenterOfMassWorld.y,
                Is.EqualTo(1f).Within(0.000001f));
            Assert.That(
                state.WaterCenterOfMassWorld.z,
                Is.EqualTo(5f / 6f).Within(0.000001f));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BakedGeometry_DrivesCapacityAndArbitraryPlaneState()
        {
            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            data.Initialize(
                new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f)),
                Vector3.one,
                new Vector3Int(2, 2, 2),
                new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                newBoundaryCellCount: 8,
                newSourceFingerprint: "play-mode-test");
            var gameObject = new GameObject("Baked FloodVolume test");
            var floodVolume = gameObject.AddComponent<FloodVolume>();

            floodVolume.ConfigureBakedGeometry(data);
            floodVolume.AddWater(4f);

            Assert.That(
                floodVolume.GeometryMode,
                Is.EqualTo(FloodGeometryMode.BakedData));
            Assert.That(floodVolume.MaximumVolume, Is.EqualTo(8f));
            Assert.That(floodVolume.CurrentHeight, Is.EqualTo(1f));
            Assert.That(
                floodVolume.WaterCenterOfMassWorld.y,
                Is.EqualTo(-0.5f).Within(0.00001f));

            Object.Destroy(gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RotatedVolume_SurfaceRemainsPerpendicularToGravity()
        {
            var gameObject = new GameObject("Rotated FloodVolume test");
            var manager = gameObject.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.AddWater(20f);
            var originalVolume = floodVolume.CurrentVolume;

            gameObject.transform.rotation =
                Quaternion.Euler(25f, 15f, 35f);
            manager.SimulateTick(0.1d);

            Assert.That(
                floodVolume.CurrentVolume,
                Is.EqualTo(originalVolume).Within(0.000001f));
            Assert.That(
                Vector3.Dot(
                    floodVolume.SurfacePlane.normal,
                    Vector3.up),
                Is.GreaterThan(0.99999f));
            Assert.That(
                System.Math.Abs(floodVolume.SurfaceVolumeError),
                Is.LessThanOrEqualTo(0.000075d));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CustomGravity_ControlsWorldSurfaceNormal()
        {
            var gameObject = new GameObject("Custom gravity test");
            var manager = gameObject.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(4f, -6f, 2f);
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.AddWater(15f);

            manager.SimulateTick(0.1d);

            Assert.That(
                Vector3.Dot(
                    floodVolume.SurfacePlane.normal,
                    -manager.CustomGravity.normalized),
                Is.GreaterThan(0.99999f));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZeroGravity_RetainsLastValidLocalOrientation()
        {
            var gameObject = new GameObject("Zero gravity fallback test");
            var manager = gameObject.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);
            var floodVolume = gameObject.AddComponent<FloodVolume>();
            floodVolume.AddWater(15f);
            manager.SimulateTick(0.1d);
            var previousLocalNormal =
                floodVolume.LocalSurfacePlane.normal;

            manager.CustomGravity = Vector3.zero;
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            manager.SimulateTick(0.1d);

            Assert.That(
                Vector3.Dot(
                    floodVolume.LocalSurfacePlane.normal,
                    previousLocalNormal),
                Is.GreaterThan(0.99999f));

            Object.Destroy(gameObject);
            yield return null;
        }
    }
}
