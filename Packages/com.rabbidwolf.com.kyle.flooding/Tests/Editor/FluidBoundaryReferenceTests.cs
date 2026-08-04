using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FluidBoundaryReferenceTests
    {
        [Test]
        public void TryResolveComponent_AcceptsNull()
        {
            Assert.That(
                FluidBoundaryReference.TryResolveComponent(null, out var resolved),
                Is.True);
            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void TryResolveComponent_AcceptsExternalFluidBoundaryComponent()
        {
            var gameObject = new GameObject("Ocean");
            var boundary = gameObject.AddComponent<ExternalFluidBoundary>();

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(
                    boundary,
                    out var resolved),
                Is.True);
            Assert.That(resolved, Is.EqualTo(boundary));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryResolveComponent_AcceptsFloodVolumeComponent()
        {
            var gameObject = new GameObject("Compartment");
            var volume = gameObject.AddComponent<FloodVolume>();

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(volume, out var resolved),
                Is.True);
            Assert.That(resolved, Is.EqualTo(volume));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryResolveComponent_AcceptsGameObjectWithExternalFluidBoundary()
        {
            var gameObject = new GameObject("StormWater");
            var boundary = gameObject.AddComponent<ExternalFluidBoundary>();

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(
                    gameObject,
                    out var resolved),
                Is.True);
            Assert.That(resolved, Is.EqualTo(boundary));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryResolveComponent_AcceptsGameObjectWithFloodVolume()
        {
            var gameObject = new GameObject("Basement");
            var volume = gameObject.AddComponent<FloodVolume>();

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(
                    gameObject,
                    out var resolved),
                Is.True);
            Assert.That(resolved, Is.EqualTo(volume));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryResolveComponent_RejectsGameObjectWithoutBoundary()
        {
            var gameObject = new GameObject("Empty");

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(
                    gameObject,
                    out var resolved),
                Is.False);
            Assert.That(resolved, Is.Null);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryResolveComponent_RejectsUnsupportedComponent()
        {
            var gameObject = new GameObject("Transform host");

            Assert.That(
                FluidBoundaryReference.TryResolveComponent(
                    gameObject.transform,
                    out var resolved),
                Is.False);
            Assert.That(resolved, Is.Null);

            Object.DestroyImmediate(gameObject);
        }
    }
}
