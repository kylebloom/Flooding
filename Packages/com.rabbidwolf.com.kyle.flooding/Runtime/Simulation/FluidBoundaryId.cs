using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Stable identity for one registered fluid boundary endpoint.
    /// </summary>
    public readonly struct FluidBoundaryId : IEquatable<FluidBoundaryId>
    {
        private readonly EntityId entityId;

        /// <summary>
        /// Creates an identity from a Unity object entity id.
        /// </summary>
        public FluidBoundaryId(EntityId entityId)
        {
            this.entityId = entityId;
        }

        /// <summary>
        /// Gets whether this identity refers to a currently valid object.
        /// </summary>
        public bool IsValid => Resources.EntityIdIsValid(entityId);

        /// <summary>
        /// Creates an identity for a Unity object.
        /// </summary>
        public static FluidBoundaryId FromObject(UnityEngine.Object obj)
        {
            return obj == null
                ? default
                : new FluidBoundaryId(obj.GetEntityId());
        }

        /// <inheritdoc />
        public bool Equals(FluidBoundaryId other)
        {
            return entityId.Equals(other.entityId);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FluidBoundaryId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return entityId.GetHashCode();
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        public static bool operator ==(FluidBoundaryId left, FluidBoundaryId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality comparison.
        /// </summary>
        public static bool operator !=(FluidBoundaryId left, FluidBoundaryId right)
        {
            return !left.Equals(right);
        }
    }
}
