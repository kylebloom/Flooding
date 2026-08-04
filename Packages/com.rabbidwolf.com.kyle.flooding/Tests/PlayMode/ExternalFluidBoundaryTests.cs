using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class ExternalFluidBoundaryTests
    {
        private const float Tolerance = 0.0001f;

        [UnityTest]
        public IEnumerator ExteriorHigherHead_ProducesInflowIntoFiniteVolume()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1.5f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f);

            setup.Manager.SimulateTick(0.5d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.GreaterThan(0d));
            Assert.That(setup.Volume.CurrentVolume, Is.GreaterThan(0f));
            Assert.That(
                setup.Manager.LastTickMetrics.ExternalInflowVolume,
                Is.GreaterThan(0d));
            Assert.That(
                setup.Manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InteriorHigherHead_ProducesOutflowToExterior()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 0.1f,
                compartmentInitialVolume: 20f,
                openingY: 0.25f);

            setup.Manager.SimulateTick(0.5d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.LessThan(0d));
            Assert.That(setup.Volume.CurrentVolume, Is.LessThan(20f));
            Assert.That(
                setup.Manager.LastTickMetrics.ExternalOutflowVolume,
                Is.GreaterThan(0d));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EqualizedHeads_ApproachZeroFlow()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1f,
                compartmentInitialVolume: 0f,
                openingY: 0.1f,
                openingWidth: 2f);

            // Coarse fill toward the exterior waterline.
            for (var tick = 0; tick < 100; tick++)
                setup.Manager.SimulateTick(0.05d);

            Assert.That(
                setup.Volume.CurrentHeight,
                Is.EqualTo(1f).Within(0.05f));

            // Fine ticks let discrete orifice integration settle into the
            // pressure-head deadband instead of oscillating around it.
            for (var tick = 0; tick < 50; tick++)
                setup.Manager.SimulateTick(0.005d);

            Assert.That(
                System.Math.Abs(setup.Connection.PressureHeadDifference),
                Is.LessThan(0.01d));
            Assert.That(
                System.Math.Abs(setup.Connection.CurrentFlowRate),
                Is.LessThan(0.05d));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosedConnection_TransfersNothing()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1.5f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f);
            setup.Connection.IsOpen = false;

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledExternalBoundary_TransfersNothing()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1.5f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f);
            setup.Ocean.BoundaryEnabled = false;

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DensityMismatch_TransfersNothing()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1.5f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f);
            setup.Ocean.ConfigureDensity(1025f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(setup.Connection.ValidationMessage, Is.Not.Null.And.Not.Empty);

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpeningAboveBothSurfaces_TransfersNothing()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 0.2f,
                compartmentInitialVolume: 1f,
                openingY: 2.5f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(1f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestinationCapacity_LimitsExternalInflow()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 3f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f,
                openingWidth: 50f);

            var capacity = setup.Volume.MaximumVolume;
            setup.Volume.AddWater(capacity - 1f);

            setup.Manager.SimulateTick(1d);

            Assert.That(
                setup.Volume.CurrentVolume,
                Is.EqualTo(capacity).Within(Tolerance));
            Assert.That(setup.Connection.RequestedFlowRate, Is.GreaterThan(1d));
            Assert.That(
                setup.Connection.CurrentFlowRate,
                Is.EqualTo(1d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleBreaches_ScaleDestinationCapacityTogether()
        {
            var root = new GameObject("Multiple breach test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var oceanObject = new GameObject("Ocean");
            oceanObject.transform.SetParent(root.transform, false);
            // Keep the exterior above the nearly-full tank (~2.94 m) so both
            // breaches request inflow and exercise destination-capacity scaling.
            oceanObject.transform.position = new Vector3(0f, 3f, 0f);
            var ocean = oceanObject.AddComponent<ExternalFluidBoundary>();

            var volume = CreateVolume(root.transform, "Tank");
            volume.AddWater(volume.MaximumVolume - 1f);

            var first = CreateBreach(
                root.transform,
                "Breach A",
                ocean,
                volume,
                openingY: 0.2f,
                openingWidth: 40f);
            var second = CreateBreach(
                root.transform,
                "Breach B",
                ocean,
                volume,
                openingY: 0.2f,
                openingWidth: 40f);

            manager.SimulateTick(1d);

            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(volume.MaximumVolume).Within(Tolerance));
            Assert.That(
                first.CurrentFlowRate + second.CurrentFlowRate,
                Is.EqualTo(1d).Within(Tolerance));
            Assert.That(
                manager.LastTickMetrics.ExternalInflowVolume,
                Is.EqualTo(1d).Within(Tolerance));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SnapshotUsesTickStartExternalPlane()
        {
            var setup = CreateBreachSetup(
                oceanSurfaceY: 1.5f,
                compartmentInitialVolume: 0f,
                openingY: 0.25f,
                openingWidth: 2f);

            setup.Manager.TickCompleted += _ =>
            {
                setup.Ocean.transform.position = new Vector3(0f, 10f, 0f);
            };

            setup.Manager.SimulateTick(0.2d);
            var firstInflow = setup.Manager.LastTickMetrics.ExternalInflowVolume;

            setup.Manager.SimulateTick(0.2d);
            var secondInflow = setup.Manager.LastTickMetrics.ExternalInflowVolume;

            Assert.That(firstInflow, Is.GreaterThan(0d));
            Assert.That(secondInflow, Is.GreaterThan(firstInflow));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RaisingOceanSurface_IncreasesInflow()
        {
            var low = CreateBreachSetup(
                oceanSurfaceY: 0.6f,
                compartmentInitialVolume: 0f,
                openingY: 0.1f,
                openingWidth: 2f);
            var high = CreateBreachSetup(
                oceanSurfaceY: 1.8f,
                compartmentInitialVolume: 0f,
                openingY: 0.1f,
                openingWidth: 2f);

            low.Manager.SimulateTick(0.25d);
            high.Manager.SimulateTick(0.25d);

            Assert.That(
                high.Manager.LastTickMetrics.ExternalInflowVolume,
                Is.GreaterThan(low.Manager.LastTickMetrics.ExternalInflowVolume));

            Object.Destroy(low.Root);
            Object.Destroy(high.Root);
            yield return null;
        }

        private static BreachSetup CreateBreachSetup(
            float oceanSurfaceY,
            float compartmentInitialVolume,
            float openingY,
            float openingWidth = 1f)
        {
            var root = new GameObject("External boundary test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var oceanObject = new GameObject("Ocean");
            oceanObject.transform.SetParent(root.transform, false);
            oceanObject.transform.position = new Vector3(0f, oceanSurfaceY, 0f);
            var ocean = oceanObject.AddComponent<ExternalFluidBoundary>();

            var volume = CreateVolume(root.transform, "Compartment");
            if (compartmentInitialVolume > 0f)
                volume.AddWater(compartmentInitialVolume);

            var connection = CreateBreach(
                root.transform,
                "Breach",
                ocean,
                volume,
                openingY,
                openingWidth);

            return new BreachSetup(root, manager, ocean, volume, connection);
        }

        private static FloodVolume CreateVolume(Transform parent, string name)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(4f, 4f, 3f);
            return volume;
        }

        private static FloodConnection CreateBreach(
            Transform parent,
            string name,
            ExternalFluidBoundary ocean,
            FloodVolume volume,
            float openingY,
            float openingWidth)
        {
            var connectionObject = new GameObject(name);
            connectionObject.transform.SetParent(parent, false);
            connectionObject.transform.position = new Vector3(0f, openingY, 2f);

            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.SideA = ocean;
            connection.SideB = volume;
            connection.OpeningWidth = openingWidth;
            connection.OpeningHeight = 1f;
            connection.DischargeCoefficient = 1f;
            return connection;
        }

        private readonly struct BreachSetup
        {
            public BreachSetup(
                GameObject root,
                FloodSimulationManager manager,
                ExternalFluidBoundary ocean,
                FloodVolume volume,
                FloodConnection connection)
            {
                Root = root;
                Manager = manager;
                Ocean = ocean;
                Volume = volume;
                Connection = connection;
            }

            public GameObject Root { get; }

            public FloodSimulationManager Manager { get; }

            public ExternalFluidBoundary Ocean { get; }

            public FloodVolume Volume { get; }

            public FloodConnection Connection { get; }
        }
    }
}
