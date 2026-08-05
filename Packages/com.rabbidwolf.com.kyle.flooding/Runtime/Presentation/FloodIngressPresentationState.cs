using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Deterministic, allocation-free local ingress patch lifecycle.
    /// </summary>
    /// <remarks>
    /// Presentation-only. Never mutates <see cref="FloodVolume"/> or solver
    /// state. One patch is owned per provider; patches are never geometrically
    /// merged across providers.
    /// </remarks>
    public sealed class FloodIngressPresentationState
    {
        private FloodIngressPatchState[] patches;
        private bool[] seenThisTick;
        private int capacity;

        /// <summary>
        /// Creates presentation state with the given maximum patch count.
        /// </summary>
        public FloodIngressPresentationState(int maximumPatches)
        {
            capacity = Math.Max(1, maximumPatches);
            patches = new FloodIngressPatchState[capacity];
            seenThisTick = new bool[capacity];
        }

        /// <summary>
        /// Gets the fixed patch capacity.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Gets a read-only view of patch slots.
        /// </summary>
        public ReadOnlySpan<FloodIngressPatchState> Patches => patches;

        /// <summary>
        /// Gets the number of non-inactive patches.
        /// </summary>
        public int ActivePatchCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < patches.Length; i++)
                {
                    if (patches[i].IsActive)
                        count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the age in seconds of the oldest active patch, or zero.
        /// </summary>
        public float OldestPatchAgeSeconds
        {
            get
            {
                var oldest = 0f;
                for (var i = 0; i < patches.Length; i++)
                {
                    if (patches[i].IsActive)
                        oldest = Math.Max(oldest, patches[i].AgeSeconds);
                }

                return oldest;
            }
        }

        /// <summary>
        /// Gets the average handoff fraction across active patches, or 1 when
        /// none are active.
        /// </summary>
        public float AverageHandoffFraction
        {
            get
            {
                var sum = 0f;
                var count = 0;
                for (var i = 0; i < patches.Length; i++)
                {
                    if (!patches[i].IsActive)
                        continue;

                    sum += patches[i].HandoffFraction;
                    count++;
                }

                return count == 0 ? 1f : sum / count;
            }
        }

        /// <summary>
        /// Ensures capacity matches the profile maximum, preserving existing
        /// active patches when growing and dropping trailing inactive slots
        /// when shrinking.
        /// </summary>
        public void EnsureCapacity(int maximumPatches)
        {
            var requested = Math.Max(1, maximumPatches);
            if (requested == capacity)
                return;

            var next = new FloodIngressPatchState[requested];
            var copy = Math.Min(requested, patches.Length);
            Array.Copy(patches, next, copy);
            patches = next;
            seenThisTick = new bool[requested];
            capacity = requested;
        }

        /// <summary>
        /// Advances patch lifecycle using the current ingress samples.
        /// </summary>
        /// <param name="deltaTime">Frame or fixed step seconds.</param>
        /// <param name="samples">
        /// Ingress samples for this destination. Providers absent from this
        /// span begin Settling.
        /// </param>
        /// <param name="profile">Presentation timing and response curves.</param>
        /// <param name="floorNormalWorld">
        /// Unit floor normal used to orient patches. Defaults to world up when
        /// near-zero.
        /// </param>
        public void Tick(
            float deltaTime,
            ReadOnlySpan<FloodIngressSample> samples,
            FloodIngressPresentationProfile profile,
            Vector3 floorNormalWorld)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                deltaTime = 0f;

            EnsureCapacity(profile.MaximumSimultaneousPatches);

            if (floorNormalWorld.sqrMagnitude <= 0.0001f)
                floorNormalWorld = Vector3.up;
            else
                floorNormalWorld = floorNormalWorld.normalized;

            var seen = seenThisTick;
            for (var i = 0; i < patches.Length; i++)
                seen[i] = false;

            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var sample = samples[sampleIndex];
                if (sample.FlowRateCubicMetersPerSecond < profile.MinimumFlowRate)
                    continue;

                if (!Resources.EntityIdIsValid(sample.ProviderId))
                    continue;

                var existing = FindPatchIndex(sample.ProviderId);
                if (existing >= 0)
                {
                    ApplyGrowingSample(
                        ref patches[existing],
                        sample,
                        profile,
                        floorNormalWorld,
                        deltaTime);
                    seen[existing] = true;
                    continue;
                }

                if (!TryAllocateSlot(
                        sample,
                        profile,
                        out var slot))
                {
                    continue;
                }

                patches[slot] = CreateGrowingPatch(
                    sample,
                    profile,
                    floorNormalWorld);
                ApplyGrowingSample(
                    ref patches[slot],
                    sample,
                    profile,
                    floorNormalWorld,
                    deltaTime);
                seen[slot] = true;
            }

            for (var i = 0; i < patches.Length; i++)
            {
                if (!patches[i].IsActive)
                    continue;

                if (!seen[i]
                    && patches[i].Phase == FloodIngressPatchPhase.Growing)
                {
                    BeginSettling(ref patches[i]);
                }

                AdvancePhase(ref patches[i], profile, deltaTime);
            }
        }

        /// <summary>
        /// Clears all patches to inactive.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < patches.Length; i++)
                patches[i] = default;
        }

        /// <summary>
        /// Tries to read the patch owned by a provider.
        /// </summary>
        public bool TryGetPatch(EntityId providerId, out FloodIngressPatchState patch)
        {
            var index = FindPatchIndex(providerId);
            if (index < 0)
            {
                patch = default;
                return false;
            }

            patch = patches[index];
            return true;
        }

        private int FindPatchIndex(EntityId providerId)
        {
            for (var i = 0; i < patches.Length; i++)
            {
                if (patches[i].IsActive && patches[i].ProviderId.Equals(providerId))
                    return i;
            }

            return -1;
        }

        private bool TryAllocateSlot(
            FloodIngressSample sample,
            FloodIngressPresentationProfile profile,
            out int slot)
        {
            for (var i = 0; i < patches.Length; i++)
            {
                if (patches[i].Phase == FloodIngressPatchPhase.Inactive)
                {
                    slot = i;
                    return true;
                }
            }

            var weakestConverging = -1;
            var weakestConvergingScore = float.MaxValue;
            for (var i = 0; i < patches.Length; i++)
            {
                if (patches[i].Phase != FloodIngressPatchPhase.Converging)
                    continue;

                var score = ScorePatch(patches[i]);
                if (score < weakestConvergingScore)
                {
                    weakestConvergingScore = score;
                    weakestConverging = i;
                }
            }

            if (weakestConverging >= 0)
            {
                slot = weakestConverging;
                return true;
            }

            // Ignore the new sample when it is weaker than every occupied slot.
            var newScore = ScoreSample(sample, profile);
            var weakestOccupied = -1;
            var weakestOccupiedScore = float.MaxValue;
            for (var i = 0; i < patches.Length; i++)
            {
                var score = ScorePatch(patches[i]);
                if (score < weakestOccupiedScore)
                {
                    weakestOccupiedScore = score;
                    weakestOccupied = i;
                }
            }

            if (weakestOccupied >= 0 && newScore > weakestOccupiedScore)
            {
                // Prefer not to steal Growing/Settling ownership; only ignore.
                slot = -1;
                return false;
            }

            slot = -1;
            return false;
        }

        private static FloodIngressPatchState CreateGrowingPatch(
            FloodIngressSample sample,
            FloodIngressPresentationProfile profile,
            Vector3 floorNormalWorld)
        {
            var direction = sample.DirectionWorld.sqrMagnitude > 0.0001f
                ? sample.DirectionWorld.normalized
                : Vector3.forward;
            var spreadDirection = ProjectOntoFloor(direction, floorNormalWorld);
            return new FloodIngressPatchState
            {
                ProviderId = sample.ProviderId,
                CenterWorld = sample.WorldPosition,
                FloorNormalWorld = floorNormalWorld,
                DirectionWorld = direction,
                SpreadDirectionWorld = spreadDirection,
                CurrentRadius = 0.05f,
                TargetRadius = 0.05f,
                VisualDepth = profile.InitialPoolDepth,
                AgeSeconds = 0f,
                PhaseAgeSeconds = 0f,
                Phase = FloodIngressPatchPhase.Growing,
                HandoffFraction = 0f,
                FlowImpulse = 0f,
                Strength = 0f,
                FlowRateCubicMetersPerSecond = sample.FlowRateCubicMetersPerSecond,
                DirectionalStretch = profile.DirectionalStretch,
                MajorRadius = 0.05f,
                MinorRadius = 0.05f,
            };
        }

        private static void ApplyGrowingSample(
            ref FloodIngressPatchState patch,
            FloodIngressSample sample,
            FloodIngressPresentationProfile profile,
            Vector3 floorNormalWorld,
            float deltaTime)
        {
            patch.Phase = FloodIngressPatchPhase.Growing;
            patch.PhaseAgeSeconds = 0f;
            patch.HandoffFraction = 0f;
            patch.CenterWorld = sample.WorldPosition;
            patch.FloorNormalWorld = floorNormalWorld;
            patch.DirectionWorld = sample.DirectionWorld.sqrMagnitude > 0.0001f
                ? sample.DirectionWorld.normalized
                : patch.DirectionWorld;
            patch.SpreadDirectionWorld = ProjectOntoFloor(
                patch.DirectionWorld,
                floorNormalWorld);
            patch.FlowRateCubicMetersPerSecond = sample.FlowRateCubicMetersPerSecond;
            patch.Strength = profile.EvaluateNormalizedStrength(
                sample.FlowRateCubicMetersPerSecond);
            patch.VisualDepth = profile.InitialPoolDepth;
            patch.DirectionalStretch = Mathf.Max(
                patch.DirectionalStretch,
                profile.DirectionalStretch * Mathf.Lerp(0.35f, 1f, patch.Strength));

            // Presentation-only impulse (not conserved cubic meters).
            patch.FlowImpulse += Mathf.Max(
                0f,
                sample.FlowRateCubicMetersPerSecond) * deltaTime;

            var spreadMultiplier = profile.EvaluateSpreadSpeedMultiplier(
                sample.FlowRateCubicMetersPerSecond);
            var desired = Mathf.Min(
                profile.MaximumLocalRadius,
                0.15f + (patch.FlowImpulse * 0.85f * Mathf.Max(0.01f, spreadMultiplier)));
            patch.TargetRadius = Mathf.Max(patch.TargetRadius, desired);
            UpdateAxisRadii(ref patch);
        }

        private static void BeginSettling(ref FloodIngressPatchState patch)
        {
            patch.Phase = FloodIngressPatchPhase.Settling;
            patch.PhaseAgeSeconds = 0f;
            patch.FlowRateCubicMetersPerSecond = 0f;
        }

        private static void AdvancePhase(
            ref FloodIngressPatchState patch,
            FloodIngressPresentationProfile profile,
            float deltaTime)
        {
            patch.AgeSeconds += deltaTime;
            patch.PhaseAgeSeconds += deltaTime;

            var spreadSpeed = profile.LocalSpreadSpeed;
            if (patch.Phase == FloodIngressPatchPhase.Growing)
            {
                spreadSpeed *= profile.EvaluateSpreadSpeedMultiplier(
                    patch.FlowRateCubicMetersPerSecond);
            }
            else
            {
                // After stop, allow residual expansion toward the last target.
                spreadSpeed *= 0.35f;
            }

            patch.CurrentRadius = MoveTowards(
                patch.CurrentRadius,
                patch.TargetRadius,
                spreadSpeed * deltaTime);

            switch (patch.Phase)
            {
                case FloodIngressPatchPhase.Growing:
                    patch.HandoffFraction = 0f;
                    break;

                case FloodIngressPatchPhase.Settling:
                    patch.DirectionalStretch = MoveTowards(
                        patch.DirectionalStretch,
                        0f,
                        profile.DirectionalRelaxation * deltaTime);
                    if (patch.PhaseAgeSeconds >= profile.SettlingDurationSeconds)
                    {
                        patch.Phase = FloodIngressPatchPhase.Converging;
                        patch.PhaseAgeSeconds = 0f;
                    }

                    break;

                case FloodIngressPatchPhase.Converging:
                    {
                        patch.DirectionalStretch = MoveTowards(
                            patch.DirectionalStretch,
                            0f,
                            profile.DirectionalRelaxation * deltaTime);
                        var duration = Mathf.Max(
                            0.01f,
                            profile.ConvergenceDurationSeconds);
                        patch.HandoffFraction = Mathf.Clamp01(
                            patch.PhaseAgeSeconds / duration);
                        if (patch.HandoffFraction >= 1f)
                            patch = default;
                    }

                    break;
            }

            if (patch.IsActive)
                UpdateAxisRadii(ref patch);
        }

        private static void UpdateAxisRadii(ref FloodIngressPatchState patch)
        {
            var stretch = Mathf.Max(0f, patch.DirectionalStretch);
            var radius = Mathf.Max(0.01f, patch.CurrentRadius);
            patch.MajorRadius = radius * (1f + stretch);
            patch.MinorRadius = radius / Mathf.Sqrt(1f + stretch);
        }

        private static Vector3 ProjectOntoFloor(Vector3 direction, Vector3 floorNormal)
        {
            if (floorNormal.sqrMagnitude <= 0.0001f)
                floorNormal = Vector3.up;
            else
                floorNormal = floorNormal.normalized;

            var projected = direction - (Vector3.Dot(direction, floorNormal) * floorNormal);
            if (projected.sqrMagnitude <= 0.0001f)
            {
                var fallback = Vector3.Cross(floorNormal, Vector3.right);
                if (fallback.sqrMagnitude <= 0.0001f)
                    fallback = Vector3.Cross(floorNormal, Vector3.forward);
                return fallback.normalized;
            }

            return projected.normalized;
        }

        private static float ScorePatch(in FloodIngressPatchState patch)
        {
            // Weaker = lower strength, higher handoff, smaller radius.
            return (patch.Strength * 2f)
                + (patch.CurrentRadius * 0.25f)
                + ((1f - patch.HandoffFraction) * 0.5f)
                - (patch.AgeSeconds * 0.01f);
        }

        private static float ScoreSample(
            in FloodIngressSample sample,
            FloodIngressPresentationProfile profile)
        {
            return profile.EvaluateNormalizedStrength(
                sample.FlowRateCubicMetersPerSecond);
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Mathf.Abs(target - current) <= maxDelta)
                return target;

            return current + (Mathf.Sign(target - current) * maxDelta);
        }
    }
}
