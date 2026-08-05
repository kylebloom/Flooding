using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodSinkTests
    {
        private const float Tolerance = 0.0001f;

        [UnityTest]
        public IEnumerator ActiveSink_RemovesConfiguredAmountWhenSupplyAllows()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(10f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 2f);

            setup.Manager.SimulateTick(1d);

            Assert.That(sink.CurrentFlowRate, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.EqualTo(2d).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InactiveSink_RemovesNothing()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(5f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 3f);
            sink.IsActive = false;

            setup.Manager.SimulateTick(1d);

            Assert.That(sink.RequestedFlowRate, Is.Zero);
            Assert.That(sink.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(5f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZeroRateSink_RemovesNothing()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(5f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 0f);

            setup.Manager.SimulateTick(1d);

            Assert.That(sink.CurrentFlowRate, Is.Zero);
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(5f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DryTarget_ProducesZeroAppliedFlow()
        {
            var setup = CreateVolumeSetup();
            var sink = CreateSink(setup.Root.transform, setup.Volume, 4f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(sink.CurrentFlowRate, Is.Zero);
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.Zero.Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LimitedSupply_ScalesSinkAndNeverGoesNegative()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(1f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 5f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(sink.CurrentFlowRate, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.EqualTo(1d).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleSinks_ShareLimitedSupplyProportionally()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(3f);
            var sinkA = CreateSink(setup.Root.transform, setup.Volume, 2f);
            var sinkB = CreateSink(setup.Root.transform, setup.Volume, 4f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(sinkA.CurrentFlowRate, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(sinkB.CurrentFlowRate, Is.EqualTo(2f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FiniteConsumers_ShareSupplyWithExactProportions()
        {
            // available 1.0; requests 0.8 + 0.4 + 0.8 = 2.0 → scale 0.5
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(1f);
            var consumerA = CreateSink(setup.Root.transform, setup.Volume, 0.8f);
            var consumerB = CreateSink(setup.Root.transform, setup.Volume, 0.4f);
            var consumerC = CreateSink(setup.Root.transform, setup.Volume, 0.8f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Volume.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(consumerA.CurrentFlowRate, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(consumerB.CurrentFlowRate, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(consumerC.CurrentFlowRate, Is.EqualTo(0.4f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SinksAndConnectionOutflows_CompeteForSameSupply()
        {
            var root = new GameObject("Sink vs connection outflow test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeA = CreateVolume(root.transform, "A");
            var volumeB = CreateVolume(root.transform, "B");
            volumeA.AddWater(1f);

            // Huge opening so unconstrained connection demand exceeds supply.
            var connection = CreateConnection(
                root.transform,
                volumeA,
                volumeB,
                openingWidth: 50f);
            connection.DischargeCoefficient = 1f;
            connection.OpeningHeight = 2f;

            var sink = CreateSink(root.transform, volumeA, 100f);

            manager.SimulateTick(1d);

            var connectionOut =
                connection.CurrentFlowRate > 0d
                    ? connection.CurrentFlowRate
                    : 0d;
            var totalRemoved = connectionOut + sink.CurrentFlowRate;

            Assert.That(volumeA.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(totalRemoved, Is.EqualTo(1d).Within(0.001d));
            Assert.That(sink.CurrentFlowRate, Is.GreaterThan(0f));
            Assert.That(connection.CurrentFlowRate, Is.GreaterThan(0d));
            Assert.That(
                manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SourceAddition_DoesNotProvideSameTickSupplyToSink()
        {
            var setup = CreateVolumeSetup();
            // Empty volume: source +1 m³/s, sink -1 m³/s for 1 s.
            var source = CreateSource(setup.Root.transform, setup.Volume, 1f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 1f);

            setup.Manager.SimulateTick(1d);

            Assert.That(sink.CurrentFlowRate, Is.Zero);
            Assert.That(source.CurrentFlowRate, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSourceVolume,
                Is.EqualTo(1d).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.Zero.Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SinkRemoval_DoesNotFreeSameTickCapacityForSource()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(setup.Volume.MaximumVolume);
            var source = CreateSource(setup.Root.transform, setup.Volume, 1f);
            var sink = CreateSink(setup.Root.transform, setup.Volume, 1f);

            setup.Manager.SimulateTick(1d);

            Assert.That(source.CurrentFlowRate, Is.Zero);
            Assert.That(sink.CurrentFlowRate, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                setup.Volume.CurrentVolume,
                Is.EqualTo(setup.Volume.MaximumVolume - 1f).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSourceVolume,
                Is.Zero.Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.EqualTo(1d).Within(Tolerance));
            Assert.That(
                setup.Manager.LastTickMetrics.ConservationError,
                Is.EqualTo(0d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AggregatesMultipleSinksAndPublishesOnce()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(10f);
            var stateEventCount = 0;
            setup.Volume.StateChanged += _ => stateEventCount++;

            CreateSink(setup.Root.transform, setup.Volume, 1f);
            CreateSink(setup.Root.transform, setup.Volume, 2f);

            setup.Manager.SimulateTick(1d);

            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(7f).Within(Tolerance));
            Assert.That(stateEventCount, Is.EqualTo(1));
            Assert.That(
                setup.Manager.LastTickMetrics.ConfiguredSinkVolume,
                Is.EqualTo(3d).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SourceCurrentFlowRate_ReflectsCapacityScaling()
        {
            var setup = CreateVolumeSetup();
            var source = CreateSource(setup.Root.transform, setup.Volume, 100f);

            setup.Manager.SimulateTick(1d);

            Assert.That(
                source.CurrentFlowRate,
                Is.EqualTo(setup.Volume.MaximumVolume).Within(Tolerance));
            Assert.That(
                setup.Volume.CurrentVolume,
                Is.EqualTo(setup.Volume.MaximumVolume).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PublicRemoveWater_StillWorksOutsideManagedSink()
        {
            var setup = CreateVolumeSetup();
            setup.Volume.AddWater(5f);

            var result = setup.Volume.RemoveWater(2f);

            Assert.That(result.AppliedChange, Is.EqualTo(-2d).Within(Tolerance));
            Assert.That(setup.Volume.CurrentVolume, Is.EqualTo(3f).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        private static VolumeSetup CreateVolumeSetup()
        {
            var root = new GameObject("Flood sink test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = CreateVolume(root.transform, "Volume");
            return new VolumeSetup(root, manager, volume);
        }

        private static FloodVolume CreateVolume(Transform parent, string name)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            return volumeObject.AddComponent<FloodVolume>();
        }

        private static FloodSink CreateSink(
            Transform parent,
            FloodVolume target,
            float flowRate)
        {
            var sinkObject = new GameObject("Flood sink");
            sinkObject.transform.SetParent(parent, false);
            var sink = sinkObject.AddComponent<FloodSink>();
            sink.Target = target;
            sink.FlowRate = flowRate;
            sink.IsActive = true;
            return sink;
        }

        private static FloodSource CreateSource(
            Transform parent,
            FloodVolume target,
            float flowRate)
        {
            var sourceObject = new GameObject("Flood source");
            sourceObject.transform.SetParent(parent, false);
            var source = sourceObject.AddComponent<FloodSource>();
            source.Target = target;
            source.FlowRate = flowRate;
            source.IsActive = true;
            return source;
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

        private readonly struct VolumeSetup
        {
            public VolumeSetup(
                GameObject root,
                FloodSimulationManager manager,
                FloodVolume volume)
            {
                Root = root;
                Manager = manager;
                Volume = volume;
            }

            public GameObject Root { get; }

            public FloodSimulationManager Manager { get; }

            public FloodVolume Volume { get; }
        }
    }
}
