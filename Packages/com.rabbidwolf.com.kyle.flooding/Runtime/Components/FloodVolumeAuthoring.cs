using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Stores source references and bake settings for Editor-only processing.
    /// This component never reads mesh vertices at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloodVolumeAuthoring : MonoBehaviour
    {
        private const float MinimumCellResolution = 0.01f;

        [SerializeField]
        [Tooltip("Flood Volume component that receives the baked data asset. If unassigned, the component on this GameObject is used.")]
        private FloodVolume targetVolume;

        [SerializeField]
        [Tooltip("Mesh Filter containing one readable, closed, manifold source mesh. Mesh analysis occurs only when Bake is pressed in the Unity Editor.")]
        private MeshFilter sourceMeshFilter;

        [SerializeField]
        [Tooltip("Requested maximum cell edge length in meters. Smaller values improve boundary fidelity but increase bake time, asset size, and runtime query cost.")]
        [Min(MinimumCellResolution)]
        private float cellResolution = 0.25f;

        [SerializeField]
        [Tooltip("Maximum number of grid cells the Editor baker may inspect. The bake stops instead of creating an unexpectedly large asset.")]
        [Min(1)]
        private int maximumGridCells = 1000000;

        [SerializeField]
        [Tooltip("Flood Volume Data asset produced by the last successful Editor bake.")]
        private FloodVolumeData bakedData;

        [SerializeField]
        [Tooltip("Draw the baked representation samples and source bounds while this GameObject is selected.")]
        private bool visualizeBake = true;

        /// <summary>Gets the target scene volume.</summary>
        public FloodVolume TargetVolume => targetVolume;

        /// <summary>Gets the Editor source mesh filter.</summary>
        public MeshFilter SourceMeshFilter => sourceMeshFilter;

        /// <summary>Gets requested maximum cell edge length in meters.</summary>
        public float CellResolution => cellResolution;

        /// <summary>Gets the Editor bake grid-cell safety limit.</summary>
        public int MaximumGridCells => maximumGridCells;

        /// <summary>Gets the last assigned baked asset.</summary>
        public FloodVolumeData BakedData => bakedData;

        /// <summary>Gets whether selected-object bake visualization is enabled.</summary>
        public bool VisualizeBake => visualizeBake;

        private void Reset()
        {
            targetVolume = GetComponent<FloodVolume>();
            sourceMeshFilter = GetComponent<MeshFilter>();
        }

        private void OnValidate()
        {
            cellResolution = Mathf.Max(
                MinimumCellResolution,
                cellResolution);
            maximumGridCells = Mathf.Max(1, maximumGridCells);

            if (targetVolume == null)
                targetVolume = GetComponent<FloodVolume>();
        }

        internal void AssignBake(FloodVolumeData data)
        {
            bakedData = data;
            if (targetVolume != null)
                targetVolume.ConfigureBakedGeometry(data);
        }
    }
}
