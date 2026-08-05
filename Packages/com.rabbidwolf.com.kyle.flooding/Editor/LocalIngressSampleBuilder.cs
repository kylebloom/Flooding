#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Builds the authored Local Ingress package sample scene and materials.
    /// </summary>
    internal static class LocalIngressSampleBuilder
    {
        private const string SampleFolder =
            "Assets/Samples/Flooding/0.11.0/Local Ingress";

        private const string PackageSampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Local Ingress";

        [MenuItem("Flooding/Internal/Build Local Ingress Sample", priority = 2004)]
        public static void Build()
        {
            TryBuild();
        }

        public static bool TryBuild()
        {
            Directory.CreateDirectory(SampleFolder);
            EnsureSampleScriptsImported();

            var bootstrapScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                Path.Combine(SampleFolder, "LocalIngressBootstrap.cs")
                    .Replace('\\', '/'));
            if (bootstrapScript == null || bootstrapScript.GetClass() == null)
            {
                Debug.LogWarning(
                    "LocalIngressBootstrap.cs was imported to "
                    + $"{SampleFolder} but is not compiled yet. "
                    + "Unity will compile on domain reload — run "
                    + "Flooding > Internal > Build Local Ingress Sample again.");
                return false;
            }

            var wallMaterial = CreateLitMaterial(
                "Room Walls",
                new Color(0.52f, 0.54f, 0.58f, 1f),
                transparent: false);
            var floorMaterial = CreateLitMaterial(
                "Room Floor",
                new Color(0.32f, 0.3f, 0.28f, 1f),
                transparent: false);
            var waterMaterial = CreateLitMaterial(
                "Room Water",
                new Color(0.12f, 0.45f, 0.8f, 0.5f),
                transparent: true);
            var localWaterMaterial = CreateIngressMaterial(
                "Local Ingress Water",
                "Kyle/Flooding/Ingress Patch",
                new Color(0.2f, 0.55f, 0.9f, 0.55f));
            var streamMaterial = CreateIngressMaterial(
                "Ingress Stream",
                "Kyle/Flooding/Ingress Jet",
                new Color(0.35f, 0.72f, 0.98f, 0.78f));
            var openingMaterial = CreateLitMaterial(
                "Breach Opening",
                new Color(0.2f, 0.85f, 0.35f, 1f),
                transparent: false);
            var oceanMaterial = CreateLitMaterial(
                "Ocean Surface",
                new Color(0.05f, 0.35f, 0.7f, 0.4f),
                transparent: true);

            var profilePath = Path.Combine(
                    SampleFolder,
                    "LocalIngressPresentationProfile.asset")
                .Replace('\\', '/');
            var profile =
                AssetDatabase.LoadAssetAtPath<FloodIngressPresentationProfile>(
                    profilePath);
            if (profile == null)
            {
                profile = ScriptableObject
                    .CreateInstance<FloodIngressPresentationProfile>();
                profile.name = "LocalIngressPresentationProfile";
                AssetDatabase.CreateAsset(profile, profilePath);
                profile = AssetDatabase.LoadAssetAtPath<FloodIngressPresentationProfile>(
                    profilePath);
            }

            profile.LocalSpreadSpeed = 0.9f;
            profile.MaximumLocalRadius = 4.5f;
            profile.SettlingDurationSeconds = 1f;
            profile.ConvergenceDurationSeconds = 5f;
            profile.MinimumFlowRate = 0.01f;
            profile.MaximumSimultaneousPatches = 8;
            profile.FloorOffsetMeters = 0.015f;
            profile.JetInitialSpeed = 5.5f;
            profile.JetLifetimeSeconds = 0.65f;
            profile.JetWidthMeters = 0.16f;
            profile.JetTaper = 0.3f;
            profile.JetGravityInfluence = 1.1f;
            profile.JetTurbulence = 0.45f;
            profile.JetUvFlowSpeed = 3f;
            profile.DirectionalStretch = 0.95f;
            profile.DirectionalRelaxation = 0.5f;
            profile.EdgeNoiseStrength = 0.4f;
            profile.RippleStrength = 0.16f;
            profile.SplashEmissionMultiplier = 1.25f;
            profile.FoamStrength = 0.5f;
            EditorUtility.SetDirty(profile);

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("Local Ingress Demo");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;
            root.AddComponent<FloodDiagnostics>();
            var bootstrap = root.AddComponent(bootstrapScript.GetClass());

            var roomRoot = new GameObject("Large Compartment");
            roomRoot.transform.SetParent(root.transform, false);

            var volumeObject = new GameObject("Room Volume");
            volumeObject.transform.SetParent(roomRoot.transform, false);
            var volume = volumeObject.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(8f, 8f, 3f);

            var floor = CreateCube(
                "Floor",
                roomRoot.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(8.2f, 0.1f, 8.2f),
                floorMaterial,
                keepCollider: true);
            CreateCube(
                "Ceiling",
                roomRoot.transform,
                new Vector3(0f, 3.05f, 0f),
                new Vector3(8.2f, 0.1f, 8.2f),
                wallMaterial,
                keepCollider: false);
            CreateCube(
                "Back Wall",
                roomRoot.transform,
                new Vector3(0f, 1.5f, -4.1f),
                new Vector3(8.2f, 3.1f, 0.1f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Left Wall",
                roomRoot.transform,
                new Vector3(-4.1f, 1.5f, 0f),
                new Vector3(0.1f, 3.1f, 8.2f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Right Wall",
                roomRoot.transform,
                new Vector3(4.1f, 1.5f, 0f),
                new Vector3(0.1f, 3.1f, 8.2f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Front Wall Low",
                roomRoot.transform,
                new Vector3(0f, 0.4f, 4.1f),
                new Vector3(8.2f, 0.8f, 0.1f),
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

            var oceanObject = new GameObject("External Ocean");
            oceanObject.transform.SetParent(root.transform, false);
            oceanObject.transform.position = new Vector3(0f, 1.4f, 5.5f);
            var ocean = oceanObject.AddComponent<ExternalFluidBoundary>();
            ocean.SimulationManager = manager;

            var oceanVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            oceanVisual.name = "Ocean Surface Visual";
            oceanVisual.transform.SetParent(oceanObject.transform, false);
            oceanVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            oceanVisual.transform.localScale = new Vector3(12f, 12f, 1f);
            Object.DestroyImmediate(oceanVisual.GetComponent<Collider>());
            oceanVisual.GetComponent<MeshRenderer>().sharedMaterial = oceanMaterial;

            var breachObject = new GameObject("Primary Breach");
            breachObject.transform.SetParent(root.transform, false);
            breachObject.transform.position = new Vector3(0f, 0.25f, 4.05f);
            breachObject.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var primaryBreach = breachObject.AddComponent<FloodConnection>();
            primaryBreach.SimulationManager = manager;
            primaryBreach.SideA = ocean;
            primaryBreach.SideB = volume;
            primaryBreach.OpeningWidth = 1.4f;
            primaryBreach.OpeningHeight = 1.2f;
            primaryBreach.DischargeCoefficient = 0.62f;
            primaryBreach.IsOpen = true;

            CreateCube(
                "Breach Opening Visual",
                breachObject.transform,
                new Vector3(0f, 0.6f, 0f),
                new Vector3(1.4f, 1.2f, 0.05f),
                openingMaterial,
                keepCollider: false);

            var streamObject = new GameObject("Breach Stream");
            streamObject.transform.SetParent(breachObject.transform, false);
            var stream = streamObject.AddComponent<FloodIngressStreamPresenter>();
            stream.StreamMaterial = streamMaterial;
            stream.FloorPlane = floor.transform;
            stream.SimulationManager = manager;
            stream.SplashParticles = CreateSplashParticles(streamObject.transform);

            var adjacentObject = new GameObject("Adjacent Flooded Room");
            adjacentObject.transform.SetParent(root.transform, false);
            adjacentObject.transform.position = new Vector3(5.5f, 0f, 0f);
            var adjacentVolume = adjacentObject.AddComponent<FloodVolume>();
            adjacentVolume.SimulationManager = manager;
            adjacentVolume.ConfigureRectangularGeometry(2.5f, 2.5f, 2.5f);
            var adjacentSerialized = new SerializedObject(adjacentVolume);
            adjacentSerialized.FindProperty("initialVolume").floatValue =
                adjacentVolume.MaximumVolume * 0.85f;
            adjacentSerialized.ApplyModifiedPropertiesWithoutUndo();
            CreateCube(
                "Adjacent Shell",
                adjacentObject.transform,
                new Vector3(0f, 1.25f, 0f),
                new Vector3(2.6f, 2.5f, 2.6f),
                wallMaterial,
                keepCollider: false);

            var doorwayObject = new GameObject("Secondary Doorway");
            doorwayObject.transform.SetParent(root.transform, false);
            doorwayObject.transform.position = new Vector3(4.05f, 0.1f, 0f);
            doorwayObject.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var secondary = doorwayObject.AddComponent<FloodConnection>();
            secondary.SimulationManager = manager;
            secondary.SideA = adjacentVolume;
            secondary.SideB = volume;
            secondary.OpeningWidth = 1f;
            secondary.OpeningHeight = 2f;
            secondary.DischargeCoefficient = 0.62f;
            secondary.IsOpen = false;

            var leakObject = new GameObject("Ceiling Leak Source");
            leakObject.transform.SetParent(root.transform, false);
            leakObject.transform.position = new Vector3(-1.5f, 2.8f, -1f);
            leakObject.transform.rotation = Quaternion.LookRotation(Vector3.down);
            var leak = leakObject.AddComponent<FloodSource>();
            leak.SimulationManager = manager;
            leak.Target = volume;
            leak.FlowRate = 0.05f;
            leak.IsActive = false;

            var presenterObject = new GameObject("Local Ingress Presenter");
            presenterObject.transform.SetParent(volumeObject.transform, false);
            var presenter = presenterObject.AddComponent<FloodLocalIngressPresenter>();
            presenter.Volume = volume;
            presenter.Profile = profile;
            presenter.FloorPlane = floor.transform;
            presenter.PatchMaterial = localWaterMaterial;
            presenter.Connections = new[] { primaryBreach, secondary };
            presenter.Sources = new[] { leak };
            presenter.PresentationEnabled = true;

            var serializedPresenter = new SerializedObject(presenter);
            var streamsProperty = serializedPresenter.FindProperty("streamPresenters");
            streamsProperty.arraySize = 2;
            streamsProperty.GetArrayElementAtIndex(0).objectReferenceValue = stream;
            streamsProperty.GetArrayElementAtIndex(1).objectReferenceValue = null;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("compartment").objectReferenceValue =
                volume;
            serializedBootstrap.FindProperty("primaryBreach").objectReferenceValue =
                primaryBreach;
            serializedBootstrap.FindProperty("secondaryIngress")
                .objectReferenceValue = secondary;
            serializedBootstrap.FindProperty("optionalLeakSource")
                .objectReferenceValue = leak;
            serializedBootstrap.FindProperty("ocean").objectReferenceValue = ocean;
            serializedBootstrap.FindProperty("ingressPresenter")
                .objectReferenceValue = presenter;
            serializedBootstrap.FindProperty("surfaceRenderer")
                .objectReferenceValue = surfaceRenderer;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            cameraObject.transform.position = new Vector3(-3.2f, 1.6f, 2.4f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 140f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            var scenePath = Path.Combine(SampleFolder, "LocalIngress.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.SaveAssets();
            MirrorGeneratedAssetsToPackage();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Built Local Ingress sample at {scenePath} "
                + $"(mirrored to {PackageSampleFolder}).");
            return true;
        }

        private static void EnsureSampleScriptsImported()
        {
            Directory.CreateDirectory(PackageSampleFolder);
            Directory.CreateDirectory(SampleFolder);

            CopyIfExists(
                Path.Combine(PackageSampleFolder, "LocalIngressBootstrap.cs"),
                Path.Combine(SampleFolder, "LocalIngressBootstrap.cs"));
            CopyIfExists(
                Path.Combine(PackageSampleFolder, "README.md"),
                Path.Combine(SampleFolder, "README.md"));

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(
                Path.Combine(SampleFolder, "LocalIngressBootstrap.cs")
                    .Replace('\\', '/'),
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void MirrorGeneratedAssetsToPackage()
        {
            Directory.CreateDirectory(PackageSampleFolder);

            string[] fileNames =
            {
                "LocalIngressBootstrap.cs",
                "LocalIngressBootstrap.cs.meta",
                "README.md",
                "README.md.meta",
                "LocalIngress.unity",
                "LocalIngress.unity.meta",
                "LocalIngressPresentationProfile.asset",
                "LocalIngressPresentationProfile.asset.meta",
                "Room Walls.mat",
                "Room Walls.mat.meta",
                "Room Floor.mat",
                "Room Floor.mat.meta",
                "Room Water.mat",
                "Room Water.mat.meta",
                "Local Ingress Water.mat",
                "Local Ingress Water.mat.meta",
                "Ingress Stream.mat",
                "Ingress Stream.mat.meta",
                "Breach Opening.mat",
                "Breach Opening.mat.meta",
                "Ocean Surface.mat",
                "Ocean Surface.mat.meta",
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

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, overwrite: true);
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
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material CreateIngressMaterial(
            string name,
            string preferredShaderName,
            Color color)
        {
            var shader = Shader.Find(preferredShaderName)
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color,
            };

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Opacity"))
                material.SetFloat("_Opacity", color.a);

            var path = Path.Combine(SampleFolder, name + ".mat").Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                existing.color = color;
                if (existing.HasProperty("_BaseColor"))
                    existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Opacity"))
                    existing.SetFloat("_Opacity", color.a);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
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
            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static ParticleSystem CreateSplashParticles(Transform parent)
        {
            var splashObject = new GameObject("Impact Splash");
            splashObject.transform.SetParent(parent, false);
            var particles = splashObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 1.8f;
            main.startSize = 0.07f;
            main.startColor = new Color(0.85f, 0.92f, 1f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.85f;
            main.maxParticles = 128;

            var emission = particles.emission;
            emission.rateOverTime = 28f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.08f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.7f, 0.85f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var renderer = splashObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                renderer.sharedMaterial = new Material(particleShader)
                {
                    color = new Color(0.85f, 0.92f, 1f, 0.7f),
                };
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }
    }
}
#endif
