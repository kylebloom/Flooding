using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Deterministic centered rectangular prism geometry.
    /// </summary>
    public sealed class RectangularPrismFloodGeometry :
        ExtrudedFloodVolumeGeometry
    {
        /// <summary>
        /// Creates centered rectangular geometry in local XZ, from local Y zero
        /// through the supplied maximum height.
        /// </summary>
        public RectangularPrismFloodGeometry(
            double width,
            double length,
            double maximumHeight)
            : base(
                CreateFootprint(width, length, maximumHeight),
                new[] { 0, 1, 2, 0, 2, 3 },
                width * length,
                maximumHeight,
                Vector2.zero,
                CreateBounds(width, length, maximumHeight))
        {
            Width = width;
            Length = length;
        }

        /// <summary>
        /// Gets local-X width in meters.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Gets local-Z length in meters.
        /// </summary>
        public double Length { get; }

        private static Vector2[] CreateFootprint(
            double width,
            double length,
            double maximumHeight)
        {
            ValidateDimension(width, nameof(width));
            ValidateDimension(length, nameof(length));
            ValidateDimension(maximumHeight, nameof(maximumHeight));

            var halfWidth = (float)(width * 0.5d);
            var halfLength = (float)(length * 0.5d);

            return new[]
            {
                new Vector2(-halfWidth, -halfLength),
                new Vector2(halfWidth, -halfLength),
                new Vector2(halfWidth, halfLength),
                new Vector2(-halfWidth, halfLength),
            };
        }

        private static Bounds CreateBounds(
            double width,
            double length,
            double maximumHeight)
        {
            return new Bounds(
                new Vector3(0f, (float)(maximumHeight * 0.5d), 0f),
                new Vector3(
                    (float)width,
                    (float)maximumHeight,
                    (float)length));
        }

        private static void ValidateDimension(double value, string name)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || value < FloodGeometryTolerances.MinimumDimension)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    $"Dimensions must be finite and at least "
                    + $"{FloodGeometryTolerances.MinimumDimension} meters.");
            }
        }
    }
}
