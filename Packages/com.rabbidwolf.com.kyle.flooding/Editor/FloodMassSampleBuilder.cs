#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Builds the authored Flood Mass Integration cutaway barge sample.
    /// </summary>
    internal static class FloodMassSampleBuilder
    {
        private const string SampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Mass Integration";

        private const float CompartmentWidth = 1.8f;
        private const float CompartmentLength = 2.8f;
        private const float CompartmentHeight = 1f;
        private const float HullHalfWidth = 2f;
        private const float HullHalfLength = 3f;
        private const float WallHeight = 0.5f;

        [MenuItem("Flooding/Internal/Build Flood Mass Integration Sample", priority = 2001)]
        public static void Build()
        {
            Directory.CreateDirectory(SampleFolder);

            var bootstrapScript = LoadSampleScript("FloodMassDemoBootstrap.cs");
            var supportScript = LoadSampleScript("SampleVesselSupport.cs");
            if (bootstrapScript == null
                || bootstrapScript.GetClass() == null
                || supportScript == null
                || supportScript.GetClass() == null)
            {
                Debug.LogError(
                    "Flood Mass sample scripts were not found. Copy or import "
                    + "Flood Mass Integration into Assets/Samples, then rebuild.");
                return;
            }

            var hullMaterial = CreateLitMaterial(
                "Vessel Hull",
                new Color(0.45f, 0.48f, 0.52f, 0.35f),
                transparent: true);
            var waterMaterial = CreateLitMaterial(
                "Compartment Water",
                new Color(0.1f, 0.45f, 0.85f, 0.55f),
                transparent: true);
            var groundMaterial = CreateLitMaterial(
                "Ground",
                new Color(0.12f, 0.13f, 0.15f, 1f),
                transparent: false);
            var dryComMaterial = CreateLitMaterial(
                "Dry Com Marker",
                new Color(1f, 0.75f, 0.15f, 1f),
                transparent: false);
            var floodComMaterial = CreateLitMaterial(
                "Flood Com Marker",
                new Color(0.15f, 0.65f, 1f, 1f),
                transparent: false);
            var combinedComMaterial = CreateLitMaterial(
                "Combined Com Marker",
                new Color(0.85f, 0.2f, 1f, 1f),
                transparent: false);

            // Keep legacy Vessel.mat in sync for existing imports.
            CreateLitMaterial(
                "Vessel",
                new Color(0.45f, 0.48f, 0.52f, 0.35f),
                transparent: true);

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var ground = CreateCube(
                "Ground Plane",
                null,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(24f, 0.1f, 24f),
                groundMaterial);

            var vessel = new GameObject("Flood Mass Demo Vessel");
            vessel.transform.position = new Vector3(0f, 1f, 0f);

            var box = vessel.AddComponent<BoxCollider>();
            box.size = new Vector3(HullHalfWidth * 2f, 1.1f, HullHalfLength * 2f);
            box.center = new Vector3(0f, 0f, 0f);

            var body = vessel.AddComponent<Rigidbody>();
            body.mass = 1500f;
            body.linearDamping = 0.8f;
            body.angularDamping = 2f;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var manager = vessel.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;

            var aggregator = vessel.AddComponent<FloodMassAggregator>();
            var adapter = vessel.AddComponent<RigidbodyFloodMassAdapter>();
            adapter.FloodMass = aggregator;
            adapter.ConfigureDryBody(1500f, Vector3.zero);

            var support = vessel.AddComponent(supportScript.GetClass());
            var supportSerialized = new SerializedObject(support);
            supportSerialized.FindProperty("supportHeight").floatValue = 1f;
            supportSerialized.FindProperty("springStiffness").floatValue = 14000f;
            supportSerialized.FindProperty("damping").floatValue = 2800f;
            supportSerialized.FindProperty("halfWidth").floatValue = HullHalfWidth;
            supportSerialized.FindProperty("halfLength").floatValue = HullHalfLength;
            supportSerialized.FindProperty("supportPointY").floatValue = -0.55f;
            supportSerialized.ApplyModifiedPropertiesWithoutUndo();

            var bootstrap = vessel.AddComponent(bootstrapScript.GetClass());

            BuildHullCutaway(vessel.transform, hullMaterial);

            // Floor top is at local Y = -0.5; compartment local Y = 0 is the floor plane.
            var portBow = CreateCompartment(
                "Port Bow Compartment",
                vessel.transform,
                manager,
                new Vector3(-0.95f, -0.5f, 1.45f),
                waterMaterial);
            var starboardBow = CreateCompartment(
                "Starboard Bow Compartment",
                vessel.transform,
                manager,
                new Vector3(0.95f, -0.5f, 1.45f),
                waterMaterial);
            var portStern = CreateCompartment(
                "Port Stern Compartment",
                vessel.transform,
                manager,
                new Vector3(-0.95f, -0.5f, -1.45f),
                waterMaterial);
            var starboardStern = CreateCompartment(
                "Starboard Stern Compartment",
                vessel.transform,
                manager,
                new Vector3(0.95f, -0.5f, -1.45f),
                waterMaterial);

            aggregator.RefreshContributors();

            var dryMarker = CreateMarker(
                "Dry Com Marker",
                vessel.transform,
                dryComMaterial,
                0.18f);
            var floodMarker = CreateMarker(
                "Flood Com Marker",
                vessel.transform,
                floodComMaterial,
                0.16f);
            var combinedMarker = CreateMarker(
                "Combined Com Marker",
                vessel.transform,
                combinedComMaterial,
                0.2f);

            var shiftLineObject = new GameObject("COM Shift Line");
            shiftLineObject.transform.SetParent(vessel.transform, false);
            var shiftLine = shiftLineObject.AddComponent<LineRenderer>();
            var lineMaterial = CreateLitMaterial(
                "COM Shift Line",
                new Color(1f, 0.75f, 0.15f, 1f),
                transparent: false);
            shiftLine.sharedMaterial = lineMaterial;
            shiftLine.startColor = new Color(1f, 0.75f, 0.15f, 1f);
            shiftLine.endColor = new Color(0.85f, 0.2f, 1f, 1f);
            shiftLine.startWidth = 0.04f;
            shiftLine.endWidth = 0.04f;
            shiftLine.positionCount = 2;
            shiftLine.useWorldSpace = true;

            var bootstrapSerialized = new SerializedObject(bootstrap);
            bootstrapSerialized.FindProperty("vesselRigidbody")
                .objectReferenceValue = body;
            bootstrapSerialized.FindProperty("massAdapter")
                .objectReferenceValue = adapter;
            bootstrapSerialized.FindProperty("massAggregator")
                .objectReferenceValue = aggregator;
            bootstrapSerialized.FindProperty("portBow")
                .objectReferenceValue = portBow;
            bootstrapSerialized.FindProperty("starboardBow")
                .objectReferenceValue = starboardBow;
            bootstrapSerialized.FindProperty("portStern")
                .objectReferenceValue = portStern;
            bootstrapSerialized.FindProperty("starboardStern")
                .objectReferenceValue = starboardStern;
            bootstrapSerialized.FindProperty("dryComMarker")
                .objectReferenceValue = dryMarker.transform;
            bootstrapSerialized.FindProperty("floodComMarker")
                .objectReferenceValue = floodMarker.transform;
            bootstrapSerialized.FindProperty("combinedComMarker")
                .objectReferenceValue = combinedMarker.transform;
            bootstrapSerialized.FindProperty("comShiftLine")
                .objectReferenceValue = shiftLine;
            bootstrapSerialized.FindProperty("presetVolumePerCompartment")
                .floatValue = 2.4f;
            bootstrapSerialized.FindProperty("transferRate").floatValue = 1.5f;
            bootstrapSerialized.FindProperty("autoDemoHoldSeconds").floatValue = 4f;
            bootstrapSerialized.FindProperty("autoDemoResetSeconds").floatValue = 2f;
            bootstrapSerialized.FindProperty("autoDemoEnabled").boolValue = true;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.2f, 1f);
            cameraObject.transform.position = new Vector3(9.5f, 7.5f, -11f);
            cameraObject.transform.rotation = Quaternion.Euler(28f, -40f, 0f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var scenePath = Path.Combine(SampleFolder, "FloodMassRollPitch.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.Refresh();
            Debug.Log(
                $"Built Flood Mass Integration sample at {scenePath}. "
                + $"Ground={ground.name}");
        }

        private static FloodVolume CreateCompartment(
            string name,
            Transform parent,
            FloodSimulationManager manager,
            Vector3 localPosition,
            Material waterMaterial)
        {
            var compartment = new GameObject(name);
            compartment.transform.SetParent(parent, false);
            compartment.transform.localPosition = localPosition;

            var volume = compartment.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(
                CompartmentWidth,
                CompartmentLength,
                CompartmentHeight);
            var volumeSerialized = new SerializedObject(volume);
            volumeSerialized.FindProperty("initialVolume").floatValue = 0f;
            volumeSerialized.ApplyModifiedPropertiesWithoutUndo();

            var waterVisual = CreateCube(
                "Water Visual",
                compartment.transform,
                Vector3.zero,
                Vector3.one,
                waterMaterial);
            var renderer = compartment.AddComponent<FloodCubeSurfaceRenderer>();
            renderer.SourceVolume = volume;
            renderer.WaterVisual = waterVisual.transform;
            return volume;
        }

        private static void BuildHullCutaway(Transform parent, Material hullMaterial)
        {
            var hullRoot = new GameObject("Hull Cutaway");
            hullRoot.transform.SetParent(parent, false);

            CreateCube(
                "Floor",
                hullRoot.transform,
                new Vector3(0f, -0.55f, 0f),
                new Vector3(HullHalfWidth * 2f + 0.1f, 0.1f, HullHalfLength * 2f + 0.1f),
                hullMaterial);

            // Low cutaway walls so water remains visible.
            CreateCube(
                "Port Wall",
                hullRoot.transform,
                new Vector3(-HullHalfWidth, WallHeight * 0.5f - 0.5f, 0f),
                new Vector3(0.08f, WallHeight, HullHalfLength * 2f),
                hullMaterial);
            CreateCube(
                "Starboard Wall",
                hullRoot.transform,
                new Vector3(HullHalfWidth, WallHeight * 0.5f - 0.5f, 0f),
                new Vector3(0.08f, WallHeight, HullHalfLength * 2f),
                hullMaterial);
            CreateCube(
                "Bow Wall",
                hullRoot.transform,
                new Vector3(0f, WallHeight * 0.5f - 0.5f, HullHalfLength),
                new Vector3(HullHalfWidth * 2f, WallHeight, 0.08f),
                hullMaterial);
            CreateCube(
                "Stern Wall",
                hullRoot.transform,
                new Vector3(0f, WallHeight * 0.5f - 0.5f, -HullHalfLength),
                new Vector3(HullHalfWidth * 2f, WallHeight, 0.08f),
                hullMaterial);
            CreateCube(
                "Centerline Bulkhead",
                hullRoot.transform,
                new Vector3(0f, WallHeight * 0.5f - 0.5f, 0f),
                new Vector3(0.06f, WallHeight, HullHalfLength * 2f - 0.2f),
                hullMaterial);
            CreateCube(
                "Cross Bulkhead",
                hullRoot.transform,
                new Vector3(0f, WallHeight * 0.5f - 0.5f, 0f),
                new Vector3(HullHalfWidth * 2f - 0.2f, WallHeight, 0.06f),
                hullMaterial);
        }

        private static GameObject CreateMarker(
            string name,
            Transform parent,
            Material material,
            float diameter)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = Vector3.one * diameter;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;
            return marker;
        }

        private static MonoScript LoadSampleScript(string fileName)
        {
            var packagePath = Path.Combine(SampleFolder, fileName)
                .Replace('\\', '/');
            var importedPath =
                "Assets/Samples/Flooding/0.10.0/Flood Mass Integration/"
                + fileName;
            return AssetDatabase.LoadAssetAtPath<MonoScript>(importedPath)
                ?? AssetDatabase.LoadAssetAtPath<MonoScript>(packagePath);
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
            if (parent != null)
                cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material CreateLitMaterial(
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

            var path = Path.Combine(SampleFolder, name + ".mat").Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                Object.DestroyImmediate(material);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
#endif
