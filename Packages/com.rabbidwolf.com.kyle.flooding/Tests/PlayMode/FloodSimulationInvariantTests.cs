using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodSimulationInvariantTests
    {
        private const double TickDuration = 0.05d;
        private const int TickCount = 120;

        [UnityTest]
        public IEnumerator RepeatedTicks_PreserveFiniteInternalNetworkInvariants()
        {
            var first = CreateNetwork("First invariant network");
            var second = CreateNetwork("Second invariant network");
            var initialTotal = TotalVolume(first.Volumes);
            var conservationTolerance = Math.Max(
                FloodGeometryTolerances.SolverAbsoluteVolume,
                initialTotal * FloodGeometryTolerances.SolverRelativeVolume);
            var sawCompetingSourceRequests = false;
            var sawCompetingDestinationRequests = false;
            var sawReconciledTransfer = false;

            Assert.That(
                TotalVolume(second.Volumes),
                Is.EqualTo(initialTotal).Within(conservationTolerance));

            for (var tick = 0; tick < TickCount; tick++)
            {
                ApplyDeterministicRotation(first, tick);
                ApplyDeterministicRotation(second, tick);

                var firstPreviousVolumes = CaptureVolumes(first.Volumes);
                var secondPreviousVolumes = CaptureVolumes(second.Volumes);

                first.Manager.SimulateTick(TickDuration);
                second.Manager.SimulateTick(TickDuration);

                RecordExercisedConditions(
                    first,
                    conservationTolerance,
                    ref sawCompetingSourceRequests,
                    ref sawCompetingDestinationRequests,
                    ref sawReconciledTransfer);
                AssertNetworkInvariants(
                    first,
                    firstPreviousVolumes,
                    initialTotal,
                    conservationTolerance,
                    tick);
                AssertNetworkInvariants(
                    second,
                    secondPreviousVolumes,
                    initialTotal,
                    conservationTolerance,
                    tick);
                AssertMatchingNetworks(
                    first,
                    second,
                    conservationTolerance,
                    tick);
            }

            Assert.That(
                sawCompetingSourceRequests,
                Is.True,
                "The repeated scenario did not exercise competing source outflows.");
            Assert.That(
                sawCompetingDestinationRequests,
                Is.True,
                "The repeated scenario did not exercise competing destination inflows.");
            Assert.That(
                sawReconciledTransfer,
                Is.True,
                "The repeated scenario did not exercise a constrained transfer.");

            UnityEngine.Object.Destroy(first.Root);
            UnityEngine.Object.Destroy(second.Root);
            yield return null;
        }

        private static NetworkSetup CreateNetwork(string name)
        {
            var root = new GameObject(name);
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = false;
            manager.GravityMode = FloodGravityMode.Custom;
            manager.CustomGravity = new Vector3(0f, -9.81f, 0f);

            var volumes = new[]
            {
                CreateVolume(root.transform, "Volume A", 11d),
                CreateVolume(root.transform, "Volume B", 7d),
                CreateVolume(root.transform, "Volume C", 2d),
                CreateVolume(root.transform, "Volume D", 0d),
            };

            volumes[0].transform.localRotation =
                Quaternion.Euler(12f, 18f, -9f);
            volumes[1].transform.localRotation =
                Quaternion.Euler(-16f, 31f, 11f);
            volumes[2].transform.localRotation =
                Quaternion.Euler(19f, -24f, 7f);
            volumes[3].transform.localRotation =
                Quaternion.Euler(-8f, 42f, -14f);

            var connections = new[]
            {
                CreateConnection(root.transform, "A to B", volumes[0], volumes[1]),
                CreateConnection(root.transform, "A to C", volumes[0], volumes[2]),
                CreateConnection(root.transform, "B to D", volumes[1], volumes[3]),
                CreateConnection(root.transform, "C to D", volumes[2], volumes[3]),
                CreateConnection(root.transform, "Closed A to D", volumes[0], volumes[3]),
            };
            connections[^1].IsOpen = false;

            return new NetworkSetup(root, manager, volumes, connections);
        }

        private static FloodVolume CreateVolume(
            Transform parent,
            string name,
            double initialVolume)
        {
            var volumeObject = new GameObject(name);
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(2f, 2f, 3f);
            volume.AddWater((float)initialVolume);
            return volume;
        }

        private static FloodConnection CreateConnection(
            Transform parent,
            string name,
            FloodVolume volumeA,
            FloodVolume volumeB)
        {
            var connectionObject = new GameObject(name);
            connectionObject.transform.SetParent(parent, false);
            var connection = connectionObject.AddComponent<FloodConnection>();
            connection.VolumeA = volumeA;
            connection.VolumeB = volumeB;
            connection.OpeningWidth = 25f;
            connection.OpeningHeight = 2f;
            connection.DischargeCoefficient = 1f;
            return connection;
        }

        private static void ApplyDeterministicRotation(
            NetworkSetup setup,
            int tick)
        {
            if (tick == 40)
            {
                setup.Volumes[1].transform.localRotation =
                    Quaternion.Euler(27f, -12f, 21f);
                setup.Volumes[3].transform.localRotation =
                    Quaternion.Euler(-23f, 37f, 16f);
            }
            else if (tick == 80)
            {
                setup.Volumes[0].transform.localRotation =
                    Quaternion.Euler(-18f, 8f, 29f);
                setup.Volumes[2].transform.localRotation =
                    Quaternion.Euler(14f, 53f, -20f);
            }
        }

        private static double[] CaptureVolumes(
            IReadOnlyList<FloodVolume> volumes)
        {
            var captured = new double[volumes.Count];

            for (var index = 0; index < volumes.Count; index++)
                captured[index] = volumes[index].CurrentState.Volume;

            return captured;
        }

        private static void AssertNetworkInvariants(
            NetworkSetup setup,
            IReadOnlyList<double> previousVolumes,
            double initialTotal,
            double conservationTolerance,
            int tick)
        {
            var expectedDeltas = new double[setup.Volumes.Length];

            foreach (var connection in setup.Connections)
            {
                AssertConnectionIsFinite(connection, tick);
                AssertTransferConsistency(
                    connection,
                    conservationTolerance,
                    tick);

                var transferredVolume =
                    connection.CurrentFlowRate * TickDuration;
                var indexA = Array.IndexOf(setup.Volumes, connection.VolumeA);
                var indexB = Array.IndexOf(setup.Volumes, connection.VolumeB);
                expectedDeltas[indexA] -= transferredVolume;
                expectedDeltas[indexB] += transferredVolume;
            }

            for (var index = 0; index < setup.Volumes.Length; index++)
            {
                var state = setup.Volumes[index].CurrentState;
                var boundsTolerance = Math.Max(
                    FloodGeometryTolerances.SolverAbsoluteVolume,
                    state.Capacity
                        * FloodGeometryTolerances.SolverRelativeVolume);

                AssertStateIsFinite(state, tick, setup.Volumes[index].name);
                Assert.That(
                    state.Volume,
                    Is.GreaterThanOrEqualTo(-boundsTolerance),
                    $"Tick {tick}: {setup.Volumes[index].name} fell below zero.");
                Assert.That(
                    state.Volume,
                    Is.LessThanOrEqualTo(state.Capacity + boundsTolerance),
                    $"Tick {tick}: {setup.Volumes[index].name} exceeded capacity.");
                Assert.That(
                    state.Volume - previousVolumes[index],
                    Is.EqualTo(expectedDeltas[index])
                        .Within(conservationTolerance),
                    $"Tick {tick}: {setup.Volumes[index].name} did not reconcile applied connection transfers.");
            }

            Assert.That(
                TotalVolume(setup.Volumes),
                Is.EqualTo(initialTotal).Within(conservationTolerance),
                $"Tick {tick}: internal network volume was not conserved.");

            var closedConnection = setup.Connections[^1];
            Assert.That(closedConnection.RequestedFlowRate, Is.Zero);
            Assert.That(closedConnection.CurrentFlowRate, Is.Zero);
            Assert.That(closedConnection.SubmergedOpeningArea, Is.Zero);
            Assert.That(closedConnection.PressureHeadDifference, Is.Zero);
        }

        private static void RecordExercisedConditions(
            NetworkSetup setup,
            double volumeTolerance,
            ref bool sawCompetingSourceRequests,
            ref bool sawCompetingDestinationRequests,
            ref bool sawReconciledTransfer)
        {
            sawCompetingSourceRequests |=
                setup.Connections[0].RequestedFlowRate > 0d
                && setup.Connections[1].RequestedFlowRate > 0d;
            sawCompetingDestinationRequests |=
                setup.Connections[2].RequestedFlowRate > 0d
                && setup.Connections[3].RequestedFlowRate > 0d;

            var rateTolerance = volumeTolerance / TickDuration;

            foreach (var connection in setup.Connections)
            {
                sawReconciledTransfer |=
                    Math.Abs(connection.CurrentFlowRate) + rateTolerance
                    < Math.Abs(connection.RequestedFlowRate);
            }
        }

        private static void AssertTransferConsistency(
            FloodConnection connection,
            double volumeTolerance,
            int tick)
        {
            var requested = connection.RequestedFlowRate;
            var applied = connection.CurrentFlowRate;
            var rateTolerance = volumeTolerance / TickDuration;

            Assert.That(
                Math.Abs(applied),
                Is.LessThanOrEqualTo(Math.Abs(requested) + rateTolerance),
                $"Tick {tick}: {connection.name} applied more than requested.");

            if (Math.Abs(applied) <= rateTolerance)
                return;

            Assert.That(
                Math.Sign(applied),
                Is.EqualTo(Math.Sign(requested)),
                $"Tick {tick}: {connection.name} reversed requested direction during reconciliation.");
        }

        private static void AssertStateIsFinite(
            FloodState state,
            int tick,
            string volumeName)
        {
            Assert.That(IsFinite(state.Volume), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.Capacity), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.Height), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.FillPercentage), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.WaterMass), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.SurfacePlane.normal), Is.True, FiniteMessage());
            Assert.That(IsFinite(state.SurfacePlane.distance), Is.True, FiniteMessage());
            Assert.That(
                IsFinite(state.WaterCenterOfMassWorld),
                Is.True,
                FiniteMessage());

            string FiniteMessage()
            {
                return $"Tick {tick}: {volumeName} published non-finite state.";
            }
        }

        private static void AssertConnectionIsFinite(
            FloodConnection connection,
            int tick)
        {
            Assert.That(
                IsFinite(connection.RequestedFlowRate),
                Is.True,
                $"Tick {tick}: {connection.name} requested flow was non-finite.");
            Assert.That(
                IsFinite(connection.CurrentFlowRate),
                Is.True,
                $"Tick {tick}: {connection.name} applied flow was non-finite.");
            Assert.That(
                IsFinite(connection.SubmergedOpeningArea),
                Is.True,
                $"Tick {tick}: {connection.name} submerged area was non-finite.");
            Assert.That(
                IsFinite(connection.PressureHeadDifference),
                Is.True,
                $"Tick {tick}: {connection.name} pressure head was non-finite.");
            Assert.That(
                IsFinite(connection.FlowDirectionWorld),
                Is.True,
                $"Tick {tick}: {connection.name} flow direction was non-finite.");
        }

        private static void AssertMatchingNetworks(
            NetworkSetup first,
            NetworkSetup second,
            double tolerance,
            int tick)
        {
            for (var index = 0; index < first.Volumes.Length; index++)
            {
                Assert.That(
                    second.Volumes[index].CurrentState.Volume,
                    Is.EqualTo(first.Volumes[index].CurrentState.Volume)
                        .Within(tolerance),
                    $"Tick {tick}: deterministic volume trajectory diverged.");
            }

            for (var index = 0; index < first.Connections.Length; index++)
            {
                Assert.That(
                    second.Connections[index].RequestedFlowRate,
                    Is.EqualTo(first.Connections[index].RequestedFlowRate)
                        .Within(tolerance / TickDuration),
                    $"Tick {tick}: deterministic requested flow diverged.");
                Assert.That(
                    second.Connections[index].CurrentFlowRate,
                    Is.EqualTo(first.Connections[index].CurrentFlowRate)
                        .Within(tolerance / TickDuration),
                    $"Tick {tick}: deterministic applied flow diverged.");
            }
        }

        private static double TotalVolume(IReadOnlyList<FloodVolume> volumes)
        {
            var total = 0d;

            foreach (var volume in volumes)
                total += volume.CurrentState.Volume;

            return total;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x)
                && IsFinite(value.y)
                && IsFinite(value.z);
        }

        private readonly struct NetworkSetup
        {
            public NetworkSetup(
                GameObject root,
                FloodSimulationManager manager,
                FloodVolume[] volumes,
                FloodConnection[] connections)
            {
                Root = root;
                Manager = manager;
                Volumes = volumes;
                Connections = connections;
            }

            public GameObject Root { get; }

            public FloodSimulationManager Manager { get; }

            public FloodVolume[] Volumes { get; }

            public FloodConnection[] Connections { get; }
        }
    }
}
