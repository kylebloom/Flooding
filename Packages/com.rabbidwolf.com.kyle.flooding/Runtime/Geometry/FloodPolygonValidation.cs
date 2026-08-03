using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Authored polygon winding in local XZ coordinates.
    /// </summary>
    public enum FloodPolygonWinding
    {
        Clockwise,
        CounterClockwise,
    }

    /// <summary>
    /// Deterministic validation for one simple polygon footprint.
    /// </summary>
    public static class FloodPolygonValidation
    {
        /// <summary>
        /// Validates finite vertices, area, duplicate points, and self-intersections.
        /// Both windings are accepted and reported; geometry normalizes to counter-clockwise.
        /// </summary>
        public static bool TryValidate(
            IReadOnlyList<Vector2> footprint,
            out FloodPolygonWinding winding,
            out string message)
        {
            winding = FloodPolygonWinding.CounterClockwise;

            if (footprint == null)
            {
                message = "Polygon footprint is missing.";
                return false;
            }

            if (footprint.Count < 3)
            {
                message = "Polygon footprint requires at least three points.";
                return false;
            }

            for (var index = 0; index < footprint.Count; index++)
            {
                var point = footprint[index];

                if (!IsFinite(point.x) || !IsFinite(point.y))
                {
                    message =
                        $"Polygon point {index} must contain finite X and Z values.";
                    return false;
                }
            }

            for (var first = 0; first < footprint.Count; first++)
            {
                for (var second = first + 1;
                     second < footprint.Count;
                     second++)
                {
                    if (SquaredDistance(
                            footprint[first],
                            footprint[second])
                        <= FloodGeometryTolerances.Position
                            * FloodGeometryTolerances.Position)
                    {
                        message =
                            $"Polygon points {first} and {second} overlap. "
                            + "Remove one point or move it farther away.";
                        return false;
                    }
                }
            }

            var signedArea = CalculateSignedArea(footprint);

            for (var firstEdge = 0;
                 firstEdge < footprint.Count;
                 firstEdge++)
            {
                var firstNext = (firstEdge + 1) % footprint.Count;

                for (var secondEdge = firstEdge + 1;
                     secondEdge < footprint.Count;
                     secondEdge++)
                {
                    var secondNext = (secondEdge + 1) % footprint.Count;

                    if (firstEdge == secondEdge
                        || firstNext == secondEdge
                        || secondNext == firstEdge)
                    {
                        continue;
                    }

                    if (SegmentsIntersect(
                            footprint[firstEdge],
                            footprint[firstNext],
                            footprint[secondEdge],
                            footprint[secondNext]))
                    {
                        message =
                            $"Polygon edges {firstEdge} and {secondEdge} "
                            + "intersect. Reorder or move the footprint points "
                            + "to form one simple perimeter.";
                        return false;
                    }
                }
            }

            if (Math.Abs(signedArea) < FloodGeometryTolerances.MinimumArea)
            {
                message =
                    "Polygon footprint has no usable area. Ensure its points "
                    + "do not all lie on one line.";
                return false;
            }

            winding = signedArea > 0d
                ? FloodPolygonWinding.CounterClockwise
                : FloodPolygonWinding.Clockwise;
            message = string.Empty;
            return true;
        }

        internal static double CalculateSignedArea(
            IReadOnlyList<Vector2> footprint)
        {
            var twiceArea = 0d;

            for (var index = 0; index < footprint.Count; index++)
            {
                var next = (index + 1) % footprint.Count;
                twiceArea +=
                    ((double)footprint[index].x * footprint[next].y)
                    - ((double)footprint[next].x * footprint[index].y);
            }

            return twiceArea * 0.5d;
        }

        private static bool SegmentsIntersect(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            var firstSideStart = Cross(
                firstStart,
                firstEnd,
                secondStart);
            var firstSideEnd = Cross(
                firstStart,
                firstEnd,
                secondEnd);
            var secondSideStart = Cross(
                secondStart,
                secondEnd,
                firstStart);
            var secondSideEnd = Cross(
                secondStart,
                secondEnd,
                firstEnd);
            var tolerance = FloodGeometryTolerances.Position;

            if (((firstSideStart > tolerance && firstSideEnd < -tolerance)
                    || (firstSideStart < -tolerance
                        && firstSideEnd > tolerance))
                && ((secondSideStart > tolerance
                        && secondSideEnd < -tolerance)
                    || (secondSideStart < -tolerance
                        && secondSideEnd > tolerance)))
            {
                return true;
            }

            return Math.Abs(firstSideStart) <= tolerance
                    && IsOnSegment(firstStart, firstEnd, secondStart)
                || Math.Abs(firstSideEnd) <= tolerance
                    && IsOnSegment(firstStart, firstEnd, secondEnd)
                || Math.Abs(secondSideStart) <= tolerance
                    && IsOnSegment(secondStart, secondEnd, firstStart)
                || Math.Abs(secondSideEnd) <= tolerance
                    && IsOnSegment(secondStart, secondEnd, firstEnd);
        }

        private static bool IsOnSegment(
            Vector2 start,
            Vector2 end,
            Vector2 point)
        {
            var tolerance = (float)FloodGeometryTolerances.Position;

            return point.x >= Math.Min(start.x, end.x) - tolerance
                && point.x <= Math.Max(start.x, end.x) + tolerance
                && point.y >= Math.Min(start.y, end.y) - tolerance
                && point.y <= Math.Max(start.y, end.y) + tolerance;
        }

        private static double Cross(
            Vector2 first,
            Vector2 second,
            Vector2 point)
        {
            return ((double)second.x - first.x)
                    * ((double)point.y - first.y)
                - ((double)second.y - first.y)
                    * ((double)point.x - first.x);
        }

        private static double SquaredDistance(
            Vector2 first,
            Vector2 second)
        {
            var x = (double)first.x - second.x;
            var y = (double)first.y - second.y;
            return (x * x) + (y * y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
