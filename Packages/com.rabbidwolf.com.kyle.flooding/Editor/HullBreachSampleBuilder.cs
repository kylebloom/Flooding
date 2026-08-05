#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Builds the authored Hull Breach package sample scene and materials.
    /// </summary>
    internal static class HullBreachSampleBuilder
    {
        private const string PackageSampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Hull Breach";

        private const string ImportedSampleFolder =
            "Assets/Samples/Flooding/0.10.0/Hull Breach";

        [MenuItem("Flooding/Internal/Build Hull Breach Sample", priority = 2000)]
        public static void Build()
        {
            var sampleFolder = ResolveSampleFolder();
            Directory.CreateDirectory(sampleFolder);

            var bootstrapScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                Path.Combine(sampleFolder, "HullBreachBootstrap.cs")
                    .Replace('\\', '/'));
            if (bootstrapScript == null || bootstrapScript.GetClass() == null)
            {
                Debug.LogError(
                    "HullBreachBootstrap.cs was not found under the Hull Breach "
                    + "sample folder. Import the sample into Assets/Samples first, "
                    + "or open Unity so the script meta exists, then rebuild.");
                return;
            }

            var wallMaterial = CreateLitMaterial(
                sampleFolder,
                "Compartment Walls",
                new Color(0.55f, 0.58f, 0.62f, 0.35f),
                transparent: true);
            var waterMaterial = CreateLitMaterial(
                sampleFolder,
                "Compartment Water",
                new Color(0.1f, 0.45f, 0.85f, 0.55f),
                transparent: true);
            var oceanMaterial = CreateLitMaterial(
                sampleFolder,
                "Ocean Surface",
                new Color(0.05f, 0.35f, 0.7f, 0.4f),
                transparent: true);
            var openingMaterial = CreateLitMaterial(
                sampleFolder,
                "Breach Opening",
                new Color(0.2f, 0.85f, 0.35f, 1f),
                transparent: false);

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("Hull Breach Demo");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;
            root.AddComponent<FloodDiagnostics>();
            var bootstrap = root.AddComponent(bootstrapScript.GetClass());

            var oceanObject = new GameObject("External Ocean");
            oceanObject.transform.SetParent(root.transform, false);
            oceanObject.transform.position = new Vector3(0f, 1f, 0f);
            var ocean = oceanObject.AddComponent<ExternalFluidBoundary>();
            ocean.SimulationManager = manager;

            var oceanVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            oceanVisual.name = "Ocean Surface Visual";
            oceanVisual.transform.SetParent(oceanObject.transform, false);
            oceanVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            oceanVisual.transform.localScale = new Vector3(14f, 14f, 1f);
            Object.DestroyImmediate(oceanVisual.GetComponent<Collider>());
            oceanVisual.GetComponent<MeshRenderer>().sharedMaterial = oceanMaterial;

            var compartmentObject = new GameObject("Breached Compartment");
            compartmentObject.transform.SetParent(root.transform, false);
            var volume = compartmentObject.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(4f, 3f, 2.5f);

            CreateCube(
                "Floor",
                compartmentObject.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(4.1f, 0.1f, 3.1f),
                wallMaterial);
            CreateCube(
                "Back Wall",
                compartmentObject.transform,
                new Vector3(0f, 1.25f, -1.55f),
                new Vector3(4.1f, 2.5f, 0.1f),
                wallMaterial);
            CreateCube(
                "Left Wall",
                compartmentObject.transform,
                new Vector3(-2.05f, 1.25f, 0f),
                new Vector3(0.1f, 2.5f, 3.1f),
                wallMaterial);
            CreateCube(
                "Right Wall",
                compartmentObject.transform,
                new Vector3(2.05f, 1.25f, 0f),
                new Vector3(0.1f, 2.5f, 3.1f),
                wallMaterial);
            CreateCube(
                "Front Wall Low",
                compartmentObject.transform,
                new Vector3(0f, 0.35f, 1.55f),
                new Vector3(4.1f, 0.7f, 0.1f),
                wallMaterial);

            var waterVisual = CreateCube(
                "Water Visual",
                compartmentObject.transform,
                Vector3.zero,
                Vector3.one,
                waterMaterial);
            var surfaceRenderer =
                compartmentObject.AddComponent<FloodCubeSurfaceRenderer>();
            surfaceRenderer.SourceVolume = volume;
            surfaceRenderer.WaterVisual = waterVisual.transform;

            var breachObject = new GameObject("Hull Breach Connection");
            breachObject.transform.SetParent(root.transform, false);
            breachObject.transform.position = new Vector3(0f, 0.2f, 1.55f);
            var connection = breachObject.AddComponent<FloodConnection>();
            connection.SimulationManager = manager;
            connection.SideA = ocean;
            connection.SideB = volume;
            connection.OpeningWidth = 1.2f;
            connection.OpeningHeight = 1f;
            connection.DischargeCoefficient = 0.62f;
            connection.IsOpen = true;

            CreateCube(
                "Opening Visual",
                breachObject.transform,
                new Vector3(0f, 0.5f, 0f),
                new Vector3(1.2f, 1f, 0.05f),
                openingMaterial);

            var bilgeObject = new GameObject("Bilge Pump Sink");
            bilgeObject.transform.SetParent(root.transform, false);
            bilgeObject.transform.position = new Vector3(0f, 0.15f, 0f);
            var bilgePump = bilgeObject.AddComponent<FloodSink>();
            bilgePump.SimulationManager = manager;
            bilgePump.Target = volume;
            bilgePump.FlowRate = 0.35f;
            bilgePump.IsActive = false;

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("ocean").objectReferenceValue = ocean;
            serializedBootstrap.FindProperty("compartment").objectReferenceValue =
                volume;
            serializedBootstrap.FindProperty("breach").objectReferenceValue =
                connection;
            serializedBootstrap.FindProperty("bilgePump").objectReferenceValue =
                bilgePump;
            serializedBootstrap.FindProperty("oceanSurfaceVisual")
                .objectReferenceValue = oceanVisual.transform;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            cameraObject.transform.position = new Vector3(8f, 4f, 8f);
            cameraObject.transform.rotation = Quaternion.Euler(25f, -135f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.2f, 1f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var scenePath = Path.Combine(sampleFolder, "HullBreach.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Built Hull Breach sample at {scenePath}");
        }

        private static string ResolveSampleFolder()
        {
            if (AssetDatabase.IsValidFolder(ImportedSampleFolder)
                || File.Exists(
                    Path.Combine(
                        ImportedSampleFolder,
                        "HullBreachBootstrap.cs")))
            {
                return ImportedSampleFolder;
            }

            if (File.Exists(
                    Path.Combine(
                        PackageSampleFolder,
                        "HullBreachBootstrap.cs")))
            {
                return PackageSampleFolder;
            }

            return ImportedSampleFolder;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material CreateLitMaterial(
            string sampleFolder,
            string name,
            Color color,
            bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color,
            };

            if (transparent && material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            var path = Path.Combine(sampleFolder, name + ".mat").Replace('\\', '/');
            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
#endif
