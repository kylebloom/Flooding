using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodSimulationManagerTests
    {
        [UnityTest]
        public IEnumerator SimulateTick_AggregatesSourcesAndPublishesOnce()
        {
            var root = new GameObject("Flood manager aggregation test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            var stateEventCount = 0;
            volume.StateChanged += _ => stateEventCount++;

            CreateSource(root.transform, volume, 1f);
            CreateSource(root.transform, volume, 2f);

            manager.SimulateTick(1d);

            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(3f).Within(0.000001f));
            Assert.That(stateEventCount, Is.EqualTo(1));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SimulateTick_ReconcilesDestinationCapacity()
        {
            var root = new GameObject("Flood manager capacity test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();

            CreateSource(root.transform, volume, 100f);

            manager.SimulateTick(1d);

            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(volume.MaximumVolume).Within(0.000001f));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Advance_UsesFixedRateAndCatchUpLimit()
        {
            var root = new GameObject("Flood manager scheduler test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.TicksPerSecond = 10f;
            manager.MaximumTicksPerFrame = 2;
            var volume = root.AddComponent<FloodVolume>();
            CreateSource(root.transform, volume, 1f);

            manager.Advance(0.05d);
            Assert.That(volume.CurrentVolume, Is.Zero.Within(0.000001f));

            manager.Advance(0.06d);
            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(0.1f).Within(0.000001f));

            manager.Advance(1d);

            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(manager.DiscardedTickCount, Is.GreaterThan(0));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledSource_DoesNotRequestInflow()
        {
            var root = new GameObject("Flood manager disabled source test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            var source = CreateSource(root.transform, volume, 5f);
            source.IsActive = false;

            manager.SimulateTick(1d);

            Assert.That(volume.CurrentVolume, Is.Zero.Within(0.000001f));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Configuration_RejectsInvalidNumericInput()
        {
            var root = new GameObject("Flood manager validation test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => manager.TicksPerSecond = float.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => manager.Advance(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => manager.SimulateTick(-1d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => manager.CustomGravity =
                    new Vector3(float.NaN, 0f, 0f));

            UnityEngine.Object.Destroy(root);
            yield return null;
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

            return source;
        }
    }
}
