#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Builds the authored First Person Flooding package sample scene and materials.
    /// </summary>
    internal static class FirstPersonFloodingSampleBuilder
    {
        private const string SampleFolder =
            "Assets/Samples/Flooding/0.10.0/First Person Flooding";

        private const string PackageSampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/First Person Flooding";

        [MenuItem(
            "Flooding/Internal/Build First Person Flooding Sample",
            priority = 2003)]
        public static void Build()
        {
            TryBuild();
        }

        public static bool TryBuild()
        {
            Directory.CreateDirectory(SampleFolder);
            EnsureSampleScriptsImported();

            var bootstrapScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                Path.Combine(SampleFolder, "FirstPersonFloodingBootstrap.cs")
                    .Replace('\\', '/'));
            if (bootstrapScript == null || bootstrapScript.GetClass() == null)
            {
                Debug.LogWarning(
                    "FirstPersonFloodingBootstrap.cs was imported to "
                    + $"{SampleFolder} but is not compiled yet. "
                    + "Unity will compile on domain reload — run "
                    + "Flooding > Internal > Build First Person Flooding Sample "
                    + "again.");
                return false;
            }

            var wallMaterial = CreateLitMaterial(
                "Room Walls",
                new Color(0.55f, 0.57f, 0.6f, 1f),
                transparent: false);
            var floorMaterial = CreateLitMaterial(
                "Room Floor",
                new Color(0.35f, 0.33f, 0.3f, 1f),
                transparent: false);
            var waterMaterial = CreateLitMaterial(
                "Room Water",
                new Color(0.12f, 0.45f, 0.8f, 0.55f),
                transparent: true);

            var profilePath = Path.Combine(
                    SampleFolder,
                    "FirstPersonUnderwaterProfile.asset")
                .Replace('\\', '/');
            var profile =
                AssetDatabase.LoadAssetAtPath<FloodUnderwaterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
                profile.name = "FirstPersonUnderwaterProfile";
                AssetDatabase.CreateAsset(profile, profilePath);
                profile = AssetDatabase.LoadAssetAtPath<FloodUnderwaterProfile>(
                    profilePath);
            }

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("First Person Flooding Demo");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;
            root.AddComponent<FloodDiagnostics>();
            var bootstrap = root.AddComponent(bootstrapScript.GetClass());

            var roomRoot = new GameObject("Flooded Room");
            roomRoot.transform.SetParent(root.transform, false);

            var volumeObject = new GameObject("Room Volume");
            volumeObject.transform.SetParent(roomRoot.transform, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(5f, 5f, 3f);

            CreateCube(
                "Floor",
                roomRoot.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(5.2f, 0.1f, 5.2f),
                floorMaterial,
                keepCollider: true);
            CreateCube(
                "Ceiling",
                roomRoot.transform,
                new Vector3(0f, 3.05f, 0f),
                new Vector3(5.2f, 0.1f, 5.2f),
                wallMaterial,
                keepCollider: false);
            CreateCube(
                "Back Wall",
                roomRoot.transform,
                new Vector3(0f, 1.5f, -2.6f),
                new Vector3(5.2f, 3.1f, 0.1f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Front Wall",
                roomRoot.transform,
                new Vector3(0f, 1.5f, 2.6f),
                new Vector3(5.2f, 3.1f, 0.1f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Left Wall",
                roomRoot.transform,
                new Vector3(-2.6f, 1.5f, 0f),
                new Vector3(0.1f, 3.1f, 5.2f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Right Wall",
                roomRoot.transform,
                new Vector3(2.6f, 1.5f, 0f),
                new Vector3(0.1f, 3.1f, 5.2f),
                wallMaterial,
                keepCollider: true);

            var waterVisual = CreateCube(
                "Water Visual",
                volumeObject.transform,
                Vector3.zero,
                Vector3.one,
                waterMaterial,
                keepCollider: false);
            var surfaceRenderer =
                volumeObject.AddComponent<FloodCubeSurfaceRenderer>();
            surfaceRenderer.SourceVolume = volume;
            surfaceRenderer.WaterVisual = waterVisual.transform;

            var volumeTelemetry =
                volumeObject.AddComponent<FloodVolumeTelemetry>();
            volumeTelemetry.Volume = volume;

            var sourceObject = new GameObject("Rising Water Source");
            sourceObject.transform.SetParent(root.transform, false);
            var source = sourceObject.AddComponent<FloodSource>();
            source.SimulationManager = manager;
            source.Target = volume;
            source.FlowRate = 2.5f;

            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.14f, 1f);
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            var tracker = cameraObject.AddComponent<FloodCameraTracker>();
            tracker.VolumeSelectionMode =
                FloodCameraVolumeSelectionMode.Explicit;
            tracker.ExplicitVolume = volume;
            tracker.Viewpoint = cameraObject.transform;
            tracker.Manager = manager;

            var underwaterEffect = TryAddUnderwaterCameraEffect(
                cameraObject,
                tracker,
                profile);

            var underwaterAudio =
                cameraObject.AddComponent<FloodUnderwaterAudio>();
            underwaterAudio.Tracker = tracker;

            var cameraTelemetry =
                cameraObject.AddComponent<FloodCameraTelemetry>();
            cameraTelemetry.Tracker = tracker;

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("roomVolume").objectReferenceValue =
                volume;
            serializedBootstrap.FindProperty("inflowSource")
                .objectReferenceValue = source;
            serializedBootstrap.FindProperty("roomRoot").objectReferenceValue =
                roomRoot.transform;
            serializedBootstrap.FindProperty("characterController")
                .objectReferenceValue = controller;
            serializedBootstrap.FindProperty("cameraTransform")
                .objectReferenceValue = cameraObject.transform;
            serializedBootstrap.FindProperty("cameraTracker")
                .objectReferenceValue = tracker;
            serializedBootstrap.FindProperty("underwaterEffect")
                .objectReferenceValue = underwaterEffect;
            serializedBootstrap.FindProperty("underwaterAudio")
                .objectReferenceValue = underwaterAudio;
            serializedBootstrap.FindProperty("volumeTelemetry")
                .objectReferenceValue = volumeTelemetry;
            serializedBootstrap.FindProperty("cameraTelemetry")
                .objectReferenceValue = cameraTelemetry;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

            var scenePath = Path.Combine(SampleFolder, "FirstPersonFlooding.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.SaveAssets();
            MirrorGeneratedAssetsToPackage();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Built First Person Flooding sample at {scenePath} "
                + $"(mirrored to {PackageSampleFolder}). "
                + "Add Flood Underwater Renderer Feature to your URP Renderer "
                + "and enable Depth Texture to see the waterline effect.");
            return true;
        }

        private static void EnsureSampleScriptsImported()
        {
            Directory.CreateDirectory(PackageSampleFolder);
            Directory.CreateDirectory(SampleFolder);

            CopyIfExists(
                Path.Combine(PackageSampleFolder, "FirstPersonFloodingBootstrap.cs"),
                Path.Combine(SampleFolder, "FirstPersonFloodingBootstrap.cs"));
            CopyIfExists(
                Path.Combine(PackageSampleFolder, "README.md"),
                Path.Combine(SampleFolder, "README.md"));

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(
                Path.Combine(SampleFolder, "FirstPersonFloodingBootstrap.cs")
                    .Replace('\\', '/'),
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void MirrorGeneratedAssetsToPackage()
        {
            Directory.CreateDirectory(PackageSampleFolder);

            string[] fileNames =
            {
                "FirstPersonFloodingBootstrap.cs",
                "FirstPersonFloodingBootstrap.cs.meta",
                "README.md",
                "README.md.meta",
                "FirstPersonFlooding.unity",
                "FirstPersonFlooding.unity.meta",
                "FirstPersonUnderwaterProfile.asset",
                "FirstPersonUnderwaterProfile.asset.meta",
                "Room Walls.mat",
                "Room Walls.mat.meta",
                "Room Floor.mat",
                "Room Floor.mat.meta",
                "Room Water.mat",
                "Room Water.mat.meta",
            };

            foreach (var fileName in fileNames)
            {
                var source = Path.Combine(SampleFolder, fileName);
                var destination = Path.Combine(PackageSampleFolder, fileName);
                if (!File.Exists(source))
                    continue;

                File.Copy(source, destination, overwrite: true);
            }
        }

        private static void CopyIfExists(string source, string destination)
        {
            if (!File.Exists(source))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }

        /// <summary>
        /// Adds <c>FloodUnderwaterCameraEffect</c> via reflection so the Editor
        /// assembly does not hard-reference optional <c>Kyle.Flooding.URP</c>.
        /// </summary>
        private static Component TryAddUnderwaterCameraEffect(
            GameObject cameraObject,
            FloodCameraTracker tracker,
            FloodUnderwaterProfile profile)
        {
            var effectType = Type.GetType(
                "Kyle.Flooding.URP.FloodUnderwaterCameraEffect, Kyle.Flooding.URP");
            if (effectType == null)
            {
                Debug.LogWarning(
                    "Kyle.Flooding.URP is unavailable (install Universal RP "
                    + ">= 17 to enable FloodUnderwaterCameraEffect). "
                    + "First Person Flooding sample will run without the "
                    + "fullscreen underwater pass.");
                return null;
            }

            var effect = cameraObject.AddComponent(effectType);
            var serializedEffect = new SerializedObject(effect);
            serializedEffect.FindProperty("tracker").objectReferenceValue =
                tracker;
            serializedEffect.FindProperty("profile").objectReferenceValue =
                profile;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material CreateLitMaterial(
            string name,
            Color color,
            bool transparent)
        {
            var path = Path.Combine(SampleFolder, name + ".mat").Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard");
            var material = existing != null ? existing : new Material(shader);
            material.shader = shader;
            material.name = name;
            material.color = color;

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

            if (existing == null)
                AssetDatabase.CreateAsset(material, path);
            else
                EditorUtility.SetDirty(material);

            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
#endif
