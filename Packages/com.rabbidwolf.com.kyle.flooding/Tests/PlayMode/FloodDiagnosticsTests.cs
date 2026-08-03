using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodDiagnosticsTests
    {
        [UnityTest]
        public IEnumerator CaptureSnapshot_DoesNotMutateObservedState()
        {
            var vessel = new GameObject("Diagnostic vessel");
            var body = vessel.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var manager = vessel.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var aggregator = vessel.AddComponent<FloodMassAggregator>();

            var compartmentA = CreateCompartment(
                vessel.transform,
                manager,
                "Compartment A",
                new Vector3(-1f, 0f, 0f),
                0.75f);
            var compartmentB = CreateCompartment(
                vessel.transform,
                manager,
                "Compartment B",
                new Vector3(1f, 0f, 0f),
                0.25f);
            aggregator.RefreshContributors();

            var adapter = vessel.AddComponent<RigidbodyFloodMassAdapter>();
            adapter.FloodMass = aggregator;
            adapter.ConfigureDryBody(
                500f,
                new Vector3(0f, -0.25f, 0f));
            adapter.ApplyMassContribution();

            var opening = new GameObject("Diagnostic connection");
            opening.transform.SetParent(vessel.transform, false);
            var connection = opening.AddComponent<FloodConnection>();
            connection.SimulationManager = manager;
            connection.VolumeA = compartmentA;
            connection.VolumeB = compartmentB;
            manager.SimulateTick(0.1d);

            var diagnostics = vessel.AddComponent<FloodDiagnostics>();
            var volumeABefore = compartmentA.CurrentVolume;
            var volumeBBefore = compartmentB.CurrentVolume;
            var bodyMassBefore = body.mass;
            var bodyCenterBefore = body.centerOfMass;
            var requestedBefore = connection.RequestedFlowRate;
            var appliedBefore = connection.CurrentFlowRate;
            var headBefore = connection.PressureHeadDifference;

            var first = diagnostics.CaptureSnapshot();
            var second = diagnostics.CaptureSnapshot();

            Assert.That(first.Volumes.Count, Is.EqualTo(2));
            Assert.That(first.Connections.Count, Is.EqualTo(1));
            Assert.That(second.Water, Is.EqualTo(first.Water));
            Assert.That(
                compartmentA.CurrentVolume,
                Is.EqualTo(volumeABefore).Within(0.000001f));
            Assert.That(
                compartmentB.CurrentVolume,
                Is.EqualTo(volumeBBefore).Within(0.000001f));
            Assert.That(body.mass, Is.EqualTo(bodyMassBefore));
            Assert.That(body.centerOfMass, Is.EqualTo(bodyCenterBefore));
            Assert.That(
                connection.RequestedFlowRate,
                Is.EqualTo(requestedBefore));
            Assert.That(
                connection.CurrentFlowRate,
                Is.EqualTo(appliedBefore));
            Assert.That(
                connection.PressureHeadDifference,
                Is.EqualTo(headBefore));

            Object.Destroy(vessel);
            yield return null;
        }

        private static FloodVolume CreateCompartment(
            Transform parent,
            FloodSimulationManager manager,
            string name,
            Vector3 localPosition,
            float waterVolume)
        {
            var compartment = new GameObject(name);
            compartment.transform.SetParent(parent, false);
            compartment.transform.localPosition = localPosition;
            var volume = compartment.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(1f, 1f, 1f);
            volume.AddWater(waterVolume);
            return volume;
        }
    }
}
