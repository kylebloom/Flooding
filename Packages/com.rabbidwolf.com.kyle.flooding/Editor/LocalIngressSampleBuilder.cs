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

            var softParticleTexture = CreateSoftParticleTexture();

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
                new Color(0.16f, 0.52f, 0.88f, 0.62f),
                configurePatch: true);
            var streamMaterial = CreateIngressMaterial(
                "Ingress Stream",
                "Kyle/Flooding/Ingress Jet",
                new Color(0.38f, 0.74f, 0.98f, 0.82f),
                configurePatch: false);
            var openingMaterial = CreateLitMaterial(
                "Breach Opening",
                new Color(0.2f, 0.85f, 0.35f, 1f),
                transparent: false);
            var oceanMaterial = CreateLitMaterial(
                "Ocean Surface",
                new Color(0.05f, 0.35f, 0.7f, 0.4f),
                transparent: true);

            var dropletMaterial = CreateSoftParticleMaterial(
                "Ingress Droplet Particle",
                softParticleTexture,
                new Color(0.78f, 0.9f, 1f, 0.85f),
                additive: false);
            var mistMaterial = CreateSoftParticleMaterial(
                "Ingress Mist Particle",
                softParticleTexture,
                new Color(0.85f, 0.93f, 1f, 0.35f),
                additive: true);
            var foamParticleMaterial = CreateSoftParticleMaterial(
                "Ingress Foam Particle",
                softParticleTexture,
                new Color(0.95f, 0.98f, 1f, 0.8f),
                additive: false);

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

            // Showcase-tuned presentation values (not generic defaults).
            profile.LocalSpreadSpeed = 1.05f;
            profile.MaximumLocalRadius = 4.8f;
            profile.SettlingDurationSeconds = 1f;
            profile.ConvergenceDurationSeconds = 5f;
            profile.MinimumFlowRate = 0.01f;
            profile.MaximumSimultaneousPatches = 8;
            profile.FloorOffsetMeters = 0.015f;
            profile.JetInitialSpeed = 6.2f;
            profile.JetLifetimeSeconds = 0.72f;
            profile.JetWidthMeters = 0.22f;
            profile.JetTaper = 0.34f;
            profile.JetGravityInfluence = 1.15f;
            profile.JetTurbulence = 0.8f;
            profile.JetUvFlowSpeed = 4.2f;
            profile.DirectionalStretch = 1.05f;
            profile.DirectionalRelaxation = 0.5f;
            profile.EdgeNoiseScale = 2.8f;
            profile.EdgeNoiseStrength = 0.52f;
            profile.EdgeSoftness = 0.22f;
            profile.RippleStrength = 0.3f;
            profile.RippleSpeed = 2.1f;
            profile.SplashEmissionMultiplier = 1.65f;
            profile.SplashDropletSpeed = 2.7f;
            profile.SplashDropletSize = 1.2f;
            profile.FoamColor = new Color(0.92f, 0.97f, 1f, 1f);
            profile.FoamStrength = 0.88f;
            profile.FoamEdgeWidth = 0.24f;
            profile.FoamNoiseScale = 5.2f;
            profile.FoamScrollSpeed = 0.9f;
            profile.SprayMistThreshold = 0.32f;
            profile.FoamBurstThreshold = 0.18f;
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
            AssignImpactLayers(
                stream,
                CreateImpactHierarchy(
                    streamObject.transform,
                    dropletMaterial,
                    mistMaterial,
                    foamParticleMaterial,
                    majorScale: true));

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

            var leakStreamObject = new GameObject("Leak Stream");
            leakStreamObject.transform.SetParent(leakObject.transform, false);
            var leakStream = leakStreamObject.AddComponent<FloodIngressStreamPresenter>();
            leakStream.StreamMaterial = streamMaterial;
            leakStream.FloorPlane = floor.transform;
            leakStream.SimulationManager = manager;
            AssignImpactLayers(
                leakStream,
                CreateImpactHierarchy(
                    leakStreamObject.transform,
                    dropletMaterial,
                    mistMaterial,
                    foamParticleMaterial,
                    majorScale: false));

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
            streamsProperty.arraySize = 3;
            streamsProperty.GetArrayElementAtIndex(0).objectReferenceValue = stream;
            streamsProperty.GetArrayElementAtIndex(1).objectReferenceValue = null;
            streamsProperty.GetArrayElementAtIndex(2).objectReferenceValue = leakStream;
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

            // First-person showcase framing toward the primary breach impact zone.
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            cameraObject.transform.position = new Vector3(-2.35f, 1.35f, 1.55f);
            cameraObject.transform.rotation = Quaternion.Euler(8f, 148f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.98f, 0.94f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(38f, -40f, 0f);

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
                "Ingress Soft Particle.png",
                "Ingress Soft Particle.png.meta",
                "Ingress Droplet Particle.mat",
                "Ingress Droplet Particle.mat.meta",
                "Ingress Mist Particle.mat",
                "Ingress Mist Particle.mat.meta",
                "Ingress Foam Particle.mat",
                "Ingress Foam Particle.mat.meta",
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

        private static Texture2D CreateSoftParticleTexture()
        {
            const int size = 64;
            var path = Path.Combine(SampleFolder, "Ingress Soft Particle.png")
                .Replace('\\', '/');

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Ingress Soft Particle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var a = Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateSoftParticleMaterial(
            string name,
            Texture2D softTexture,
            Color color,
            bool additive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var material = new Material(shader)
            {
                name = name,
            };

            ConfigureTransparentParticleMaterial(material, softTexture, color, additive);

            var path = Path.Combine(SampleFolder, name + ".mat").Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                ConfigureTransparentParticleMaterial(
                    existing,
                    softTexture,
                    color,
                    additive);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void ConfigureTransparentParticleMaterial(
            Material material,
            Texture2D softTexture,
            Color color,
            bool additive)
        {
            if (material.HasProperty("_BaseMap") && softTexture != null)
                material.SetTexture("_BaseMap", softTexture);
            if (material.HasProperty("_MainTex") && softTexture != null)
                material.SetTexture("_MainTex", softTexture);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            material.color = color;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", additive ? 1f : 0f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)(additive ? BlendMode.SrcAlpha : BlendMode.SrcAlpha));
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)(additive
                        ? BlendMode.One
                        : BlendMode.OneMinusSrcAlpha));
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive)
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            else
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static Material CreateIngressMaterial(
            string name,
            string preferredShaderName,
            Color color,
            bool configurePatch)
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

            ConfigureIngressMaterial(material, color, configurePatch);

            var path = Path.Combine(SampleFolder, name + ".mat").Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                ConfigureIngressMaterial(existing, color, configurePatch);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void ConfigureIngressMaterial(
            Material material,
            Color color,
            bool configurePatch)
        {
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
            material.color = color;

            if (configurePatch)
            {
                if (material.HasProperty("_FoamColor"))
                    material.SetColor("_FoamColor", new Color(0.92f, 0.97f, 1f, 1f));
                if (material.HasProperty("_FoamStrength"))
                    material.SetFloat("_FoamStrength", 0.85f);
                if (material.HasProperty("_FoamEdgeWidth"))
                    material.SetFloat("_FoamEdgeWidth", 0.24f);
                if (material.HasProperty("_FoamNoiseScale"))
                    material.SetFloat("_FoamNoiseScale", 5.2f);
                if (material.HasProperty("_FoamScrollSpeed"))
                    material.SetFloat("_FoamScrollSpeed", 0.9f);
                if (material.HasProperty("_EdgeNoiseStrength"))
                    material.SetFloat("_EdgeNoiseStrength", 0.52f);
                if (material.HasProperty("_RippleStrength"))
                    material.SetFloat("_RippleStrength", 0.3f);
                if (material.HasProperty("_RippleSpeed"))
                    material.SetFloat("_RippleSpeed", 2.1f);
                if (material.HasProperty("_NormalStrength"))
                    material.SetFloat("_NormalStrength", 0.65f);
                if (material.HasProperty("_FlowMotion"))
                    material.SetFloat("_FlowMotion", 1.0f);
            }
            else
            {
                if (material.HasProperty("_Turbulence"))
                    material.SetFloat("_Turbulence", 0.8f);
                if (material.HasProperty("_FlowSpeed"))
                    material.SetFloat("_FlowSpeed", 4.2f);
                if (material.HasProperty("_EdgeFade"))
                    material.SetFloat("_EdgeFade", 0.3f);
                if (material.HasProperty("_AlphaBreakup"))
                    material.SetFloat("_AlphaBreakup", 0.5f);
                if (material.HasProperty("_FoamHighlight"))
                    material.SetFloat("_FoamHighlight", 0.4f);
                if (material.HasProperty("_FresnelIntensity"))
                    material.SetFloat("_FresnelIntensity", 0.6f);
            }
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
                existing.shader = shader;
                existing.color = color;
                if (transparent && existing.HasProperty("_Surface"))
                {
                    existing.SetFloat("_Surface", 1f);
                    existing.SetFloat("_Blend", 0f);
                    existing.SetOverrideTag("RenderType", "Transparent");
                    existing.renderQueue = (int)RenderQueue.Transparent;
                    existing.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

                if (existing.HasProperty("_BaseColor"))
                    existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private readonly struct ImpactLayers
        {
            public readonly ParticleSystem Droplets;
            public readonly ParticleSystem SprayMist;
            public readonly ParticleSystem FoamBurst;

            public ImpactLayers(
                ParticleSystem droplets,
                ParticleSystem sprayMist,
                ParticleSystem foamBurst)
            {
                Droplets = droplets;
                SprayMist = sprayMist;
                FoamBurst = foamBurst;
            }
        }

        private static void AssignImpactLayers(
            FloodIngressStreamPresenter stream,
            ImpactLayers layers)
        {
            stream.DropletParticles = layers.Droplets;
            stream.SprayMistParticles = layers.SprayMist;
            stream.FoamBurstParticles = layers.FoamBurst;
        }

        private static ImpactLayers CreateImpactHierarchy(
            Transform parent,
            Material dropletMaterial,
            Material mistMaterial,
            Material foamMaterial,
            bool majorScale)
        {
            var root = new GameObject("FloodIngressImpact");
            root.transform.SetParent(parent, false);

            var rateScale = majorScale ? 1f : 0.35f;
            var sizeScale = majorScale ? 1f : 0.7f;

            var droplets = CreateLayerParticles(
                root.transform,
                "Droplets",
                dropletMaterial,
                lifetime: 0.55f,
                startSpeed: 2.4f,
                startSize: 0.1f * sizeScale,
                startColor: new Color(0.8f, 0.92f, 1f, 0.8f),
                gravity: 1.05f,
                maxParticles: majorScale ? 96 : 40,
                rate: 42f * rateScale,
                coneAngle: 26f,
                coneRadius: 0.1f,
                stretched: true,
                velocityScale: 0.1f,
                lengthScale: 2.6f);

            var mist = CreateLayerParticles(
                root.transform,
                "SprayMist",
                mistMaterial,
                lifetime: 0.35f,
                startSpeed: 1.1f,
                startSize: 0.24f * sizeScale,
                startColor: new Color(0.88f, 0.94f, 1f, 0.28f),
                gravity: 0.15f,
                maxParticles: majorScale ? 64 : 24,
                rate: 48f * rateScale,
                coneAngle: 42f,
                coneRadius: 0.14f,
                stretched: false,
                velocityScale: 0f,
                lengthScale: 1f);

            var foam = CreateLayerParticles(
                root.transform,
                "FoamBurst",
                foamMaterial,
                lifetime: 0.7f,
                startSpeed: 0.65f,
                startSize: 0.32f * sizeScale,
                startColor: new Color(0.95f, 0.98f, 1f, 0.75f),
                gravity: 0.05f,
                maxParticles: majorScale ? 48 : 16,
                rate: 28f * rateScale,
                coneAngle: 60f,
                coneRadius: 0.16f,
                stretched: false,
                velocityScale: 0f,
                lengthScale: 1f,
                sizeOverLifetimeExpand: true);

            return new ImpactLayers(droplets, mist, foam);
        }

        private static ParticleSystem CreateLayerParticles(
            Transform parent,
            string name,
            Material material,
            float lifetime,
            float startSpeed,
            float startSize,
            Color startColor,
            float gravity,
            int maxParticles,
            float rate,
            float coneAngle,
            float coneRadius,
            bool stretched,
            float velocityScale,
            float lengthScale,
            bool sizeOverLifetimeExpand = false)
        {
            var particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, false);
            var particles = particleObject.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = startColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = maxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = particles.emission;
            emission.rateOverTime = rate;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = coneRadius;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(
                        new Color(0.75f, 0.88f, 1f),
                        1f),
                },
                new[]
                {
                    new GradientAlphaKey(Mathf.Clamp01(startColor.a), 0f),
                    new GradientAlphaKey(Mathf.Clamp01(startColor.a * 0.55f), 0.35f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            if (sizeOverLifetimeExpand)
            {
                var sizeOverLifetime = particles.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                    1f,
                    AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.35f));
            }

            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            if (stretched)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = velocityScale;
                renderer.lengthScale = lengthScale;
                renderer.cameraVelocityScale = 0f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }
    }
}
#endif
