using System;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodIngressPresentationTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Sampler_Source_MapsTargetPositionDirectionAndRate()
        {
            var root = new GameObject("Source sampler");
            var volume = root.AddComponent<FloodVolume>();
            var source = root.AddComponent<FloodSource>();
            source.Target = volume;
            source.FlowRate = 1.25f;
            source.IsActive = true;
            root.transform.position = new Vector3(2f, 3f, 4f);
            root.transform.rotation = Quaternion.LookRotation(Vector3.down);

            Assert.That(
                FloodIngressSampler.TrySample(source, out var sample),
                Is.True);
            Assert.That(sample.DestinationVolume, Is.SameAs(volume));
            Assert.That(sample.WorldPosition, Is.EqualTo(root.transform.position));
            Assert.That(
                Vector3.Dot(sample.DirectionWorld.normalized, Vector3.down),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                sample.FlowRateCubicMetersPerSecond,
                Is.EqualTo(1.25f).Within(Tolerance));
            Assert.That(sample.ProviderId, Is.EqualTo(source.GetEntityId()));

            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Sampler_Connection_PositiveFlow_SelectsSideBAndForward()
        {
            var setup = CreateConnectionSetup();
            setup.Connection.transform.position = new Vector3(0f, 0f, 0f);
            setup.Connection.transform.rotation = Quaternion.LookRotation(Vector3.right);
            setup.Connection.OpeningHeight = 2f;
            setup.Connection.ApplyTickResult(
                new FloodFlowResult(1.5d, 1d, 1d),
                1.5d);

            Assert.That(
                FloodIngressSampler.TrySample(
                    setup.Connection,
                    setup.VolumeB,
                    out var sample),
                Is.True);
            Assert.That(sample.DestinationVolume, Is.SameAs(setup.VolumeB));
            Assert.That(
                sample.WorldPosition,
                Is.EqualTo(setup.Connection.OpeningCenterWorld));
            Assert.That(
                Vector3.Dot(sample.DirectionWorld.normalized, Vector3.right),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                sample.FlowRateCubicMetersPerSecond,
                Is.EqualTo(1.5f).Within(Tolerance));

            Assert.That(
                FloodIngressSampler.TrySample(
                    setup.Connection,
                    setup.VolumeA,
                    out _),
                Is.False);

            UnityEngine.Object.DestroyImmediate(setup.Root);
        }

        [Test]
        public void Sampler_Connection_ReversedFlow_SelectsSideAAndFlipsDirection()
        {
            var setup = CreateConnectionSetup();
            setup.Connection.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            setup.Connection.ApplyTickResult(
                new FloodFlowResult(-0.8d, 1d, -1d),
                -0.8d);

            Assert.That(
                FloodIngressSampler.TrySample(
                    setup.Connection,
                    out var sample),
                Is.True);
            Assert.That(sample.DestinationVolume, Is.SameAs(setup.VolumeA));
            Assert.That(
                Vector3.Dot(sample.DirectionWorld.normalized, -Vector3.forward),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                sample.FlowRateCubicMetersPerSecond,
                Is.EqualTo(0.8f).Within(Tolerance));

            Assert.That(
                FloodIngressSampler.TrySample(
                    setup.Connection,
                    setup.VolumeB,
                    out _),
                Is.False);

            UnityEngine.Object.DestroyImmediate(setup.Root);
        }

        [Test]
        public void Sampler_Connection_UsesIngressAnchorOverOpeningCenter()
        {
            var setup = CreateConnectionSetup();
            var anchor = new GameObject("Anchor");
            anchor.transform.SetParent(setup.Root.transform, false);
            anchor.transform.position = new Vector3(9f, 8f, 7f);
            setup.Connection.IngressAnchor = anchor.transform;
            setup.Connection.OpeningHeight = 2f;
            setup.Connection.ApplyTickResult(
                new FloodFlowResult(1d, 1d, 1d),
                1d);

            Assert.That(
                FloodIngressSampler.TrySample(setup.Connection, out var sample),
                Is.True);
            Assert.That(sample.WorldPosition, Is.EqualTo(anchor.transform.position));
            Assert.That(
                setup.Connection.IngressWorldPosition,
                Is.Not.EqualTo(setup.Connection.OpeningCenterWorld));

            UnityEngine.Object.DestroyImmediate(setup.Root);
        }

        [Test]
        public void PresentationState_IgnoresFlowBelowMinimum()
        {
            var profile = CreateProfile();
            profile.MinimumFlowRate = 0.2f;
            var state = new FloodIngressPresentationState(4);
            var provider = CreateProviderId();

            state.Tick(
                0.1f,
                new[]
                {
                    new FloodIngressSample(
                        provider,
                        null,
                        Vector3.zero,
                        Vector3.forward,
                        0.05f),
                },
                profile,
                Vector3.up);

            Assert.That(state.ActivePatchCount, Is.Zero);

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_CreatesPatchAboveMinimumFlow()
        {
            var profile = CreateProfile();
            var state = new FloodIngressPresentationState(4);
            var provider = CreateProviderId();

            state.Tick(
                0.1f,
                new[] { MakeSample(provider, 0.5f) },
                profile,
                Vector3.up);

            Assert.That(state.ActivePatchCount, Is.EqualTo(1));
            Assert.That(state.TryGetPatch(provider, out var patch), Is.True);
            Assert.That(patch.Phase, Is.EqualTo(FloodIngressPatchPhase.Growing));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_SameProviderUpdatesOnePatch()
        {
            var profile = CreateProfile();
            var state = new FloodIngressPresentationState(4);
            var provider = CreateProviderId();

            state.Tick(0.1f, new[] { MakeSample(provider, 0.5f) }, profile, Vector3.up);
            state.Tick(0.1f, new[] { MakeSample(provider, 0.6f) }, profile, Vector3.up);
            state.Tick(0.1f, new[] { MakeSample(provider, 0.7f) }, profile, Vector3.up);

            Assert.That(state.ActivePatchCount, Is.EqualTo(1));
            Assert.That(state.TryGetPatch(provider, out var patch), Is.True);
            Assert.That(patch.Phase, Is.EqualTo(FloodIngressPatchPhase.Growing));
            Assert.That(patch.FlowImpulse, Is.GreaterThan(0f));
            Assert.That(patch.AgeSeconds, Is.EqualTo(0.3f).Within(Tolerance));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_RadiusIncreasesWhileGrowing()
        {
            var profile = CreateProfile();
            profile.LocalSpreadSpeed = 2f;
            profile.MaximumLocalRadius = 5f;
            var state = new FloodIngressPresentationState(2);
            var provider = CreateProviderId();

            state.Tick(0.1f, new[] { MakeSample(provider, 1f) }, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var first), Is.True);
            var radiusAfterFirst = first.CurrentRadius;

            state.Tick(0.5f, new[] { MakeSample(provider, 1f) }, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var second), Is.True);
            Assert.That(second.CurrentRadius, Is.GreaterThan(radiusAfterFirst));
            Assert.That(second.TargetRadius, Is.GreaterThan(second.CurrentRadius - Tolerance));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_StopTransitionsThroughSettlingAndConvergingWithoutPopping()
        {
            var profile = CreateProfile();
            profile.SettlingDurationSeconds = 0.5f;
            profile.ConvergenceDurationSeconds = 1f;
            var state = new FloodIngressPresentationState(2);
            var provider = CreateProviderId();

            state.Tick(0.1f, new[] { MakeSample(provider, 1f) }, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var growing), Is.True);
            Assert.That(growing.Phase, Is.EqualTo(FloodIngressPatchPhase.Growing));

            // Provider disappears / flow stops — do not remove immediately.
            state.Tick(0.1f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var settling), Is.True);
            Assert.That(settling.Phase, Is.EqualTo(FloodIngressPatchPhase.Settling));
            Assert.That(settling.HandoffFraction, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(state.ActivePatchCount, Is.EqualTo(1));

            state.Tick(0.5f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var converging), Is.True);
            Assert.That(converging.Phase, Is.EqualTo(FloodIngressPatchPhase.Converging));
            Assert.That(state.ActivePatchCount, Is.EqualTo(1));

            state.Tick(0.5f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var midHandoff), Is.True);
            Assert.That(midHandoff.Phase, Is.EqualTo(FloodIngressPatchPhase.Converging));
            Assert.That(midHandoff.HandoffFraction, Is.GreaterThan(0f));
            Assert.That(state.ActivePatchCount, Is.EqualTo(1));

            state.Tick(1f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out _), Is.False);
            Assert.That(state.ActivePatchCount, Is.Zero);

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_HandoffProgressesDuringConvergence()
        {
            var profile = CreateProfile();
            profile.SettlingDurationSeconds = 0f;
            profile.ConvergenceDurationSeconds = 2f;
            var state = new FloodIngressPresentationState(1);
            var provider = CreateProviderId();

            state.Tick(0.1f, new[] { MakeSample(provider, 1f) }, profile, Vector3.up);
            state.Tick(0.01f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var early), Is.True);
            Assert.That(early.Phase, Is.EqualTo(FloodIngressPatchPhase.Converging));

            state.Tick(1f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);
            Assert.That(state.TryGetPatch(provider, out var mid), Is.True);
            Assert.That(mid.HandoffFraction, Is.EqualTo(0.5f).Within(0.05f));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void PresentationState_RespectsMaximumPatchesAndProviderOwnership()
        {
            var profile = CreateProfile();
            profile.MaximumSimultaneousPatches = 2;
            var state = new FloodIngressPresentationState(2);
            var a = CreateProviderId();
            var b = CreateProviderId();
            var c = CreateProviderId();

            state.Tick(
                0.1f,
                new[]
                {
                    MakeSample(a, 1f),
                    MakeSample(b, 1f),
                    MakeSample(c, 0.2f),
                },
                profile,
                Vector3.up);

            Assert.That(state.ActivePatchCount, Is.EqualTo(2));
            Assert.That(state.TryGetPatch(a, out _), Is.True);
            Assert.That(state.TryGetPatch(b, out _), Is.True);
            Assert.That(state.TryGetPatch(c, out _), Is.False);

            // Same providers still update in place — no third spawn.
            state.Tick(
                0.1f,
                new[]
                {
                    MakeSample(a, 1.1f),
                    MakeSample(b, 1.1f),
                },
                profile,
                Vector3.up);
            Assert.That(state.ActivePatchCount, Is.EqualTo(2));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(a));
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(b));
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(c));
        }

        [Test]
        public void PresentationState_DoesNotMutateFloodVolume()
        {
            var root = new GameObject("Volume mutation guard");
            var volume = root.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 2f);
            volume.AddWater(3f);
            var before = volume.CurrentVolume;

            var profile = CreateProfile();
            var state = new FloodIngressPresentationState(2);
            var provider = CreateProviderId();

            state.Tick(
                0.25f,
                new[]
                {
                    new FloodIngressSample(
                        provider,
                        volume,
                        Vector3.zero,
                        Vector3.forward,
                        2f),
                },
                profile,
                Vector3.up);
            state.Tick(0.25f, ReadOnlySpan<FloodIngressSample>.Empty, profile, Vector3.up);

            Assert.That(volume.CurrentVolume, Is.EqualTo(before).Within(Tolerance));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void PresentationState_UsesFloorNormalNotWorldY()
        {
            var profile = CreateProfile();
            var state = new FloodIngressPresentationState(1);
            var provider = CreateProviderId();
            var floorNormal = new Vector3(0.2f, 0.96f, 0f).normalized;

            state.Tick(
                0.1f,
                new[] { MakeSample(provider, 1f) },
                profile,
                floorNormal);

            Assert.That(state.TryGetPatch(provider, out var patch), Is.True);
            Assert.That(
                Vector3.Dot(patch.FloorNormalWorld.normalized, floorNormal),
                Is.EqualTo(1f).Within(0.001f));

            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(Resources.EntityIdToObject(provider));
        }

        [Test]
        public void Profile_EvaluateNormalizedStrength_UsesThresholds()
        {
            var profile = CreateProfile();
            profile.LowFlowThreshold = 0.1f;
            profile.HighFlowThreshold = 1f;

            Assert.That(profile.EvaluateNormalizedStrength(0f), Is.Zero);
            Assert.That(profile.EvaluateNormalizedStrength(1f), Is.EqualTo(1f));
            Assert.That(
                profile.EvaluateNormalizedStrength(0.05f),
                Is.GreaterThan(0f).And.LessThan(1f));

            UnityEngine.Object.DestroyImmediate(profile);
        }

        private static FloodIngressPresentationProfile CreateProfile()
        {
            var profile =
                ScriptableObject.CreateInstance<FloodIngressPresentationProfile>();
            profile.MinimumFlowRate = 0.01f;
            profile.LocalSpreadSpeed = 1f;
            profile.MaximumLocalRadius = 4f;
            profile.SettlingDurationSeconds = 0.25f;
            profile.ConvergenceDurationSeconds = 1f;
            profile.MaximumSimultaneousPatches = 8;
            profile.LowFlowThreshold = 0.1f;
            profile.HighFlowThreshold = 2f;
            return profile;
        }

        private static EntityId CreateProviderId()
        {
            return new GameObject("IngressProvider").GetEntityId();
        }

        private static FloodIngressSample MakeSample(EntityId providerId, float rate)
        {
            return new FloodIngressSample(
                providerId,
                null,
                Vector3.zero,
                Vector3.forward,
                rate);
        }

        private static ConnectionSetup CreateConnectionSetup()
        {
            var root = new GameObject("Ingress connection setup");
            var volumeAObject = new GameObject("A");
            volumeAObject.transform.SetParent(root.transform, false);
            var volumeA = volumeAObject.AddComponent<FloodVolume>();
            volumeA.ConfigureRectangularGeometry(3f, 3f, 2f);

            var volumeBObject = new GameObject("B");
            volumeBObject.transform.SetParent(root.transform, false);
            var volumeB = volumeBObject.AddComponent<FloodVolume>();
            volumeB.ConfigureRectangularGeometry(3f, 3f, 2f);

            var connectionObject = new GameObject("Connection");
            connectionObject.transform.SetParent(root.transform, false);
            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.VolumeA = volumeA;
            connection.VolumeB = volumeB;
            connection.OpeningWidth = 1f;
            connection.OpeningHeight = 2f;

            return new ConnectionSetup(root, volumeA, volumeB, connection);
        }

        private readonly struct ConnectionSetup
        {
            public ConnectionSetup(
                GameObject root,
                FloodVolume volumeA,
                FloodVolume volumeB,
                FloodConnection connection)
            {
                Root = root;
                VolumeA = volumeA;
                VolumeB = volumeB;
                Connection = connection;
            }

            public GameObject Root { get; }

            public FloodVolume VolumeA { get; }

            public FloodVolume VolumeB { get; }

            public FloodConnection Connection { get; }
        }
    }
}
