using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodLocalIngressPresenterTests
    {
        private const float Tolerance = 0.0001f;

        [UnityTest]
        public IEnumerator Presenter_DoesNotMutateAuthoritativeVolume()
        {
            var root = new GameObject("Local ingress presenter test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeObject = new GameObject("Volume");
            volumeObject.transform.SetParent(root.transform, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(4f, 4f, 2.5f);

            var sourceObject = new GameObject("Source");
            sourceObject.transform.SetParent(root.transform, false);
            sourceObject.transform.position = new Vector3(0f, 1.5f, 0f);
            sourceObject.transform.rotation = Quaternion.LookRotation(Vector3.down);
            var source = sourceObject.AddComponent<FloodSource>();
            source.SimulationManager = manager;
            source.Target = volume;
            source.FlowRate = 1.5f;
            source.IsActive = true;

            var profile =
                ScriptableObject.CreateInstance<FloodIngressPresentationProfile>();
            profile.MinimumFlowRate = 0.01f;
            profile.MaximumSimultaneousPatches = 4;

            var presenterObject = new GameObject("Presenter");
            presenterObject.transform.SetParent(root.transform, false);
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform, false);
            floor.transform.position = Vector3.zero;

            var presenter = presenterObject.AddComponent<FloodLocalIngressPresenter>();
            presenter.Volume = volume;
            presenter.Profile = profile;
            presenter.FloorPlane = floor.transform;
            presenter.Sources = new[] { source };
            presenter.PresentationEnabled = true;

            var volumeBefore = volume.CurrentVolume;
            manager.SimulateTick(0.25d);
            var volumeAfterSim = volume.CurrentVolume;
            presenter.Refresh(0.1f);

            Assert.That(volumeAfterSim, Is.GreaterThan(volumeBefore));
            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(volumeAfterSim).Within(Tolerance));
            Assert.That(presenter.ActivePatchCount, Is.GreaterThan(0));
            Assert.That(
                presenter.CurrentInflowRateCubicMetersPerSecond,
                Is.EqualTo(1.5f).Within(Tolerance));

            presenter.PresentationEnabled = false;
            presenter.Refresh(0.1f);
            Assert.That(presenter.ActivePatchCount, Is.Zero);
            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(volumeAfterSim).Within(Tolerance));

            Object.Destroy(profile);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Presenter_ReversedConnectionUpdatesDestinationIngressOnly()
        {
            var root = new GameObject("Reversed ingress test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeA = CreateVolume(root.transform, "A");
            var volumeB = CreateVolume(root.transform, "B");
            volumeB.AddWater(12f);

            var connectionObject = new GameObject("Connection");
            connectionObject.transform.SetParent(root.transform, false);
            connectionObject.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.VolumeA = volumeA;
            connection.VolumeB = volumeB;
            connection.OpeningWidth = 2f;
            connection.OpeningHeight = 1f;
            connection.DischargeCoefficient = 1f;

            manager.SimulateTick(0.5d);
            Assert.That(connection.CurrentFlowRate, Is.LessThan(0d));

            Assert.That(
                FloodIngressSampler.TrySample(connection, volumeA, out var intoA),
                Is.True);
            Assert.That(
                Vector3.Dot(intoA.DirectionWorld, -Vector3.forward),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                FloodIngressSampler.TrySample(connection, volumeB, out _),
                Is.False);

            var profile =
                ScriptableObject.CreateInstance<FloodIngressPresentationProfile>();
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform, false);

            var presenterA = root.AddComponent<FloodLocalIngressPresenter>();
            presenterA.Volume = volumeA;
            presenterA.Profile = profile;
            presenterA.FloorPlane = floor.transform;
            presenterA.Connections = new[] { connection };
            presenterA.Refresh(0.1f);

            Assert.That(presenterA.ActivePatchCount, Is.EqualTo(1));

            Object.Destroy(profile);
            Object.Destroy(root);
            yield return null;
        }

        private static FloodVolume CreateVolume(Transform parent, string name)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(3f, 3f, 2f);
            return volume;
        }
    }
}
