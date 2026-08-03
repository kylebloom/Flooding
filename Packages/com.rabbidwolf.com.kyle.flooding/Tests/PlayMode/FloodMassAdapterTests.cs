using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodMassAdapterTests
    {
        [UnityTest]
        public IEnumerator Adapter_AppliesCompositeAndRestoresDryBody()
        {
            var vessel = new GameObject("Flood mass vessel");
            var body = vessel.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            var aggregator = vessel.AddComponent<FloodMassAggregator>();

            var compartment = new GameObject("Flood compartment");
            compartment.transform.SetParent(vessel.transform, false);
            compartment.transform.localPosition = new Vector3(2f, 0f, 0f);
            var volume = compartment.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(1f, 1f, 2f);
            volume.AddWater(1f);

            aggregator.RefreshContributors();
            var adapter = vessel.AddComponent<RigidbodyFloodMassAdapter>();
            adapter.FloodMass = aggregator;
            adapter.ConfigureDryBody(1000f, Vector3.zero);
            var volumeBeforePhysics = volume.CurrentVolume;
            adapter.ApplyMassContribution();

            Assert.That(body.mass, Is.EqualTo(2000f).Within(0.001f));
            Assert.That(body.centerOfMass.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(body.centerOfMass.y, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(volumeBeforePhysics).Within(0.000001f));

            adapter.enabled = false;

            Assert.That(body.mass, Is.EqualTo(1000f).Within(0.001f));
            Assert.That(body.centerOfMass, Is.EqualTo(Vector3.zero));

            Object.Destroy(vessel);
            yield return null;
        }
    }
}
