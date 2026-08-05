using System;
using NUnit.Framework;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodFlowCalculatorTests
    {
        private const double Tolerance = 0.000001d;

        [Test]
        public void Calculate_EqualHeadsProduceNoFlow()
        {
            var result = FloodFlowCalculator.Calculate(
                pressureHeadA: 2d,
                pressureHeadB: 2d,
                openingWidth: 1d,
                openingHeight: 1d,
                dischargeCoefficient: 0.62d,
                gravityMagnitude: 9.81d);

            Assert.That(result.SignedFlowRate, Is.Zero.Within(Tolerance));
            Assert.That(result.PressureHeadDifference, Is.Zero.Within(Tolerance));
            Assert.That(result.SubmergedOpeningArea, Is.EqualTo(1d).Within(Tolerance));
            Assert.That(result.IsFlowing, Is.False);
        }

        [Test]
        public void Calculate_UsesCentroidHeadForOrificeFlow()
        {
            var result = FloodFlowCalculator.Calculate(
                pressureHeadA: 1d,
                pressureHeadB: 0d,
                openingWidth: 2d,
                openingHeight: 0.5d,
                dischargeCoefficient: 0.62d,
                gravityMagnitude: 9.81d);

            // Submerged height 0.5 m → centroid 0.25 m above opening bottom.
            var expectedHeadDifference = 0.75d;
            var expectedFlow =
                0.62d * 1d * Math.Sqrt(2d * 9.81d * expectedHeadDifference);

            Assert.That(
                result.SignedFlowRate,
                Is.EqualTo(expectedFlow).Within(Tolerance));
            Assert.That(
                result.SubmergedOpeningArea,
                Is.EqualTo(1d).Within(Tolerance));
            Assert.That(
                result.PressureHeadDifference,
                Is.EqualTo(expectedHeadDifference).Within(Tolerance));
        }

        [Test]
        public void Calculate_ReversesWhenSideBHasGreaterHead()
        {
            var forward = FloodFlowCalculator.Calculate(
                1d,
                0d,
                1d,
                1d,
                0.62d,
                9.81d);
            var reverse = FloodFlowCalculator.Calculate(
                0d,
                1d,
                1d,
                1d,
                0.62d,
                9.81d);

            Assert.That(
                reverse.SignedFlowRate,
                Is.EqualTo(-forward.SignedFlowRate).Within(Tolerance));
            Assert.That(
                reverse.PressureHeadDifference,
                Is.EqualTo(-forward.PressureHeadDifference).Within(Tolerance));
        }

        [Test]
        public void Calculate_UsesPartiallySubmergedOpeningArea()
        {
            var result = FloodFlowCalculator.Calculate(
                pressureHeadA: 0.25d,
                pressureHeadB: 0d,
                openingWidth: 2d,
                openingHeight: 1d,
                dischargeCoefficient: 0.62d,
                gravityMagnitude: 9.81d);

            Assert.That(
                result.SubmergedOpeningArea,
                Is.EqualTo(0.5d).Within(Tolerance));
            Assert.That(
                result.PressureHeadDifference,
                Is.EqualTo(0.125d).Within(Tolerance));
        }

        [Test]
        public void Calculate_DryOpeningProducesNoFlow()
        {
            var result = FloodFlowCalculator.Calculate(
                pressureHeadA: -1d,
                pressureHeadB: -2d,
                openingWidth: 1d,
                openingHeight: 1d,
                dischargeCoefficient: 0.62d,
                gravityMagnitude: 9.81d);

            Assert.That(result.SignedFlowRate, Is.Zero.Within(Tolerance));
            Assert.That(result.SubmergedOpeningArea, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void Calculate_HeadWithinToleranceProducesNoFlow()
        {
            var result = FloodFlowCalculator.Calculate(
                pressureHeadA: FloodFluidTolerances.PressureHead * 0.5d,
                pressureHeadB: 0d,
                openingWidth: 1d,
                openingHeight: 1d,
                dischargeCoefficient: 0.62d,
                gravityMagnitude: 9.81d);

            Assert.That(result.SignedFlowRate, Is.Zero.Within(Tolerance));
            Assert.That(result.IsFlowing, Is.False);
        }

        [TestCase(-0.1d)]
        [TestCase(1.1d)]
        [TestCase(double.NaN)]
        public void Calculate_RejectsInvalidDischargeCoefficient(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => FloodFlowCalculator.Calculate(
                    1d,
                    0d,
                    1d,
                    1d,
                    value,
                    9.81d));
        }

        [Test]
        public void Calculate_OpenFractionOne_PreservesExistingBehavior()
        {
            var withoutFraction = FloodFlowCalculator.Calculate(
                1d,
                0d,
                2d,
                0.5d,
                0.62d,
                9.81d);
            var withFraction = FloodFlowCalculator.Calculate(
                1d,
                0d,
                2d,
                0.5d,
                0.62d,
                9.81d,
                openFraction: 1d);

            Assert.That(
                withFraction.SignedFlowRate,
                Is.EqualTo(withoutFraction.SignedFlowRate).Within(Tolerance));
            Assert.That(
                withFraction.SubmergedOpeningArea,
                Is.EqualTo(withoutFraction.SubmergedOpeningArea)
                    .Within(Tolerance));
            Assert.That(
                withFraction.PressureHeadDifference,
                Is.EqualTo(withoutFraction.PressureHeadDifference)
                    .Within(Tolerance));
        }

        [Test]
        public void Calculate_OpenFractionZero_ProducesZeroFlowAndArea()
        {
            var result = FloodFlowCalculator.Calculate(
                1d,
                0d,
                2d,
                1d,
                0.62d,
                9.81d,
                openFraction: 0d);

            Assert.That(result.SignedFlowRate, Is.Zero.Within(Tolerance));
            Assert.That(result.SubmergedOpeningArea, Is.Zero.Within(Tolerance));
            Assert.That(result.IsFlowing, Is.False);
        }

        [Test]
        public void Calculate_OpenFractionHalf_HalvesFlowAndEffectiveArea()
        {
            var full = FloodFlowCalculator.Calculate(
                1d,
                0d,
                2d,
                0.5d,
                0.62d,
                9.81d,
                openFraction: 1d);
            var half = FloodFlowCalculator.Calculate(
                1d,
                0d,
                2d,
                0.5d,
                0.62d,
                9.81d,
                openFraction: 0.5d);

            Assert.That(
                half.SignedFlowRate,
                Is.EqualTo(full.SignedFlowRate * 0.5d).Within(Tolerance));
            Assert.That(
                half.SubmergedOpeningArea,
                Is.EqualTo(full.SubmergedOpeningArea * 0.5d).Within(Tolerance));
            Assert.That(
                half.PressureHeadDifference,
                Is.EqualTo(full.PressureHeadDifference).Within(Tolerance));
        }

        [Test]
        public void Calculate_OpenFraction_AppliesToReverseFlow()
        {
            var full = FloodFlowCalculator.Calculate(
                0d,
                1d,
                1d,
                1d,
                0.62d,
                9.81d,
                openFraction: 1d);
            var half = FloodFlowCalculator.Calculate(
                0d,
                1d,
                1d,
                1d,
                0.62d,
                9.81d,
                openFraction: 0.5d);

            Assert.That(full.SignedFlowRate, Is.LessThan(0d));
            Assert.That(
                half.SignedFlowRate,
                Is.EqualTo(full.SignedFlowRate * 0.5d).Within(Tolerance));
        }

        [TestCase(-0.1d)]
        [TestCase(1.1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Calculate_RejectsInvalidOpenFraction(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => FloodFlowCalculator.Calculate(
                    1d,
                    0d,
                    1d,
                    1d,
                    0.62d,
                    9.81d,
                    openFraction: value));
        }
    }
}
