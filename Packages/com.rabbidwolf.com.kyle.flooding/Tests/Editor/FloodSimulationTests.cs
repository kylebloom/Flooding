using System;
using NUnit.Framework;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodSimulationTests
    {
        private const double Tolerance = 0.000001d;

        [Test]
        public void Constructor_CalculatesCapacityAndDerivedState()
        {
            var simulation = new FloodSimulation(
                floorArea: 12d,
                maximumHeight: 3d,
                initialVolume: 9d);

            Assert.That(simulation.MaximumVolume, Is.EqualTo(36d).Within(Tolerance));
            Assert.That(simulation.CurrentVolume, Is.EqualTo(9d).Within(Tolerance));
            Assert.That(simulation.CurrentHeight, Is.EqualTo(0.75d).Within(Tolerance));
            Assert.That(simulation.FillPercentage, Is.EqualTo(0.25d).Within(Tolerance));
            Assert.That(simulation.IsEmpty, Is.False);
            Assert.That(simulation.IsFull, Is.False);
        }

        [TestCase(0d, 1d)]
        [TestCase(-1d, 1d)]
        [TestCase(1d, 0d)]
        [TestCase(1d, -1d)]
        public void Constructor_RejectsNonPositiveDimensions(
            double floorArea,
            double maximumHeight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FloodSimulation(floorArea, maximumHeight));
        }

        [TestCase(-5d, 0d)]
        [TestCase(5d, 5d)]
        [TestCase(25d, 20d)]
        public void Constructor_ClampsInitialVolume(
            double initialVolume,
            double expectedVolume)
        {
            var simulation = new FloodSimulation(
                floorArea: 10d,
                maximumHeight: 2d,
                initialVolume: initialVolume);

            Assert.That(
                simulation.CurrentVolume,
                Is.EqualTo(expectedVolume).Within(Tolerance));
        }

        [Test]
        public void AddVolume_ClampsAtCapacity()
        {
            var simulation = new FloodSimulation(10d, 2d, 18d);

            var result = simulation.AddVolume(5d);

            Assert.That(simulation.CurrentVolume, Is.EqualTo(20d).Within(Tolerance));
            Assert.That(simulation.IsFull, Is.True);
            Assert.That(result.RequestedChange, Is.EqualTo(5d).Within(Tolerance));
            Assert.That(result.AppliedChange, Is.EqualTo(2d).Within(Tolerance));
            Assert.That(result.RejectedVolume, Is.EqualTo(3d).Within(Tolerance));
            Assert.That(result.PreviousVolume, Is.EqualTo(18d).Within(Tolerance));
            Assert.That(result.CurrentVolume, Is.EqualTo(20d).Within(Tolerance));
            Assert.That(result.Changed, Is.True);
        }

        [Test]
        public void RemoveVolume_ClampsAtZero()
        {
            var simulation = new FloodSimulation(10d, 2d, 2d);

            var result = simulation.RemoveVolume(5d);

            Assert.That(simulation.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(simulation.IsEmpty, Is.True);
            Assert.That(result.RequestedChange, Is.EqualTo(-5d).Within(Tolerance));
            Assert.That(result.AppliedChange, Is.EqualTo(-2d).Within(Tolerance));
            Assert.That(result.RejectedVolume, Is.EqualTo(3d).Within(Tolerance));
        }

        [Test]
        public void SetVolume_ClampsToValidRange()
        {
            var simulation = new FloodSimulation(10d, 2d);

            var fillResult = simulation.SetVolume(25d);
            Assert.That(simulation.CurrentVolume, Is.EqualTo(20d).Within(Tolerance));
            Assert.That(fillResult.AppliedChange, Is.EqualTo(20d).Within(Tolerance));
            Assert.That(fillResult.RejectedVolume, Is.EqualTo(5d).Within(Tolerance));

            var emptyResult = simulation.SetVolume(-1d);
            Assert.That(simulation.CurrentVolume, Is.Zero.Within(Tolerance));
            Assert.That(emptyResult.RequestedChange, Is.EqualTo(-21d).Within(Tolerance));
            Assert.That(emptyResult.AppliedChange, Is.EqualTo(-20d).Within(Tolerance));
            Assert.That(emptyResult.RejectedVolume, Is.EqualTo(1d).Within(Tolerance));
        }

        [Test]
        public void Step_AppliesNetFlowOverTime()
        {
            var simulation = new FloodSimulation(10d, 2d, 4d);

            var result = simulation.Step(
                deltaTime: 2d,
                inflowRate: 3d,
                outflowRate: 1d);

            Assert.That(simulation.CurrentVolume, Is.EqualTo(8d).Within(Tolerance));
            Assert.That(result.RequestedChange, Is.EqualTo(4d).Within(Tolerance));
            Assert.That(result.AppliedChange, Is.EqualTo(4d).Within(Tolerance));
            Assert.That(result.RejectedVolume, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void Step_ClampsNetFlowToCapacity()
        {
            var simulation = new FloodSimulation(10d, 2d, 19d);

            simulation.Step(
                deltaTime: 1d,
                inflowRate: 5d,
                outflowRate: 0d);

            Assert.That(simulation.CurrentVolume, Is.EqualTo(20d).Within(Tolerance));
        }

        [Test]
        public void NonPositiveMutations_DoNotChangeVolume()
        {
            var simulation = new FloodSimulation(10d, 2d, 5d);

            simulation.AddVolume(0d);
            simulation.AddVolume(-1d);
            simulation.RemoveVolume(0d);
            simulation.RemoveVolume(-1d);
            simulation.Step(0d, 10d, 0d);
            simulation.Step(-1d, 10d, 0d);

            Assert.That(simulation.CurrentVolume, Is.EqualTo(5d).Within(Tolerance));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Constructor_RejectsNonFiniteValues(double invalidValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FloodSimulation(invalidValue, 1d));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FloodSimulation(1d, invalidValue));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FloodSimulation(1d, 1d, invalidValue));
        }

        [Test]
        public void Constructor_RejectsDimensionsWithInfiniteCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FloodSimulation(double.MaxValue, 2d));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Mutations_RejectNonFiniteValues(double invalidValue)
        {
            var simulation = new FloodSimulation(10d, 2d, 5d);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => simulation.AddVolume(invalidValue));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => simulation.RemoveVolume(invalidValue));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => simulation.SetVolume(invalidValue));
        }

        [Test]
        public void Step_RejectsNegativeFlowRates()
        {
            var simulation = new FloodSimulation(10d, 2d, 5d);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => simulation.Step(1d, -1d, 0d));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => simulation.Step(1d, 0d, -1d));
        }
    }
}
