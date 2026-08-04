using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Kyle.Flooding.URP;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodUnderwaterCameraEffectTests
    {
        [UnityTest]
        public IEnumerator EffectBlend_EnablesInsideFloodedVolumeWhileDry()
        {
            var root = new GameObject("URP underwater effect root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            var cameraObject = new GameObject("Effect Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 1.5f, 0f);

            var tracker = cameraObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = cameraObject.transform;

            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
            profile.TransitionDurationSeconds = 0f;

            var effect = cameraObject.AddComponent<FloodUnderwaterCameraEffect>();
            effect.UpdateAutomatically = false;
            effect.Tracker = tracker;
            effect.Profile = profile;

            tracker.Refresh();
            effect.Refresh(0f);

            Assert.That(tracker.IsInsideFloodVolume, Is.True);
            Assert.That(tracker.IsUnderwater, Is.False);
            Assert.That(effect.EffectBlend, Is.EqualTo(1f));
            Assert.That(effect.CanRender, Is.True);

            cameraObject.transform.position = new Vector3(10f, 1.5f, 0f);
            tracker.Refresh();
            effect.Refresh(0f);

            Assert.That(tracker.IsInsideFloodVolume, Is.False);
            Assert.That(effect.EffectBlend, Is.Zero);
            Assert.That(effect.CanRender, Is.False);

            Object.Destroy(profile);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EffectBlend_SmoothsWithDeltaTime()
        {
            var root = new GameObject("URP underwater smooth root");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;

            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(4f);

            var cameraObject = new GameObject("Smooth Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 1.5f, 0f);

            var tracker = cameraObject.AddComponent<FloodCameraTracker>();
            tracker.UpdateAutomatically = false;
            tracker.VolumeSelectionMode = FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = cameraObject.transform;

            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
            profile.TransitionDurationSeconds = 0.5f;

            var effect = cameraObject.AddComponent<FloodUnderwaterCameraEffect>();
            effect.UpdateAutomatically = false;
            effect.Tracker = tracker;
            effect.Profile = profile;

            tracker.Refresh();
            effect.Refresh(0.1f);

            Assert.That(effect.EffectBlend, Is.EqualTo(0.2f).Within(0.0001f));

            Object.Destroy(profile);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
            yield return null;
        }
    }
}
