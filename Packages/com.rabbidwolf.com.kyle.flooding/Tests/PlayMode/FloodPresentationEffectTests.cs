using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodPresentationEffectTests
    {
        private const float Tolerance = 0.0001f;

        [UnityTest]
        public IEnumerator ConnectionVisual_ScalesWithAppliedFlowAndDoesNotMutateSimulation()
        {
            var setup = CreateConnectedSetup();
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(setup.Root.transform, false);
            indicator.transform.localScale = Vector3.one;

            var visual = setup.Connection.gameObject.AddComponent<FloodConnectionVisual>();
            visual.Connection = setup.Connection;
            visual.FlowIndicator = indicator.transform;

            setup.VolumeA.AddWater(12f);
            var initialTotal =
                setup.VolumeA.CurrentVolume + setup.VolumeB.CurrentVolume;

            setup.Manager.SimulateTick(0.5d);
            visual.Refresh();

            Assert.That(setup.Connection.CurrentFlowRate, Is.GreaterThan(0d));
            Assert.That(visual.CurrentIntensity, Is.GreaterThan(0f));
            Assert.That(
                indicator.transform.localScale.magnitude,
                Is.GreaterThan(1f));
            Assert.That(
                setup.VolumeA.CurrentVolume + setup.VolumeB.CurrentVolume,
                Is.EqualTo(initialTotal).Within(Tolerance));

            visual.enabled = false;
            setup.Manager.SimulateTick(0.5d);
            var totalAfterDisable =
                setup.VolumeA.CurrentVolume + setup.VolumeB.CurrentVolume;

            Assert.That(totalAfterDisable, Is.EqualTo(initialTotal).Within(Tolerance));

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConnectionVisual_HidesWhenFlowStops()
        {
            var setup = CreateConnectedSetup();
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(setup.Root.transform, false);

            var visual = setup.Connection.gameObject.AddComponent<FloodConnectionVisual>();
            visual.Connection = setup.Connection;
            visual.FlowIndicator = indicator.transform;

            setup.VolumeA.AddWater(8f);
            setup.Manager.SimulateTick(0.25d);
            visual.Refresh();
            Assert.That(visual.CurrentIntensity, Is.GreaterThan(0f));

            setup.Connection.IsOpen = false;
            setup.Manager.SimulateTick(0.25d);
            visual.Refresh();

            Assert.That(setup.Connection.CurrentFlowRate, Is.Zero);
            Assert.That(visual.CurrentIntensity, Is.Zero);
            Assert.That(indicator.activeSelf, Is.False);

            Object.Destroy(setup.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VolumeAudio_TracksFillWithoutMutatingVolume()
        {
            var root = new GameObject("Volume audio test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            var audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = AudioClip.Create(
                "test-ambience",
                4410,
                1,
                44100,
                false);

            var volumeAudio = root.AddComponent<FloodVolumeAudio>();
            volumeAudio.Volume = volume;

            volume.AddWater(volume.MaximumVolume * 0.5f);
            manager.SimulateTick(0.1d);
            var expected = volume.CurrentVolume;
            volumeAudio.Refresh();

            Assert.That(volumeAudio.CurrentIntensity, Is.GreaterThan(0f));
            Assert.That(volume.CurrentVolume, Is.EqualTo(expected).Within(Tolerance));

            volumeAudio.enabled = false;
            volume.AddWater(1f);
            manager.SimulateTick(0.1d);

            Assert.That(
                volume.CurrentVolume,
                Is.EqualTo(expected + 1f).Within(Tolerance));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SourceAudio_IsSilentWhenInactive()
        {
            var root = new GameObject("Source audio test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            var source = root.AddComponent<FloodSource>();
            source.Target = volume;
            source.FlowRate = 2f;
            source.IsActive = false;

            var audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = AudioClip.Create(
                "test-source",
                4410,
                1,
                44100,
                false);

            var sourceAudio = root.AddComponent<FloodSourceAudio>();
            sourceAudio.Source = source;
            sourceAudio.Refresh();

            Assert.That(sourceAudio.CurrentIntensity, Is.Zero);
            Assert.That(audioSource.isPlaying, Is.False);

            source.IsActive = true;
            sourceAudio.Refresh();

            Assert.That(sourceAudio.CurrentIntensity, Is.GreaterThan(0f));
            Assert.That(audioSource.isPlaying, Is.True);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConnectionAudio_MissingClipFailsSoft()
        {
            var setup = CreateConnectedSetup();
            var audioSource = setup.Connection.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = null;

            var connectionAudio =
                setup.Connection.gameObject.AddComponent<FloodConnectionAudio>();
            connectionAudio.Connection = setup.Connection;

            setup.VolumeA.AddWater(10f);
            setup.Manager.SimulateTick(0.5d);
            Assert.DoesNotThrow(() => connectionAudio.Refresh());
            Assert.That(audioSource.isPlaying, Is.False);

            Object.Destroy(setup.Root);
            yield return null;
        }

        private static ConnectedSetup CreateConnectedSetup()
        {
            var root = new GameObject("Presentation effect test");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeA = CreateVolume(root.transform, "A");
            var volumeB = CreateVolume(root.transform, "B");
            var connectionObject = new GameObject("Connection");
            connectionObject.transform.SetParent(root.transform, false);
            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.VolumeA = volumeA;
            connection.VolumeB = volumeB;
            connection.OpeningWidth = 2f;
            connection.OpeningHeight = 1f;
            connection.DischargeCoefficient = 1f;

            return new ConnectedSetup(root, manager, volumeA, volumeB, connection);
        }

        private static FloodVolume CreateVolume(Transform parent, string name)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(3f, 3f, 2f);
            return volume;
        }

        private readonly struct ConnectedSetup
        {
            public ConnectedSetup(
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
