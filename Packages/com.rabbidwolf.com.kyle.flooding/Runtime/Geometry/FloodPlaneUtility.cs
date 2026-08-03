using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Plane transformations that preserve the represented half-space under
    /// rotation and non-uniform scale.
    /// </summary>
    public static class FloodPlaneUtility
    {
        /// <summary>Transforms a local plane into world space.</summary>
        public static Plane LocalToWorld(
            Transform transform,
            Plane localPlane)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return TransformPlane(localPlane, transform.localToWorldMatrix);
        }

        /// <summary>Transforms a world plane into Transform-local space.</summary>
        public static Plane WorldToLocal(
            Transform transform,
            Plane worldPlane)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return TransformPlane(worldPlane, transform.worldToLocalMatrix);
        }

        /// <summary>
        /// Transforms a world-space plane normal into Transform-local space.
        /// </summary>
        public static Vector3 WorldNormalToLocal(
            Transform transform,
            Vector3 worldNormal)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var localNormal =
                transform.localToWorldMatrix.transpose.MultiplyVector(
                    worldNormal);

            return localNormal.normalized;
        }

        private static Plane TransformPlane(
            Plane sourcePlane,
            Matrix4x4 sourceToDestination)
        {
            var sourceNormal = sourcePlane.normal.normalized;
            var sourcePoint = -sourceNormal * sourcePlane.distance;
            var destinationPoint =
                sourceToDestination.MultiplyPoint3x4(sourcePoint);
            var destinationNormal =
                sourceToDestination.inverse.transpose.MultiplyVector(
                    sourceNormal).normalized;

            return new Plane(destinationNormal, destinationPoint);
        }
    }
}
