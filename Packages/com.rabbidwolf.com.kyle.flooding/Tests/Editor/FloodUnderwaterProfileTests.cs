using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodUnderwaterProfileTests
    {
        [Test]
        public void Defaults_AreRestrainedAndFinite()
        {
            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();

            Assert.That(profile.FullEffectDepthMeters, Is.EqualTo(2f));
            Assert.That(profile.FogDensity, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(profile.MaximumFogStrength, Is.InRange(0f, 1f));
            Assert.That(profile.Saturation, Is.InRange(0.5f, 1.2f));
            Assert.That(profile.Contrast, Is.InRange(0.5f, 1.5f));
            Assert.That(profile.DistortionStrength, Is.InRange(0f, 0.05f));
            Assert.That(profile.TransitionDurationSeconds, Is.GreaterThan(0f));
            Assert.That(IsFiniteColor(profile.ShallowTintColor), Is.True);
            Assert.That(IsFiniteColor(profile.DeepTintColor), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void EvaluateDepthStrength_MapsSubmersionToUnitInterval()
        {
            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
            profile.FullEffectDepthMeters = 2f;

            Assert.That(profile.EvaluateDepthStrength(-1f), Is.Zero);
            Assert.That(profile.EvaluateDepthStrength(0f), Is.Zero);
            Assert.That(profile.EvaluateDepthStrength(1f), Is.EqualTo(0.5f));
            Assert.That(profile.EvaluateDepthStrength(2f), Is.EqualTo(1f));
            Assert.That(profile.EvaluateDepthStrength(4f), Is.EqualTo(1f));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void EvaluateTintColor_LerpsShallowToDeepByDepth()
        {
            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
            profile.FullEffectDepthMeters = 2f;
            profile.ShallowTintColor = Color.white;
            profile.DeepTintColor = Color.black;

            var mid = profile.EvaluateTintColor(1f);
            Assert.That(mid.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                profile.EvaluateTintColor(0f),
                Is.EqualTo(Color.white));
            Assert.That(
                profile.EvaluateTintColor(2f),
                Is.EqualTo(Color.black));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void EvaluateFogStrength_IncreasesWithDepthAndRespectsMaximum()
        {
            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
            profile.FullEffectDepthMeters = 2f;
            profile.FogDensity = 0.5f;
            profile.MaximumFogStrength = 0.75f;

            var shallow = profile.EvaluateFogStrength(0.2f);
            var deep = profile.EvaluateFogStrength(2f);

            Assert.That(shallow, Is.GreaterThan(0f));
            Assert.That(deep, Is.GreaterThan(shallow));
            Assert.That(deep, Is.LessThanOrEqualTo(0.75f));
            Assert.That(profile.EvaluateFogStrength(0f), Is.Zero);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void PropertySetters_ClampInvalidValues()
        {
            var profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();

            profile.FullEffectDepthMeters = -5f;
            profile.FogDensity = -1f;
            profile.MaximumFogStrength = 3f;
            profile.DistortionStrength = -0.5f;
            profile.TransitionDurationSeconds = -2f;

            Assert.That(profile.FullEffectDepthMeters, Is.EqualTo(0.01f));
            Assert.That(profile.FogDensity, Is.Zero);
            Assert.That(profile.MaximumFogStrength, Is.EqualTo(1f));
            Assert.That(profile.DistortionStrength, Is.Zero);
            Assert.That(profile.TransitionDurationSeconds, Is.Zero);

            Object.DestroyImmediate(profile);
        }

        private static bool IsFiniteColor(Color color)
        {
            return IsFinite(color.r)
                && IsFinite(color.g)
                && IsFinite(color.b)
                && IsFinite(color.a);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
