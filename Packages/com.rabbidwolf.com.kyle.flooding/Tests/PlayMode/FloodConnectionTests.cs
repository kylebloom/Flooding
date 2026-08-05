using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodConnectionTests
    {
        private const float Tolerance = 0.0001f;

        [UnityTest]
        public IEnumerator EqualHeads_ProduceNoTransfer()
        {
            var setup = CreateSetup();
            setup.VolumeA.AddWater(5f);
            setup.VolumeB.AddWater(5f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.VolumeA.CurrentVolume, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(setup.VolumeB.CurrentVolume, Is.EqualTo(5f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreaterHeadOnA_TransfersVolumeAndConservesTotal()
        {
            var setup = CreateSetup();
            setup.VolumeA.AddWater(10f);
            var initialTotal =
                setup.VolumeA.CurrentVolume
                + setup.VolumeB.CurrentVolume;

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.GreaterThan(0d));
            Assert.That(setup.VolumeA.CurrentVolume, Is.LessThan(10f));
            Assert.That(setup.VolumeB.CurrentVolume, Is.GreaterThan(0f));
            Assert.That(
                setup.VolumeA.CurrentVolume + setup.VolumeB.CurrentVolume,
                Is.EqualTo(initialTotal).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreaterHeadOnB_ReversesFlow()
        {
            var setup = CreateSetup();
            setup.VolumeB.AddWater(10f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.LessThan(0d));
            Assert.That(setup.VolumeA.CurrentVolume, Is.GreaterThan(0f));
            Assert.That(setup.VolumeB.CurrentVolume, Is.LessThan(10f));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosedConnection_TransfersNothing()
        {
            var setup = CreateSetup();
            setup.VolumeA.AddWater(10f);
            setup.Connection.IsOpen = false;

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.VolumeA.CurrentVolume, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(setup.VolumeB.CurrentVolume, Is.Zero.Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenFractionOne_PreservesExistingRequestedFlow()
        {
            var baseline = CreateSetup();
            baseline.VolumeA.AddWater(10f);
            baseline.Manager.SimulateTick(1d);
            var baselineRequested = baseline.Connection.RequestedFlowRate;

            var withFraction = CreateSetup();
            withFraction.VolumeA.AddWater(10f);
            withFraction.Connection.OpenFraction = 1f;
            withFraction.Manager.SimulateTick(1d);

            Assert.That(
                withFraction.Connection.RequestedFlowRate,
                Is.EqualTo(baselineRequested).Within(Tolerance));

            Object.Destroy(baseline.Root);
            Object.Destroy(withFraction.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenFractionZero_TransfersNothingWhileIsOpenRemainsTrue()
        {
            var setup = CreateSetup();
            setup.VolumeA.AddWater(10f);
            setup.Connection.IsOpen = true;
            setup.Connection.OpenFraction = 0f;

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.IsOpen, Is.True);
            Assert.That(setup.Connection.RequestedFlowRate, Is.Zero);
            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Connection.SubmergedOpeningArea, Is.Zero);
            Assert.That(setup.VolumeA.CurrentVolume, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(setup.VolumeB.CurrentVolume, Is.Zero.Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenFractionHalf_HalvesRequestedFlowUnderIdenticalHeads()
        {
            var full = CreateSetup();
            full.VolumeA.AddWater(10f);
            full.Connection.OpenFraction = 1f;
            full.Manager.SimulateTick(1d);

            var half = CreateSetup();
            half.VolumeA.AddWater(10f);
            half.Connection.OpenFraction = 0.5f;
            half.Manager.SimulateTick(1d);

            Assert.That(full.Connection.RequestedFlowRate, Is.GreaterThan(0d));
            Assert.That(
                half.Connection.RequestedFlowRate,
                Is.EqualTo(full.Connection.RequestedFlowRate * 0.5d)
                    .Within(Tolerance));
            Assert.That(
                half.Connection.SubmergedOpeningArea,
                Is.EqualTo(full.Connection.SubmergedOpeningArea * 0.5d)
                    .Within(Tolerance));

            Object.Destroy(full.Root);
            Object.Destroy(half.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenFractionHalf_AppliesToReverseFlow()
        {
            var full = CreateSetup();
            full.VolumeB.AddWater(10f);
            full.Connection.OpenFraction = 1f;
            full.Manager.SimulateTick(1d);

            var half = CreateSetup();
            half.VolumeB.AddWater(10f);
            half.Connection.OpenFraction = 0.5f;
            half.Manager.SimulateTick(1d);

            Assert.That(full.Connection.RequestedFlowRate, Is.LessThan(0d));
            Assert.That(
                half.Connection.RequestedFlowRate,
                Is.EqualTo(full.Connection.RequestedFlowRate * 0.5d)
                    .Within(Tolerance));

            Object.Destroy(full.Root);
            Object.Destroy(half.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenFraction_DoesNotMutateAuthoredOpeningDimensions()
        {
            var setup = CreateSetup(openingWidth: 1.25f);
            setup.Connection.OpeningHeight = 1.75f;
            var authoredWidth = setup.Connection.OpeningWidth;
            var authoredHeight = setup.Connection.OpeningHeight;

            setup.Connection.OpenFraction = 0.25f;
            setup.VolumeA.AddWater(8f);
            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Connection.OpeningWidth, Is.EqualTo(authoredWidth));
            Assert.That(setup.Connection.OpeningHeight, Is.EqualTo(authoredHeight));
            Assert.That(
                setup.Connection.FullOpeningArea,
                Is.EqualTo(authoredWidth * authoredHeight).Within(Tolerance));
            Assert.That(
                setup.Connection.EffectiveOpeningArea,
                Is.EqualTo(authoredWidth * authoredHeight * 0.25f)
                    .Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [Test]
        public void OpenFraction_RejectsNonFiniteRuntimeValues()
        {
            var setup = CreateSetup();

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => setup.Connection.OpenFraction = float.NaN);
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => setup.Connection.OpenFraction = float.PositiveInfinity);
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => setup.Connection.OpenFraction = float.NegativeInfinity);
            Assert.That(setup.Connection.OpenFraction, Is.EqualTo(1f));

            Object.DestroyImmediate(setup.Root);
        }

        [UnityTest]
        public IEnumerator MultipleConnections_CannotOverdrawSource()
        {
            var root = new GameObject("Connection source reconciliation test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var source = CreateVolume(root.transform, "Source");
            var destinationA = CreateVolume(root.transform, "Destination A");
            var destinationB = CreateVolume(root.transform, "Destination B");
            source.AddWater(10f);

            CreateConnection(
                root.transform,
                source,
                destinationA,
                openingWidth: 100f);
            CreateConnection(
                root.transform,
                source,
                destinationB,
                openingWidth: 100f);

            manager.SimulateTick(1d);

            Assert.That(source.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(
                destinationA.CurrentVolume + destinationB.CurrentVolume,
                Is.EqualTo(10f).Within(Tolerance));
            Assert.That(
                source.CurrentVolume
                + destinationA.CurrentVolume
                + destinationB.CurrentVolume,
                Is.EqualTo(10f).Within(Tolerance));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestinationCapacity_LimitsAppliedFlow()
        {
            var setup = CreateSetup(openingWidth: 100f);
            setup.VolumeA.AddWater(setup.VolumeA.MaximumVolume);
            setup.VolumeB.AddWater(setup.VolumeB.MaximumVolume - 1f);

            setup.Manager.SimulateTick(1d);

            Assert.That(
                setup.VolumeB.CurrentVolume,
                Is.EqualTo(setup.VolumeB.MaximumVolume).Within(Tolerance));
            Assert.That(
                setup.VolumeA.CurrentVolume,
                Is.EqualTo(setup.VolumeA.MaximumVolume - 1f).Within(Tolerance));
            Assert.That(setup.Connection.RequestedFlowRate, Is.GreaterThan(1d));
            Assert.That(
                setup.Connection.CurrentFlowRate,
                Is.EqualTo(1d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        private static ConnectionSetup CreateSetup(float openingWidth = 1f)
        {
            var root = new GameObject("Flood connection test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volumeA = CreateVolume(root.transform, "Volume A");
            var volumeB = CreateVolume(root.transform, "Volume B");
            var connection = CreateConnection(
                root.transform,
                volumeA,
                volumeB,
                openingWidth);

            return new ConnectionSetup(
                root,
                manager,
                volumeA,
                volumeB,
                connection);
        }

        private static FloodVolume CreateVolume(
            Transform parent,
            string name)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            return volumeObject.AddComponent<FloodVolume>();
        }

        private static FloodConnection CreateConnection(
            Transform parent,
            FloodVolume volumeA,
            FloodVolume volumeB,
            float openingWidth)
        {
            var connectionObject = new GameObject("Flood connection");
            connectionObject.transform.SetParent(parent, false);

            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.VolumeA = volumeA;
            connection.VolumeB = volumeB;
            connection.OpeningWidth = openingWidth;
            connection.OpeningHeight = 1f;
            connection.DischargeCoefficient = 1f;

            return connection;
        }

        private readonly struct ConnectionSetup
        {
            public ConnectionSetup(
                GameObject root,
                FloodSimulationManager manager,
                FloodVolume volumeA,
                FloodVolume volumeB,
                FloodConnection connection)
            {
                Root = root;
                Manager = manager;
                VolumeA = volumeA;
                VolumeB = volumeB;
                Connection = connection;
            }

            public GameObject Root { get; }

            public FloodSimulationManager Manager { get; }

            public FloodVolume VolumeA { get; }

            public FloodVolume VolumeB { get; }

            public FloodConnection Connection { get; }
        }
    }
}
