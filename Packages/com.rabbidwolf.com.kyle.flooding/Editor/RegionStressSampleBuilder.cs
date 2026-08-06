#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Builds the authored Region Stress package sample (Phase 17 / 0.14.3).
    /// </summary>
    internal static class RegionStressSampleBuilder
    {
        private const string SampleFolder =
            "Assets/Samples/Flooding/0.14.3/Region Stress";

        private const string PackageSampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Region Stress";

        private const float RegionCellResolution = 0.55f;
        private const float NicheCellResolution = 0.45f;

        [MenuItem("Flooding/Internal/Build Region Stress Sample", priority = 2004)]
        public static void Build()
        {
            TryBuild();
        }

        public static bool TryBuild()
        {
            Directory.CreateDirectory(SampleFolder);
            EnsureSampleScriptsImported();

            var bootstrapScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                Path.Combine(SampleFolder, "RegionStressBootstrap.cs")
                    .Replace('\\', '/'));
            if (bootstrapScript == null || bootstrapScript.GetClass() == null)
            {
                Debug.LogWarning(
                    "RegionStressBootstrap.cs was imported to "
                    + $"{SampleFolder} but is not compiled yet. "
                    + "Unity will compile on domain reload — run "
                    + "Flooding > Internal > Build Region Stress Sample again.");
                return false;
            }

            var wallMaterial = CreateLitMaterial(
                "Compartment Walls",
                new Color(0.52f, 0.55f, 0.58f, 1f),
                transparent: false);
            var floorMaterial = CreateLitMaterial(
                "Compartment Floor",
                new Color(0.32f, 0.3f, 0.28f, 1f),
                transparent: false);
            var waterMaterial = CreateLitMaterial(
                "Region Water",
                new Color(0.1f, 0.45f, 0.82f, 0.5f),
                transparent: true);
            var oceanMaterial = CreateLitMaterial(
                "Ocean Surface",
                new Color(0.05f, 0.35f, 0.7f, 0.4f),
                transparent: true);
            var openingMaterial = CreateLitMaterial(
                "Opening Marker",
                new Color(0.2f, 0.85f, 0.35f, 1f),
                transparent: false);
            var nicheMaterial = CreateLitMaterial(
                "Niche Hull",
                new Color(0.4f, 0.45f, 0.5f, 0.45f),
                transparent: true);

            var profilePath = Path.Combine(
                    SampleFolder,
                    "RegionStressUnderwaterProfile.asset")
                .Replace('\\', '/');
            var profile =
                AssetDatabase.LoadAssetAtPath<FloodUnderwaterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FloodUnderwaterProfile>();
                profile.name = "RegionStressUnderwaterProfile";
                AssetDatabase.CreateAsset(profile, profilePath);
                profile = AssetDatabase.LoadAssetAtPath<FloodUnderwaterProfile>(
                    profilePath);
            }

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("Region Stress Demo");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;
            manager.TicksPerSecond = 5f;
            manager.MaximumTicksPerFrame = 2;
            root.AddComponent<FloodDiagnostics>();
            var bootstrap = root.AddComponent(bootstrapScript.GetClass());

            var vessel = new GameObject("Vessel");
            vessel.transform.SetParent(root.transform, false);

            var oceanObject = new GameObject("External Ocean");
            oceanObject.transform.SetParent(vessel.transform, false);
            oceanObject.transform.localPosition = new Vector3(0f, 4.4f, -4.5f);
            var ocean = oceanObject.AddComponent<ExternalFluidBoundary>();
            ocean.SimulationManager = manager;

            var oceanVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            oceanVisual.name = "Ocean Surface Visual";
            oceanVisual.transform.SetParent(oceanObject.transform, false);
            oceanVisual.transform.localPosition = new Vector3(0f, -0.482f, 0f);
            oceanVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            oceanVisual.transform.localScale = new Vector3(16f, 16f, 1f);
            UnityEngine.Object.DestroyImmediate(oceanVisual.GetComponent<Collider>());
            oceanVisual.GetComponent<MeshRenderer>().sharedMaterial = oceanMaterial;

            // --- Region A (upper deck compartment + alcove) ---
            var regionARoot = new GameObject("Region A");
            regionARoot.transform.SetParent(vessel.transform, false);
            regionARoot.transform.localPosition = new Vector3(0f, 3f, 0f);
            regionARoot.SetActive(false);
            var regionA = regionARoot.AddComponent<FloodRegion>();
            regionA.SimulationManager = manager;
            regionA.ConfigureBakeSettings(RegionCellResolution, 1000000);

            var roomA = CreateRectangularMember(
                regionARoot.transform,
                "Room A",
                Vector3.zero,
                4f,
                3f,
                2.5f,
                manager);
            var alcoveA = CreateRectangularMember(
                regionARoot.transform,
                "Alcove A",
                new Vector3(2.2f, 0f, 0f),
                1.6f,
                2f,
                2.5f,
                manager);
            regionA.SetMembers(new[] { roomA, alcoveA });

            BuildRoomShell(
                regionARoot.transform,
                "Room A Shell",
                Vector3.zero,
                4f,
                3f,
                2.5f,
                wallMaterial,
                floorMaterial,
                openPositiveZ: true,
                openNegativeZ: true);

            if (!BakeRegion(
                    regionA,
                    "RegionA FloodRegionData.asset",
                    out var regionABakeMessage))
            {
                Debug.LogError("Region A bake failed: " + regionABakeMessage);
                return false;
            }

            CreateRegionWaterVisual(regionA, waterMaterial);
            regionARoot.SetActive(true);

            // --- Corridor + stair (one multi-deck region) ---
            var corridorRoot = new GameObject("Region Corridor Stair");
            corridorRoot.transform.SetParent(vessel.transform, false);
            corridorRoot.transform.localPosition = Vector3.zero;
            corridorRoot.SetActive(false);
            var regionCorridor = corridorRoot.AddComponent<FloodRegion>();
            regionCorridor.SimulationManager = manager;
            regionCorridor.ConfigureBakeSettings(RegionCellResolution, 1000000);

            // Upper corridor butts against Region A +Z door at world z≈1.5.
            var upperCorridor = CreateRectangularMember(
                corridorRoot.transform,
                "Upper Corridor",
                new Vector3(0f, 3f, 3.5f),
                3f,
                4f,
                2.5f,
                manager);
            var upperLanding = CreateRectangularMember(
                corridorRoot.transform,
                "Upper Landing",
                new Vector3(0f, 3f, 6.25f),
                2.4f,
                2f,
                2.5f,
                manager);
            // Tall shaft from lower deck up through the upper landing footprint.
            var stairShaft = CreateRectangularMember(
                corridorRoot.transform,
                "Stair Shaft",
                new Vector3(0f, 0f, 6.5f),
                2f,
                2.2f,
                5.5f,
                manager);
            var lowerLanding = CreateRectangularMember(
                corridorRoot.transform,
                "Lower Landing",
                new Vector3(0f, 0f, 8.5f),
                3f,
                3f,
                2.5f,
                manager);
            regionCorridor.SetMembers(
                new[] { upperCorridor, upperLanding, stairShaft, lowerLanding });

            BuildRoomShell(
                corridorRoot.transform,
                "Upper Corridor Shell",
                new Vector3(0f, 3f, 3.5f),
                3f,
                4f,
                2.5f,
                wallMaterial,
                floorMaterial,
                openNegativeZ: true,
                openPositiveZ: true);
            BuildRoomShell(
                corridorRoot.transform,
                "Lower Landing Shell",
                new Vector3(0f, 0f, 9.42f),
                3f,
                3f,
                2.5f,
                wallMaterial,
                floorMaterial,
                openNegativeZ: true,
                openPositiveZ: true);
            // Simple stair step colliders for walking (presentation only).
            CreateStairSteps(
                corridorRoot.transform,
                new Vector3(0.002f, -0.199f, 7.012f),
                Quaternion.Euler(0f, -179.613f, 0f),
                new Vector3(1.8005f, 1f, 1f),
                floorMaterial);

            if (!BakeRegion(
                    regionCorridor,
                    "CorridorStair FloodRegionData.asset",
                    out var corridorBakeMessage))
            {
                Debug.LogError(
                    "Corridor/Stair bake failed: " + corridorBakeMessage);
                return false;
            }

            CreateRegionWaterVisual(regionCorridor, waterMaterial);
            corridorRoot.SetActive(true);

            // --- Region B (room + irregular baked niche) ---
            var regionBRoot = new GameObject("Region B");
            regionBRoot.transform.SetParent(vessel.transform, false);
            regionBRoot.transform.localPosition = new Vector3(0f, 0f, 11.5f);
            regionBRoot.SetActive(false);
            var regionB = regionBRoot.AddComponent<FloodRegion>();
            regionB.SimulationManager = manager;
            regionB.ConfigureBakeSettings(RegionCellResolution, 1000000);

            var roomB = CreateRectangularMember(
                regionBRoot.transform,
                "Room B",
                Vector3.zero,
                4f,
                4f,
                2.5f,
                manager);

            if (!TryCreateIrregularNiche(
                    regionBRoot.transform,
                    manager,
                    nicheMaterial,
                    out var nicheVolume,
                    out var nicheMessage))
            {
                Debug.LogError("Irregular niche bake failed: " + nicheMessage);
                return false;
            }

            regionB.SetMembers(new[] { roomB, nicheVolume });

            BuildRoomShell(
                regionBRoot.transform,
                "Room B Shell",
                new Vector3(0f, 0f, 1.645f),
                4f,
                4f,
                2.5f,
                wallMaterial,
                floorMaterial,
                openNegativeZ: true,
                openPositiveZ: false);

            if (!BakeRegion(
                    regionB,
                    "RegionB FloodRegionData.asset",
                    out var regionBBakeMessage))
            {
                Debug.LogError("Region B bake failed: " + regionBBakeMessage);
                return false;
            }

            CreateRegionWaterVisual(regionB, waterMaterial);
            regionBRoot.SetActive(true);

            // --- Connections ---
            var breach = CreateConnection(
                vessel.transform,
                "Breach Ocean to A",
                new Vector3(0f, 3.35f, -1.55f),
                manager,
                ocean,
                regionA,
                openingWidth: 1.2f,
                openingHeight: 1f,
                openFraction: 0.6f,
                openingMaterial);

            var door = CreateConnection(
                vessel.transform,
                "Door A to Corridor",
                new Vector3(0f, 3.35f, 1.55f),
                manager,
                regionA,
                regionCorridor,
                openingWidth: 1f,
                openingHeight: 2f,
                openFraction: 0.25f,
                openingMaterial);

            var hatch = CreateConnection(
                vessel.transform,
                "Hatch Corridor to B",
                new Vector3(0f, 0.35f, 10f),
                manager,
                regionCorridor,
                regionB,
                openingWidth: 1.1f,
                openingHeight: 1.2f,
                openFraction: 1f,
                openingMaterial);

            var pumpObject = new GameObject("Bilge Pump");
            pumpObject.transform.SetParent(vessel.transform, false);
            pumpObject.transform.localPosition = new Vector3(0f, 0.2f, 11.5f);
            var pump = pumpObject.AddComponent<FloodSink>();
            pump.SimulationManager = manager;
            pump.Target = roomB;
            pump.FlowRate = 0.45f;
            pump.IsActive = false;

            // --- Player ---
            var player = new GameObject("Player");
            player.transform.SetParent(vessel.transform, false);
            player.transform.localPosition = new Vector3(0f, 3.05f, 0f);
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
                FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;
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

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(root.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("simulationManager")
                .objectReferenceValue = manager;
            serializedBootstrap.FindProperty("regionA").objectReferenceValue =
                regionA;
            serializedBootstrap.FindProperty("regionCorridor")
                .objectReferenceValue = regionCorridor;
            serializedBootstrap.FindProperty("regionB").objectReferenceValue =
                regionB;
            serializedBootstrap.FindProperty("breach").objectReferenceValue =
                breach;
            serializedBootstrap.FindProperty("door").objectReferenceValue = door;
            serializedBootstrap.FindProperty("hatch").objectReferenceValue =
                hatch;
            serializedBootstrap.FindProperty("pump").objectReferenceValue = pump;
            serializedBootstrap.FindProperty("vesselRoot").objectReferenceValue =
                vessel.transform;
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
            serializedBootstrap.FindProperty("cameraTelemetry")
                .objectReferenceValue = cameraTelemetry;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            var scenePath = Path.Combine(SampleFolder, "RegionStress.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.SaveAssets();
            MirrorGeneratedAssetsToPackage();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Built Region Stress sample at {scenePath} "
                + $"(mirrored to {PackageSampleFolder}). "
                + $"Region A: {regionABakeMessage} | "
                + $"Corridor: {corridorBakeMessage} | "
                + $"Region B: {regionBBakeMessage}");
            return true;
        }

        private static bool BakeRegion(
            FloodRegion region,
            string assetFileName,
            out string message)
        {
            var path = Path.Combine(SampleFolder, assetFileName)
                .Replace('\\', '/');
            var data = AssetDatabase.LoadAssetAtPath<FloodRegionData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FloodRegionData>();
                AssetDatabase.CreateAsset(data, path);
                data = AssetDatabase.LoadAssetAtPath<FloodRegionData>(path);
            }

            region.AssignBake(data);
            if (!FloodRegionBaker.TryBake(
                    region,
                    out data,
                    out message,
                    promptForAssetPath: false))
            {
                return false;
            }

            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(region);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static FloodVolume CreateRectangularMember(
            Transform parent,
            string name,
            Vector3 localPosition,
            float width,
            float length,
            float height,
            FloodSimulationManager manager)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var volume = go.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;
            volume.ConfigureRectangularGeometry(width, length, height);
            return volume;
        }

        private static void CreateRegionWaterVisual(
            FloodRegion region,
            Material waterMaterial)
        {
            var waterObject = new GameObject("Water Visual");
            waterObject.transform.SetParent(region.transform, false);
            waterObject.AddComponent<MeshFilter>();
            var renderer = waterObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial;

            var surfaceRenderer =
                region.gameObject.AddComponent<FloodRegionSurfaceRenderer>();
            var serialized = new SerializedObject(surfaceRenderer);
            serialized.FindProperty("floodRegion").objectReferenceValue = region;
            serialized.FindProperty("waterVisual").objectReferenceValue =
                waterObject.transform;
            serialized.FindProperty("interpolationDuration").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static FloodConnection CreateConnection(
            Transform parent,
            string name,
            Vector3 localPosition,
            FloodSimulationManager manager,
            IFluidBoundary sideA,
            IFluidBoundary sideB,
            float openingWidth,
            float openingHeight,
            float openFraction,
            Material openingMaterial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var connection = go.AddComponent<FloodConnection>();
            connection.SimulationManager = manager;
            connection.SideA = sideA;
            connection.SideB = sideB;
            connection.OpeningWidth = openingWidth;
            connection.OpeningHeight = openingHeight;
            connection.DischargeCoefficient = 0.62f;
            connection.IsOpen = openFraction > 0f;
            connection.OpenFraction = openFraction;

            CreateCube(
                "Opening Visual",
                go.transform,
                new Vector3(0f, openingHeight * 0.5f, 0f),
                new Vector3(openingWidth, openingHeight, 0.05f),
                openingMaterial,
                keepCollider: false);
            return connection;
        }

        private static bool TryCreateIrregularNiche(
            Transform parent,
            FloodSimulationManager manager,
            Material nicheMaterial,
            out FloodVolume nicheVolume,
            out string message)
        {
            nicheVolume = null;
            message = null;

            var nicheRoot = new GameObject("Irregular Niche");
            nicheRoot.transform.SetParent(parent, false);
            // Overlap Room B on +X so the region bake stays face-connected.
            nicheRoot.transform.localPosition = new Vector3(1.7f, 0f, 0.3f);

            nicheVolume = nicheRoot.AddComponent<FloodVolume>();
            nicheVolume.SimulationManager = manager;

            var sourceMesh = CreateOrUpdateSlopedNicheMesh();
            if (!FloodVolumeBaker.TryValidateClosedMesh(
                    sourceMesh,
                    out message))
            {
                return false;
            }

            var sourceObject = new GameObject("Niche Source Mesh");
            sourceObject.transform.SetParent(nicheRoot.transform, false);
            var sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = sourceMesh;
            var sourceRenderer = sourceObject.AddComponent<MeshRenderer>();
            sourceRenderer.sharedMaterial = nicheMaterial;

            var bakedPath = Path.Combine(
                    SampleFolder,
                    "NicheSlopedFloodVolumeData.asset")
                .Replace('\\', '/');
            var bakedData =
                AssetDatabase.LoadAssetAtPath<FloodVolumeData>(bakedPath);
            if (bakedData == null)
            {
                bakedData = ScriptableObject.CreateInstance<FloodVolumeData>();
                AssetDatabase.CreateAsset(bakedData, bakedPath);
                bakedData =
                    AssetDatabase.LoadAssetAtPath<FloodVolumeData>(bakedPath);
            }

            var authoring = nicheRoot.AddComponent<FloodVolumeAuthoring>();
            var authoringSerialized = new SerializedObject(authoring);
            authoringSerialized.FindProperty("targetVolume")
                .objectReferenceValue = nicheVolume;
            authoringSerialized.FindProperty("sourceMeshFilter")
                .objectReferenceValue = sourceFilter;
            authoringSerialized.FindProperty("cellResolution").floatValue =
                NicheCellResolution;
            authoringSerialized.FindProperty("maximumGridCells").intValue =
                1000000;
            authoringSerialized.FindProperty("bakedData")
                .objectReferenceValue = bakedData;
            authoringSerialized.FindProperty("visualizeBake").boolValue = true;
            authoringSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (!FloodVolumeBaker.TryBake(
                    authoring,
                    out bakedData,
                    out message))
            {
                return false;
            }

            nicheVolume.ConfigureBakedGeometry(bakedData);
            return true;
        }

        private static Mesh CreateOrUpdateSlopedNicheMesh()
        {
            var mesh = BuildSlopedNicheMesh();
            var path = Path.Combine(SampleFolder, "NicheSlopedSourceMesh.asset")
                .Replace('\\', '/');
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            var loaded = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            loaded.name = "NicheSlopedSourceMesh";
            return loaded;
        }

        /// <summary>
        /// Closed compartment with a distinctly sloped outer wall so occupancy
        /// free-surface contours are visibly non-rectangular.
        /// </summary>
        private static Mesh BuildSlopedNicheMesh()
        {
            // Local X: 0..2, Z: -1.2..1.2, Y: 0..2.4
            // Outer +X wall slopes inward toward the top (x shrinks with y).
            const float zMin = -1.2f;
            const float zMax = 1.2f;
            const float yMin = 0f;
            const float yMax = 2.4f;
            const float xInner = 0f;
            const float xOuterBottom = 2f;
            const float xOuterTop = 1.1f;

            var v = new List<Vector3>
            {
                // 0-3 floor
                new(xInner, yMin, zMin),
                new(xOuterBottom, yMin, zMin),
                new(xOuterBottom, yMin, zMax),
                new(xInner, yMin, zMax),
                // 4-7 ceiling
                new(xInner, yMax, zMin),
                new(xOuterTop, yMax, zMin),
                new(xOuterTop, yMax, zMax),
                new(xInner, yMax, zMax),
            };

            var t = new List<int>();
            AddQuad(t, 0, 1, 2, 3); // floor
            AddQuad(t, 4, 7, 6, 5); // ceiling
            AddQuad(t, 0, 3, 7, 4); // inner -X
            AddQuad(t, 1, 5, 6, 2); // sloped +X
            AddQuad(t, 0, 4, 5, 1); // -Z
            AddQuad(t, 3, 2, 6, 7); // +Z

            var mesh = new Mesh { name = "NicheSlopedSourceMesh" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void AddQuad(
            List<int> triangles,
            int a,
            int b,
            int c,
            int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private static void BuildRoomShell(
            Transform parent,
            string name,
            Vector3 localCenter,
            float width,
            float length,
            float height,
            Material wallMaterial,
            Material floorMaterial,
            bool openPositiveZ = false,
            bool openNegativeZ = false)
        {
            var shell = new GameObject(name);
            shell.transform.SetParent(parent, false);
            shell.transform.localPosition = localCenter;

            CreateCube(
                "Floor",
                shell.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(width + 0.2f, 0.1f, length + 0.2f),
                floorMaterial,
                keepCollider: true);
            CreateCube(
                "Ceiling",
                shell.transform,
                new Vector3(0f, height + 0.05f, 0f),
                new Vector3(width + 0.2f, 0.1f, length + 0.2f),
                wallMaterial,
                keepCollider: false);
            CreateCube(
                "Left Wall",
                shell.transform,
                new Vector3(-(width * 0.5f + 0.05f), height * 0.5f, 0f),
                new Vector3(0.1f, height + 0.1f, length + 0.2f),
                wallMaterial,
                keepCollider: true);
            CreateCube(
                "Right Wall",
                shell.transform,
                new Vector3(width * 0.5f + 0.05f, height * 0.5f, 0f),
                new Vector3(0.1f, height + 0.1f, length + 0.2f),
                wallMaterial,
                keepCollider: true);

            if (!openNegativeZ)
            {
                CreateCube(
                    "Back Wall",
                    shell.transform,
                    new Vector3(0f, height * 0.5f, -(length * 0.5f + 0.05f)),
                    new Vector3(width + 0.2f, height + 0.1f, 0.1f),
                    wallMaterial,
                    keepCollider: true);
            }

            if (!openPositiveZ)
            {
                CreateCube(
                    "Front Wall",
                    shell.transform,
                    new Vector3(0f, height * 0.5f, length * 0.5f + 0.05f),
                    new Vector3(width + 0.2f, height + 0.1f, 0.1f),
                    wallMaterial,
                    keepCollider: true);
            }
        }

        private static void CreateStairSteps(
            Transform parent,
            Vector3 shaftLocalCenter,
            Quaternion localRotation,
            Vector3 localScale,
            Material floorMaterial)
        {
            var steps = new GameObject("Stair Steps");
            steps.transform.SetParent(parent, false);
            steps.transform.localPosition = shaftLocalCenter;
            steps.transform.localRotation = localRotation;
            steps.transform.localScale = localScale;

            const int stepCount = 8;
            const float rise = 0.4f;
            const float run = 0.28f;
            for (var i = 0; i < stepCount; i++)
            {
                CreateCube(
                    $"Step {i + 1}",
                    steps.transform,
                    new Vector3(0f, rise * (i + 0.5f), -0.7f + (run * i)),
                    new Vector3(1.6f, rise, run),
                    floorMaterial,
                    keepCollider: true);
            }
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

        private static void EnsureSampleScriptsImported()
        {
            Directory.CreateDirectory(PackageSampleFolder);
            Directory.CreateDirectory(SampleFolder);

            CopyIfExists(
                Path.Combine(PackageSampleFolder, "RegionStressBootstrap.cs"),
                Path.Combine(SampleFolder, "RegionStressBootstrap.cs"));
            CopyIfExists(
                Path.Combine(PackageSampleFolder, "README.md"),
                Path.Combine(SampleFolder, "README.md"));

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(
                Path.Combine(SampleFolder, "RegionStressBootstrap.cs")
                    .Replace('\\', '/'),
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void MirrorGeneratedAssetsToPackage()
        {
            Directory.CreateDirectory(PackageSampleFolder);

            string[] fileNames =
            {
                "RegionStressBootstrap.cs",
                "RegionStressBootstrap.cs.meta",
                "README.md",
                "README.md.meta",
                "RegionStress.unity",
                "RegionStress.unity.meta",
                "RegionStressUnderwaterProfile.asset",
                "RegionStressUnderwaterProfile.asset.meta",
                "RegionA FloodRegionData.asset",
                "RegionA FloodRegionData.asset.meta",
                "CorridorStair FloodRegionData.asset",
                "CorridorStair FloodRegionData.asset.meta",
                "RegionB FloodRegionData.asset",
                "RegionB FloodRegionData.asset.meta",
                "NicheSlopedFloodVolumeData.asset",
                "NicheSlopedFloodVolumeData.asset.meta",
                "NicheSlopedSourceMesh.asset",
                "NicheSlopedSourceMesh.asset.meta",
                "Compartment Walls.mat",
                "Compartment Walls.mat.meta",
                "Compartment Floor.mat",
                "Compartment Floor.mat.meta",
                "Region Water.mat",
                "Region Water.mat.meta",
                "Ocean Surface.mat",
                "Ocean Surface.mat.meta",
                "Opening Marker.mat",
                "Opening Marker.mat.meta",
                "Niche Hull.mat",
                "Niche Hull.mat.meta",
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
                    "Kyle.Flooding.URP is unavailable. Region Stress sample "
                    + "will run without the fullscreen underwater pass.");
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
