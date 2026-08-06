using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Deterministically solves a surface-plane offset from authoritative volume.
    /// </summary>
    public static class FloodSurfaceSolver
    {
        /// <summary>
        /// Solves a local-space plane whose negative half-space contains the
        /// requested volume.
        /// </summary>
        public static FloodSurfaceSolution Solve(
            IFloodVolumeGeometry geometry,
            Vector3 localSurfaceNormal,
            double targetVolume)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (!IsFinite(localSurfaceNormal)
                || localSurfaceNormal.sqrMagnitude
                    <= FloodGeometryTolerances.PlaneNormal
                        * FloodGeometryTolerances.PlaneNormal)
            {
                throw new ArgumentException(
                    "Surface normal must be finite and non-zero.",
                    nameof(localSurfaceNormal));
            }
            if (!double.IsFinite(targetVolume))
                throw new ArgumentOutOfRangeException(nameof(targetVolume));

            localSurfaceNormal.Normalize();
            targetVolume = Math.Max(
                0d,
                Math.Min(geometry.Capacity, targetVolume));

            GetProjectionBounds(
                geometry.LocalBounds,
                localSurfaceNormal,
                out var minimumOffset,
                out var maximumOffset);

            var volumeTolerance = Math.Max(
                FloodGeometryTolerances.SolverAbsoluteVolume,
                geometry.Capacity
                    * FloodGeometryTolerances.SolverRelativeVolume);

            if (targetVolume <= volumeTolerance)
            {
                return CreateSolution(
                    geometry,
                    localSurfaceNormal,
                    minimumOffset,
                    targetVolume,
                    iterations: 0);
            }

            if (targetVolume >= geometry.Capacity - volumeTolerance)
            {
                return CreateSolution(
                    geometry,
                    localSurfaceNormal,
                    maximumOffset,
                    targetVolume,
                    iterations: 0);
            }

            var lowerOffset = minimumOffset;
            var upperOffset = maximumOffset;
            var solvedOffset = (lowerOffset + upperOffset) * 0.5d;
            var iterations = 0;

            for (var iteration = 0;
                 iteration < FloodGeometryTolerances.SolverMaximumIterations;
                 iteration++)
            {
                iterations = iteration + 1;
                solvedOffset = (lowerOffset + upperOffset) * 0.5d;
                var plane = CreatePlane(localSurfaceNormal, solvedOffset);
                var submergedVolume =
                    geometry.CalculateSubmergedVolume(plane);
                var error = submergedVolume - targetVolume;

                if (Math.Abs(error) <= volumeTolerance
                    || upperOffset - lowerOffset
                        <= FloodGeometryTolerances.SolverPlanePosition)
                {
                    break;
                }

                if (error < 0d)
                    lowerOffset = solvedOffset;
                else
                    upperOffset = solvedOffset;
            }

            return CreateSolution(
                geometry,
                localSurfaceNormal,
                solvedOffset,
                targetVolume,
                iterations);
        }

        private static FloodSurfaceSolution CreateSolution(
            IFloodVolumeGeometry geometry,
            Vector3 normal,
            double offset,
            double targetVolume,
            int iterations)
        {
            var plane = CreatePlane(normal, offset);

            // Occupancy quantity solves do not need free-surface contours; those
            // are rebuilt by presentation components when they apply state.
            var submersion = geometry is BakedFloodGeometry baked
                ? baked.EvaluateQuantities(plane)
                : geometry.EvaluateSubmersion(plane);

            return new FloodSurfaceSolution(
                plane,
                submersion,
                targetVolume,
                submersion.Volume - targetVolume,
                iterations);
        }

        private static Plane CreatePlane(Vector3 normal, double offset)
        {
            return new Plane(normal, normal * (float)offset);
        }

        private static void GetProjectionBounds(
            Bounds bounds,
            Vector3 normal,
            out double minimum,
            out double maximum)
        {
            var min = bounds.min;
            var max = bounds.max;
            minimum = double.PositiveInfinity;
            maximum = double.NegativeInfinity;

            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        var corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        var projection = Vector3.Dot(normal, corner);
                        minimum = Math.Min(minimum, projection);
                        maximum = Math.Max(maximum, projection);
                    }
                }
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsNaN(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.x)
                && !float.IsInfinity(value.y)
                && !float.IsInfinity(value.z);
        }
    }

    /// <summary>
    /// Immutable diagnostics from a bounded water-surface solve.
    /// </summary>
    public readonly struct FloodSurfaceSolution
    {
        internal FloodSurfaceSolution(
            Plane localSurfacePlane,
            FloodSubmersionResult submersion,
            double targetVolume,
            double volumeError,
            int iterations)
        {
            LocalSurfacePlane = localSurfacePlane;
            Submersion = submersion;
            TargetVolume = targetVolume;
            VolumeError = volumeError;
            Iterations = iterations;
        }

        /// <summary>Gets the solved local-space surface plane.</summary>
        public Plane LocalSurfacePlane { get; }

        /// <summary>Gets geometry evaluated at the solved plane.</summary>
        public FloodSubmersionResult Submersion { get; }

        /// <summary>Gets the requested authoritative volume.</summary>
        public double TargetVolume { get; }

        /// <summary>Gets evaluated volume minus requested volume.</summary>
        public double VolumeError { get; }

        /// <summary>Gets the bounded binary-search iteration count.</summary>
        public int Iterations { get; }
    }
}
