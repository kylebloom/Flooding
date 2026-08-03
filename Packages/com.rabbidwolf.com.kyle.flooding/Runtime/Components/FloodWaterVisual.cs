using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Compatibility component for scenes authored before version 0.3.0.
    /// </summary>
    [Obsolete("Use FloodCubeSurfaceRenderer instead.")]
    [AddComponentMenu("")]
    public sealed class FloodWaterVisual : FloodCubeSurfaceRenderer
    {
    }
}