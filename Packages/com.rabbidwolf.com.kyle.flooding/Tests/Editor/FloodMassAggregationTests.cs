using System;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodMassAggregationTests
    {
        [Test]
        public void Combine_UsesMassWeightedWorldCenter()
        {
            IMassContributor[] contributors =
            {
                new TestContributor(100d, new Vector3(-2f, 1f, 0f)),
                new TestContributor(300d, new Vector3(2f, 3f, 4f)),
            };

            var result = FloodMassAggregation.Combine(contributors);

            Assert.That(result.Mass, Is.EqualTo(400d));
            Assert.That(
                result.CenterOfMassWorld,
                Is.EqualTo(new Vector3(1f, 2.5f, 3f)));
        }

        [Test]
        public void Combine_ZeroMass_ReturnsEmptyContribution()
        {
            IMassContributor[] contributors =
            {
                null,
                new TestContributor(0d, new Vector3(12f, 4f, -3f)),
            };

            var result = FloodMassAggregation.Combine(contributors);

            Assert.That(result, Is.EqualTo(FloodMassContribution.Empty));
        }

        [Test]
        public void Combine_NegativeMass_Throws()
        {
            IMassContributor[] contributors =
            {
                new TestContributor(-1d, Vector3.zero),
            };

            Assert.That(
                () => FloodMassAggregation.Combine(contributors),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private sealed class TestContributor : IMassContributor
        {
            public TestContributor(double mass, Vector3 centerOfMassWorld)
            {
                Mass = mass;
                CenterOfMassWorld = centerOfMassWorld;
            }

            public double Mass { get; }

            public Vector3 CenterOfMassWorld { get; }
        }
    }
}
