using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodCameraTrackerTests
    {
        [UnityTest]
        public IEnumerator RegisteredVolumes_IsReadOnlyLiveView()
        {
            var root = new GameObject("Camera tracker registry");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);

            var view = manager.RegisteredVolumes;
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Count, Is.EqualTo(1));
            Assert.That(view[0], Is.SameAs(volume));

            var asList = (IList<FloodVolume>)view;
            Assert.That(asList.IsReadOnly, Is.True);
            Assert.That(
                () => asList.Add(volume),
                Throws.TypeOf<System.NotSupportedException>());

            var secondObject = new GameObject("Second volume");
            secondObject.transform.SetParent(root.transform, false);
            var second = secondObject.AddComponent<FloodVolume>();
            second.ConfigureRectangularGeometry(2f, 2f, 2f);

            Assert.That(view.Count, Is.EqualTo(2));
            Assert.That(view, Does.Contain(second));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitMode_TracksAssignedVolume()
        {
            var root = CreateRoot(out var manager, out var volume);
            volume.AddWater(4f); // 1 m fill in 2x2x2

            var trackerObject = new GameObject("Tracker");
            trackerObject.transform.SetParent(root.transform, false);
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = trackerObject.transform;
            tracker.Refresh();

            Assert.That(tracker.ActiveVolume, Is.SameAs(volume));
            Assert.That(tracker.IsInsideFloodVolume, Is.True);
            Assert.That(tracker.IsUnderwater, Is.True);
            Assert.That(tracker.SurfaceSignedDistanceMeters, Is.LessThan(0f));
            Assert.That(tracker.SubmersionDepthMeters, Is.GreaterThan(0f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoMode_SelectsContainingRegisteredVolume()
        {
            var root = CreateRoot(out var manager, out var volume);
            volume.AddWater(4f);

            var trackerObject = new GameObject("Tracker");
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode =
                FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;
            tracker.Manager = manager;
            tracker.Viewpoint = trackerObject.transform;
            tracker.Refresh();

            Assert.That(tracker.ActiveVolume, Is.SameAs(volume));
            Assert.That(tracker.IsInsideFloodVolume, Is.True);

            trackerObject.transform.position = new Vector3(10f, 0.25f, 0f);
            tracker.Refresh();

            Assert.That(tracker.ActiveVolume, Is.Null);
            Assert.That(tracker.IsInsideFloodVolume, Is.False);
            Assert.That(tracker.IsUnderwater, Is.False);

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StickySelection_KeepsActiveVolumeWhileContainedAndDry()
        {
            var root = new GameObject("Sticky selection root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeA = CreateVolume(
                root.transform,
                "Volume A",
                Vector3.zero,
                4f);
            volumeA.AddWater(8f); // fill 0.5 m in 4x4x2

            var volumeB = CreateVolume(
                root.transform,
                "Volume B",
                Vector3.zero,
                4f);
            // Overlapping geometry; B remains empty so A is preferred when both
            // contain and selection is re-evaluated.

            var trackerObject = new GameObject("Tracker");
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode =
                FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;
            tracker.Manager = manager;
            tracker.Viewpoint = trackerObject.transform;
            tracker.Refresh();

            Assert.That(tracker.ActiveVolume, Is.SameAs(volumeA));
            Assert.That(tracker.IsUnderwater, Is.True);

            // Rise above the surface but remain inside both compartments.
            trackerObject.transform.position = new Vector3(0f, 1.0f, 0f);
            tracker.Refresh();

            Assert.That(tracker.IsInsideFloodVolume, Is.True);
            Assert.That(tracker.IsUnderwater, Is.False);
            Assert.That(
                tracker.ActiveVolume,
                Is.SameAs(volumeA),
                "Dry inside the current compartment must not reselect another overlapping volume.");

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Overlap_PrefersSubmergedThenRegistrationOrder()
        {
            var root = new GameObject("Overlap selection root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var dryFirst = CreateVolume(
                root.transform,
                "Dry first",
                Vector3.zero,
                4f);
            var wetSecond = CreateVolume(
                root.transform,
                "Wet second",
                Vector3.zero,
                4f);
            wetSecond.AddWater(8f);

            var trackerObject = new GameObject("Tracker");
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode =
                FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;
            tracker.Manager = manager;
            tracker.Viewpoint = trackerObject.transform;
            tracker.Refresh();

            Assert.That(tracker.ActiveVolume, Is.SameAs(wetSecond));

            // Leave both, then re-enter with both dry: first registration wins.
            trackerObject.transform.position = new Vector3(20f, 0.25f, 0f);
            tracker.Refresh();
            Assert.That(tracker.ActiveVolume, Is.Null);

            wetSecond.RemoveWater(8f);
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            tracker.Refresh();
            Assert.That(tracker.ActiveVolume, Is.SameAs(dryFirst));

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Events_FireForVolumeAndWaterTransitions()
        {
            var root = CreateRoot(out _, out var volume);
            volume.AddWater(4f);

            var trackerObject = new GameObject("Tracker");
            trackerObject.SetActive(false);
            trackerObject.transform.position = new Vector3(0f, 1.5f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = trackerObject.transform;
            tracker.EnterWaterThresholdMeters = -0.02f;
            tracker.ExitWaterThresholdMeters = 0.02f;

            var enteredVolume = 0;
            var exitedVolume = 0;
            var enteredWater = 0;
            var exitedWater = 0;
            var activeChanged = 0;
            FloodVolume lastEntered = null;
            FloodVolume lastExited = null;

            tracker.EnteredFloodVolume += v =>
            {
                enteredVolume++;
                lastEntered = v;
            };
            tracker.ExitedFloodVolume += v =>
            {
                exitedVolume++;
                lastExited = v;
            };
            tracker.EnteredWater += () => enteredWater++;
            tracker.ExitedWater += () => exitedWater++;
            tracker.ActiveVolumeChanged += _ => activeChanged++;

            trackerObject.SetActive(true);
            Assert.That(tracker.IsInsideFloodVolume, Is.True);
            Assert.That(tracker.IsUnderwater, Is.False);
            Assert.That(enteredVolume, Is.EqualTo(1));
            Assert.That(lastEntered, Is.SameAs(volume));
            Assert.That(enteredWater, Is.EqualTo(0));
            Assert.That(activeChanged, Is.EqualTo(1));

            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsUnderwater, Is.True);
            Assert.That(enteredWater, Is.EqualTo(1));

            trackerObject.transform.position = new Vector3(0f, 1.05f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsUnderwater, Is.False);
            Assert.That(exitedWater, Is.EqualTo(1));

            trackerObject.transform.position = new Vector3(10f, 1.05f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsInsideFloodVolume, Is.False);
            Assert.That(exitedVolume, Is.EqualTo(1));
            Assert.That(lastExited, Is.SameAs(volume));

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hysteresis_PreventsSurfaceFlicker()
        {
            var root = CreateRoot(out _, out var volume);
            volume.AddWater(4f); // surface at y = 1 in 2x2x2

            var trackerObject = new GameObject("Tracker");
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = trackerObject.transform;
            tracker.EnterWaterThresholdMeters = -0.02f;
            tracker.ExitWaterThresholdMeters = 0.02f;

            trackerObject.transform.position = new Vector3(0f, 1.05f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsUnderwater, Is.False);

            trackerObject.transform.position = new Vector3(0f, 0.99f, 0f);
            tracker.Refresh();
            Assert.That(
                tracker.IsUnderwater,
                Is.False,
                "Slightly below the geometric surface but above enter threshold stays dry.");

            trackerObject.transform.position = new Vector3(0f, 0.97f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsUnderwater, Is.True);

            trackerObject.transform.position = new Vector3(0f, 1.01f, 0f);
            tracker.Refresh();
            Assert.That(
                tracker.IsUnderwater,
                Is.True,
                "Slightly above the geometric surface but below exit threshold stays wet.");

            trackerObject.transform.position = new Vector3(0f, 1.03f, 0f);
            tracker.Refresh();
            Assert.That(tracker.IsUnderwater, Is.False);

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoMode_ResolvesManagerSpawnedAfterTracker()
        {
            var trackerObject = new GameObject("Late manager tracker");
            trackerObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode =
                FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;
            tracker.Viewpoint = trackerObject.transform;

            tracker.Refresh();
            Assert.That(tracker.Manager, Is.Null);
            Assert.That(tracker.ActiveVolume, Is.Null);

            var root = new GameObject("Late manager root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            // First failed resolve schedules a retry; wait past the interval.
            yield return new WaitForSecondsRealtime(0.55f);
            tracker.Refresh();

            Assert.That(tracker.Manager, Is.SameAs(manager));
            Assert.That(tracker.ActiveVolume, Is.SameAs(volume));
            Assert.That(tracker.IsInsideFloodVolume, Is.True);

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThresholdSetters_EnforceEnterNotAboveExit()
        {
            var trackerObject = new GameObject("Threshold tracker");
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();

            tracker.ExitWaterThresholdMeters = 0.01f;
            tracker.EnterWaterThresholdMeters = 0.05f;

            Assert.That(tracker.EnterWaterThresholdMeters, Is.EqualTo(0.05f));
            Assert.That(tracker.ExitWaterThresholdMeters, Is.EqualTo(0.05f));

            tracker.ExitWaterThresholdMeters = 0.01f;
            Assert.That(tracker.ExitWaterThresholdMeters, Is.EqualTo(0.05f));

            Object.Destroy(trackerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RotatedVolume_UsesSurfacePlaneSignedDistance()
        {
            var root = new GameObject("Rotated tracker root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volumeObject = new GameObject("Rotated volume");
            volumeObject.transform.SetParent(root.transform, false);
            volumeObject.transform.SetPositionAndRotation(
                new Vector3(2f, 1f, -1f),
                Quaternion.Euler(12f, -25f, 18f));
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(4f, 4f, 2f);
            volume.AddWater(8f);

            var sample = volume.WaterCenterOfMassWorld;
            var trackerObject = new GameObject("Tracker");
            trackerObject.transform.position = sample;
            var tracker = trackerObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = trackerObject.transform;
            tracker.Refresh();

            var expected = volume.SurfacePlane.GetDistanceToPoint(sample);
            Assert.That(tracker.IsInsideFloodVolume, Is.True);
            Assert.That(tracker.IsUnderwater, Is.True);
            Assert.That(
                tracker.SurfaceSignedDistanceMeters,
                Is.EqualTo(expected).Within(0.0001f));
            Assert.That(
                tracker.SurfaceSignedDistanceMeters,
                Is.Not.EqualTo(sample.y).Within(0.01f));

            Object.Destroy(root);
            Object.Destroy(trackerObject);
            yield return null;
        }

        private static GameObject CreateRoot(
            out FloodSimulationManager manager,
            out FloodVolume volume)
        {
            var root = new GameObject("Camera tracker root");
            manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            return root;
        }

        private static FloodVolume CreateVolume(
            Transform parent,
            string name,
            Vector3 localPosition,
            float size)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            var volume = gameObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(size, size, 2f);
            return volume;
        }
    }
}
