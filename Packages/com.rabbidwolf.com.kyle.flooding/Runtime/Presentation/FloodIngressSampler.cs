using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Profile-independent helpers that build factual
    /// <see cref="FloodIngressSample"/> values from solver components.
    /// </summary>
    public static class FloodIngressSampler
    {
        /// <summary>
        /// Tries to sample ingress into <paramref name="forVolume"/> from a
        /// connection's latest applied flow.
        /// </summary>
        /// <remarks>
        /// Positive <see cref="FloodConnection.CurrentFlowRate"/> means flow
        /// A→B (ingress into Side B). Negative means B→A (ingress into Side A).
        /// <see cref="FloodIngressSample.DirectionWorld"/> always points into
        /// the destination volume.
        /// </remarks>
        public static bool TrySample(
            FloodConnection connection,
            FloodVolume forVolume,
            out FloodIngressSample sample)
        {
            sample = default;

            if (connection == null || forVolume == null)
                return false;

            if (!TryResolveConnectionIngress(
                    connection,
                    out var destination,
                    out var directionWorld,
                    out var flowRate))
            {
                return false;
            }

            if (destination != forVolume)
                return false;

            sample = new FloodIngressSample(
                connection.GetEntityId(),
                destination,
                connection.IngressWorldPosition,
                directionWorld,
                flowRate);
            return true;
        }

        /// <summary>
        /// Tries to sample ingress from a connection into whichever finite
        /// volume currently receives applied flow.
        /// </summary>
        public static bool TrySample(
            FloodConnection connection,
            out FloodIngressSample sample)
        {
            sample = default;

            if (connection == null)
                return false;

            if (!TryResolveConnectionIngress(
                    connection,
                    out var destination,
                    out var directionWorld,
                    out var flowRate))
            {
                return false;
            }

            sample = new FloodIngressSample(
                connection.GetEntityId(),
                destination,
                connection.IngressWorldPosition,
                directionWorld,
                flowRate);
            return true;
        }

        /// <summary>
        /// Tries to sample ingress from an active <see cref="FloodSource"/>.
        /// </summary>
        public static bool TrySample(
            FloodSource source,
            out FloodIngressSample sample)
        {
            sample = default;

            if (source == null
                || !source.isActiveAndEnabled
                || !source.IsActive
                || source.Target == null
                || source.FlowRate <= 0f)
            {
                return false;
            }

            var direction = ResolveSourceDirection(source);
            sample = new FloodIngressSample(
                source.GetEntityId(),
                source.Target,
                source.IngressWorldPosition,
                direction,
                source.FlowRate);
            return true;
        }

        /// <summary>
        /// Tries to sample ingress from a source when its target matches
        /// <paramref name="forVolume"/>.
        /// </summary>
        public static bool TrySample(
            FloodSource source,
            FloodVolume forVolume,
            out FloodIngressSample sample)
        {
            if (!TrySample(source, out sample))
                return false;

            if (forVolume != null && sample.DestinationVolume != forVolume)
            {
                sample = default;
                return false;
            }

            return true;
        }

        private static bool TryResolveConnectionIngress(
            FloodConnection connection,
            out FloodVolume destination,
            out Vector3 directionWorld,
            out float flowRate)
        {
            destination = null;
            directionWorld = Vector3.zero;
            flowRate = 0f;

            var signedRate = connection.CurrentFlowRate;
            if (!FloodPresentationUtility.IsFlowing(signedRate))
                return false;

            if (signedRate > 0d)
            {
                destination = connection.VolumeB;
                directionWorld = connection.transform.forward;
            }
            else
            {
                destination = connection.VolumeA;
                directionWorld = -connection.transform.forward;
            }

            if (destination == null)
                return false;

            if (directionWorld.sqrMagnitude <= 0.0001f)
                directionWorld = Vector3.forward;
            else
                directionWorld.Normalize();

            flowRate = (float)Math.Abs(signedRate);
            if (float.IsNaN(flowRate) || float.IsInfinity(flowRate) || flowRate <= 0f)
                return false;

            return true;
        }

        private static Vector3 ResolveSourceDirection(FloodSource source)
        {
            var basis = source.IngressAnchor != null
                ? source.IngressAnchor
                : source.transform;
            var direction = basis.forward;
            if (direction.sqrMagnitude <= 0.0001f)
                return Vector3.down;

            return direction.normalized;
        }
    }
}
