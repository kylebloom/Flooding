using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Serializable reference that resolves to a supported
    /// <see cref="IFluidBoundary"/> component.
    /// </summary>
    [Serializable]
    public struct FluidBoundaryReference : IEquatable<FluidBoundaryReference>
    {
        [SerializeField]
        private Component component;

        /// <summary>
        /// Gets the serialized component, if any.
        /// </summary>
        public Component Component => component;

        /// <summary>
        /// Gets whether a non-null component is assigned.
        /// </summary>
        public bool IsAssigned => component != null;

        /// <summary>
        /// Creates a reference to the supplied boundary.
        /// </summary>
        public static FluidBoundaryReference From(IFluidBoundary boundary)
        {
            return new FluidBoundaryReference
            {
                component = boundary as Component,
            };
        }

        /// <summary>
        /// Assigns a supported boundary component.
        /// </summary>
        public void Set(IFluidBoundary boundary)
        {
            component = boundary as Component;
        }

        /// <summary>
        /// Clears the assigned component.
        /// </summary>
        public void Clear()
        {
            component = null;
        }

        /// <summary>
        /// Attempts to resolve a supported fluid boundary.
        /// </summary>
        public bool TryGet(out IFluidBoundary boundary)
        {
            boundary = component as IFluidBoundary;
            return boundary != null;
        }

        /// <summary>
        /// Resolves a cleared reference, an <see cref="IFluidBoundary"/> component,
        /// or a GameObject that owns one boundary component on the same GameObject.
        /// </summary>
        /// <param name="source">
        /// Null, a supported boundary component, or a GameObject containing one.
        /// </param>
        /// <param name="boundaryComponent">
        /// The resolved boundary component, or null when <paramref name="source"/>
        /// is null.
        /// </param>
        /// <returns>
        /// True when <paramref name="source"/> is null or resolves to a supported
        /// boundary; otherwise false.
        /// </returns>
        public static bool TryResolveComponent(
            UnityEngine.Object source,
            out Component boundaryComponent)
        {
            boundaryComponent = null;

            if (source == null)
                return true;

            if (source is Component component && component is IFluidBoundary)
            {
                boundaryComponent = component;
                return true;
            }

            if (source is GameObject gameObject)
            {
                foreach (var candidate in gameObject.GetComponents<Component>())
                {
                    if (candidate is IFluidBoundary)
                    {
                        boundaryComponent = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool Equals(FluidBoundaryReference other)
        {
            return component == other.component;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FluidBoundaryReference other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return component == null ? 0 : component.GetHashCode();
        }
    }
}
