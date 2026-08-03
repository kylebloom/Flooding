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
