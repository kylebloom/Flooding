using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Aggregates water mass from flood volumes below one vessel root.
    /// </summary>
    [AddComponentMenu("Flooding/Flood Mass Aggregator")]
    [DisallowMultipleComponent]
    public sealed class FloodMassAggregator : MonoBehaviour, IMassContributor
    {
        [SerializeField]
        [Tooltip("Include disabled child FloodVolume components in the aggregate.")]
        private bool includeInactive;

        private FloodVolume[] contributors = System.Array.Empty<FloodVolume>();

        /// <summary>
        /// Gets the number of discovered child flood volumes.
        /// </summary>
        public int ContributorCount => contributors.Length;

        /// <summary>
        /// Gets the current aggregate flood mass in kilograms.
        /// </summary>
        public double Mass => CurrentContribution.Mass;

        /// <summary>
        /// Gets the current aggregate flood center of mass in world space.
        /// For zero mass, this component's position is returned.
        /// </summary>
        public Vector3 CenterOfMassWorld
        {
            get
            {
                var contribution = CurrentContribution;
                return contribution.Mass > 0d
                    ? contribution.CenterOfMassWorld
                    : transform.position;
            }
        }

        /// <summary>
        /// Gets the current immutable aggregate contribution.
        /// </summary>
        public FloodMassContribution CurrentContribution
        {
            get
            {
                var contribution = FloodMassAggregation.Combine(contributors);
                return contribution.Mass > 0d
                    ? contribution
                    : new FloodMassContribution(0d, transform.position);
            }
        }

        private void Awake()
        {
            RefreshContributors();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshContributors();
        }

        private void OnValidate()
        {
            RefreshContributors();
        }

        /// <summary>
        /// Rediscovers child flood volumes after a hierarchy change.
        /// </summary>
        public void RefreshContributors()
        {
            contributors = GetComponentsInChildren<FloodVolume>(includeInactive);
        }
    }
}
