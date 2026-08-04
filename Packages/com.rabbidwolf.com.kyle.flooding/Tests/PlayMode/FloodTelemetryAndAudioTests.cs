using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodTelemetryAndAudioTests
    {
        [UnityTest]
        public IEnumerator VolumeTelemetry_ReportsFillVolumeAndCapacity()
        {
            var root = new GameObject("Volume telemetry");
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            var telemetry = root.AddComponent<FloodVolumeTelemetry>();
            telemetry.Volume = volume;
            telemetry.Refresh();

            Assert.That(telemetry.FillPercentage, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                telemetry.CurrentVolumeCubicMeters,
                Is.EqualTo(4f).Within(0.0001f));
            Assert.That(
                telemetry.CapacityCubicMeters,
                Is.EqualTo(8f).Within(0.0001f));
            Assert.That(telemetry.IsEmpty, Is.False);
            Assert.That(telemetry.IsFull, Is.False);
            Assert.That(telemetry.HasConnection, Is.False);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraTelemetry_ReportsTrackerState()
        {
            var root = new GameObject("Camera telemetry root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            var cameraObject = new GameObject("Telemetry Camera");
            cameraObject.transform.position = new Vector3(0f, 0.25f, 0f);
            var tracker = cameraObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = cameraObject.transform;

            var telemetry = cameraObject.AddComponent<FloodCameraTelemetry>();
            telemetry.Tracker = tracker;

            tracker.Refresh();
            telemetry.Refresh();

            Assert.That(telemetry.IsInsideFloodVolume, Is.True);
            Assert.That(telemetry.IsUnderwater, Is.True);
            Assert.That(telemetry.SubmersionDepthMeters, Is.GreaterThan(0f));
            Assert.That(telemetry.SurfaceSignedDistanceMeters, Is.LessThan(0f));
            Assert.That(telemetry.ActiveVolume, Is.SameAs(volume));

            Object.Destroy(root);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnderwaterAudio_SmoothsTowardUnderwaterTargets()
        {
            var root = new GameObject("Audio root");
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            var cameraObject = new GameObject("Audio Camera");
            cameraObject.SetActive(false);
            cameraObject.transform.position = new Vector3(0f, 1.5f, 0f);
            var tracker = cameraObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = cameraObject.transform;

            var audio = cameraObject.AddComponent<FloodUnderwaterAudio>();
            audio.Tracker = tracker;
            audio.TransitionDurationSeconds = 0.5f;
            audio.NormalLowPassCutoffHz = 20000f;
            audio.UnderwaterLowPassCutoffHz = 500f;
            audio.NormalVolumeDb = 0f;
            audio.UnderwaterVolumeDb = -6f;

            cameraObject.SetActive(true);
            tracker.Refresh();
            audio.Refresh(0f);

            Assert.That(audio.CurrentUnderwaterBlend, Is.Zero);
            Assert.That(audio.CurrentLowPassCutoffHz, Is.EqualTo(20000f));

            cameraObject.transform.position = new Vector3(0f, 0.25f, 0f);
            tracker.Refresh();
            audio.Refresh(0.25f);

            Assert.That(audio.CurrentUnderwaterBlend, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                audio.CurrentLowPassCutoffHz,
                Is.EqualTo(10250f).Within(0.1f));
            Assert.That(audio.CurrentVolumeDb, Is.EqualTo(-3f).Within(0.0001f));

            audio.Refresh(1f);
            Assert.That(audio.CurrentUnderwaterBlend, Is.EqualTo(1f));
            Assert.That(audio.CurrentLowPassCutoffHz, Is.EqualTo(500f));
            Assert.That(audio.CurrentVolumeDb, Is.EqualTo(-6f));

            Object.Destroy(root);
            Object.Destroy(cameraObject);
            yield return null;
        }
    }
}
