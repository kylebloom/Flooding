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
    /// Builds the authored Baked Geometry hull-section sample.
    /// </summary>
    internal static class BakedGeometrySampleBuilder
    {
        // AssetDatabase can create/load Mesh assets reliably under Assets/Samples.
        // Generated files are mirrored into the package Samples~ folder at the end.
        private const string SampleFolder =
            "Assets/Samples/Flooding/0.9.1/Baked Geometry";

        private const string PackageSampleFolder =
            "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Baked Geometry";

        private const float HalfWidth = 2f;
        private const float Height = 2.5f;
        private const float HalfLength = 1.5f;
        // Deep bilge so retained cells leave empty bottom corners inside the AABB.
        private const float BilgeRadius = 1.35f;
        private const float CellResolution = 0.4f;
        private const int BilgeSegments = 10;
        private const int LengthSegments = 4;

        [MenuItem("Flooding/Internal/Build Baked Geometry Sample", priority = 2002)]
        public static void Build()
        {
            TryBuild();
        }

        public static bool TryBuild()
        {
            Directory.CreateDirectory(SampleFolder);

            var bootstrapScript = LoadSampleScript("BakedGeometrySampleBootstrap.cs");
            if (bootstrapScript == null || bootstrapScript.GetClass() == null)
            {
                Debug.LogError(
                    "Baked Geometry sample script was not found. Copy or import "
                    + "Baked Geometry into Assets/Samples, then rebuild.");
                return false;
            }

            DeleteAssetIfExists("SlopedCompartmentFloodVolumeData.asset");
            DeleteAssetIfExists("BakedGeometryStructure.mat");

            var hullMaterial = CreateLitMaterial(
                "Hull Structure",
                new Color(0.45f, 0.48f, 0.52f, 0.35f),
                transparent: true);
            var waterMaterial = CreateLitMaterial(
                "Compartment Water",
                new Color(0.1f, 0.45f, 0.85f, 0.55f),
                transparent: true);
            var cellsMaterial = CreateLitMaterial(
                "Baked Cells",
                new Color(1f, 0.75f, 0.15f, 0.45f),
                transparent: true);

            var sourceMesh = CreateOrUpdateHullSectionMesh();
            if (!FloodVolumeBaker.TryValidateClosedMesh(sourceMesh, out var meshMessage))
            {
                Debug.LogError(
                    "Generated hull-section mesh failed closed-mesh validation: "
                    + meshMessage);
                return false;
            }

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("Baked Geometry Sample");
            var manager = root.AddComponent<FloodSimulationManager>();
            manager.SimulateAutomatically = true;
            var bootstrap = root.AddComponent(bootstrapScript.GetClass());

            var compartment = new GameObject("Hull Section Compartment");
            compartment.transform.SetParent(root.transform, false);

            var volume = compartment.AddComponent<FloodVolume>();
            volume.SimulationManager = manager;

            var sourceObject = new GameObject("Authoring Source Mesh");
            sourceObject.transform.SetParent(compartment.transform, false);
            var sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = sourceMesh;
            var sourceRenderer = sourceObject.AddComponent<MeshRenderer>();
            sourceRenderer.sharedMaterial = hullMaterial;

            var bakedData = GetOrCreateBakedDataAsset();
            var authoring = compartment.AddComponent<FloodVolumeAuthoring>();
            var authoringSerialized = new SerializedObject(authoring);
            authoringSerialized.FindProperty("targetVolume")
                .objectReferenceValue = volume;
            authoringSerialized.FindProperty("sourceMeshFilter")
                .objectReferenceValue = sourceFilter;
            authoringSerialized.FindProperty("cellResolution").floatValue =
                CellResolution;
            authoringSerialized.FindProperty("maximumGridCells").intValue =
                1000000;
            authoringSerialized.FindProperty("bakedData")
                .objectReferenceValue = bakedData;
            authoringSerialized.FindProperty("visualizeBake").boolValue = true;
            authoringSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (!FloodVolumeBaker.TryBake(authoring, out bakedData, out var bakeMessage))
            {
                Debug.LogError("Baked Geometry sample bake failed: " + bakeMessage);
                return false;
            }

            Debug.Log("Baked Geometry sample bake: " + bakeMessage);

            var volumeSerialized = new SerializedObject(volume);
            volumeSerialized.FindProperty("initialVolume").floatValue =
                (float)(bakedData.Capacity * 0.5d);
            volumeSerialized.ApplyModifiedPropertiesWithoutUndo();

            var waterObject = new GameObject("Baked Water Surface");
            waterObject.transform.SetParent(compartment.transform, false);
            var waterFilter = waterObject.AddComponent<MeshFilter>();
            var waterRenderer = waterObject.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = waterMaterial;

            var surfaceRenderer = compartment.AddComponent<FloodBakedSurfaceRenderer>();
            surfaceRenderer.SourceVolume = volume;
            surfaceRenderer.WaterMeshFilter = waterFilter;

            var cellsMesh = CreateOccupiedCellsMesh(bakedData);
            var cellsPath = Path.Combine(SampleFolder, "HullSectionBakedCells.asset")
                .Replace('\\', '/');
            var savedCellsMesh = SaveMeshAsset(cellsMesh, cellsPath);

            var cellsObject = new GameObject("Baked Cells Presentation");
            cellsObject.transform.SetParent(compartment.transform, false);
            cellsObject.SetActive(false);
            var cellsFilter = cellsObject.AddComponent<MeshFilter>();
            cellsFilter.sharedMesh = savedCellsMesh;
            var cellsRenderer = cellsObject.AddComponent<MeshRenderer>();
            cellsRenderer.sharedMaterial = cellsMaterial;

            var bootstrapSerialized = new SerializedObject(bootstrap);
            bootstrapSerialized.FindProperty("floodVolume")
                .objectReferenceValue = volume;
            bootstrapSerialized.FindProperty("bakedData")
                .objectReferenceValue = bakedData;
            bootstrapSerialized.FindProperty("bakedCellsPresentation")
                .objectReferenceValue = cellsObject;
            bootstrapSerialized.FindProperty("animateFill").boolValue = true;
            bootstrapSerialized.FindProperty("animateRoll").boolValue = true;
            bootstrapSerialized.FindProperty("minimumFillFraction").floatValue =
                0.28f;
            bootstrapSerialized.FindProperty("maximumFillFraction").floatValue =
                0.72f;
            bootstrapSerialized.FindProperty("fillRate").floatValue = 1.5f;
            bootstrapSerialized.FindProperty("rollDegrees").floatValue = 10f;
            bootstrapSerialized.FindProperty("rollPeriod").floatValue = 7f;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.2f, 1f);
            cameraObject.transform.position = new Vector3(6.5f, 3.8f, -6.5f);
            cameraObject.transform.rotation = Quaternion.Euler(20f, -45f, 0f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var scenePath = Path.Combine(SampleFolder, "BakedGeometry.unity")
                .Replace('\\', '/');
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                scenePath);
            AssetDatabase.SaveAssets();
            MirrorGeneratedAssetsToPackage();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Built Baked Geometry sample at {scenePath} "
                + $"(mirrored to {PackageSampleFolder}). "
                + $"Retained cells={bakedData.SampleCount}, "
                + $"capacity={bakedData.Capacity:0.###} m³.");
            return true;
        }

        private static Mesh CreateOrUpdateHullSectionMesh()
        {
            var mesh = BuildHullSectionMesh();
            var path = Path.Combine(SampleFolder, "HullSectionSourceMesh.asset")
                .Replace('\\', '/');
            var saved = SaveMeshAsset(mesh, path);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "Failed to save HullSectionSourceMesh.asset under Samples~/.");
            }

            saved.name = "HullSectionSourceMesh";
            EditorUtility.SetDirty(saved);
            return saved;
        }

        private static Mesh BuildHullSectionMesh()
        {
            var profile = BuildHullProfile();
            var ringCount = LengthSegments + 1;
            var pointsPerRing = profile.Count;
            var vertices = new List<Vector3>(ringCount * pointsPerRing);
            var triangles = new List<int>();

            for (var ring = 0; ring < ringCount; ring++)
            {
                var z = Mathf.Lerp(
                    -HalfLength,
                    HalfLength,
                    ring / (float)LengthSegments);
                for (var point = 0; point < pointsPerRing; point++)
                {
                    var profilePoint = profile[point];
                    vertices.Add(new Vector3(
                        profilePoint.x,
                        profilePoint.y - (Height * 0.5f),
                        z));
                }
            }

            for (var ring = 0; ring < LengthSegments; ring++)
            {
                var ringStart = ring * pointsPerRing;
                var nextRingStart = (ring + 1) * pointsPerRing;
                for (var point = 0; point < pointsPerRing; point++)
                {
                    var nextPoint = (point + 1) % pointsPerRing;
                    var first = ringStart + point;
                    var second = ringStart + nextPoint;
                    var third = nextRingStart + nextPoint;
                    var fourth = nextRingStart + point;
                    triangles.Add(first);
                    triangles.Add(second);
                    triangles.Add(third);
                    triangles.Add(first);
                    triangles.Add(third);
                    triangles.Add(fourth);
                }
            }

            CapProfile(
                triangles,
                ringOffset: 0,
                pointsPerRing,
                reverse: true);
            CapProfile(
                triangles,
                ringOffset: LengthSegments * pointsPerRing,
                pointsPerRing,
                reverse: false);

            var mesh = new Mesh
            {
                name = "HullSectionSourceMesh",
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static List<Vector2> BuildHullProfile()
        {
            // CCW when viewed from +Z: keel → starboard bilge/wall → deck → port wall/bilge.
            var profile = new List<Vector2>();
            var flatHalf = Mathf.Max(0.05f, HalfWidth - BilgeRadius);

            profile.Add(new Vector2(-flatHalf, 0f));
            profile.Add(new Vector2(flatHalf, 0f));

            var starboardCenter = new Vector2(flatHalf, BilgeRadius);
            for (var segment = 1; segment <= BilgeSegments; segment++)
            {
                var angle = Mathf.Lerp(
                    -Mathf.PI * 0.5f,
                    0f,
                    segment / (float)BilgeSegments);
                profile.Add(starboardCenter + new Vector2(
                    Mathf.Cos(angle) * BilgeRadius,
                    Mathf.Sin(angle) * BilgeRadius));
            }

            profile.Add(new Vector2(HalfWidth, Height));
            profile.Add(new Vector2(-HalfWidth, Height));
            profile.Add(new Vector2(-HalfWidth, BilgeRadius));

            var portCenter = new Vector2(-flatHalf, BilgeRadius);
            // Exclude the final angle (3π/2): it duplicates the keel-port start point.
            for (var segment = 1; segment < BilgeSegments; segment++)
            {
                var angle = Mathf.Lerp(
                    Mathf.PI,
                    Mathf.PI * 1.5f,
                    segment / (float)BilgeSegments);
                profile.Add(portCenter + new Vector2(
                    Mathf.Cos(angle) * BilgeRadius,
                    Mathf.Sin(angle) * BilgeRadius));
            }

            return profile;
        }

        private static void CapProfile(
            List<int> triangles,
            int ringOffset,
            int pointsPerRing,
            bool reverse)
        {
            for (var point = 1; point < pointsPerRing - 1; point++)
            {
                var first = ringOffset;
                var second = ringOffset + point;
                var third = ringOffset + point + 1;
                if (reverse)
                {
                    triangles.Add(first);
                    triangles.Add(third);
                    triangles.Add(second);
                }
                else
                {
                    triangles.Add(first);
                    triangles.Add(second);
                    triangles.Add(third);
                }
            }
        }

        private static Mesh CreateOccupiedCellsMesh(FloodVolumeData data)
        {
            var occupied = data.OccupiedCellIndices;
            var half = data.CellSize * 0.46f;
            var vertices = new List<Vector3>(occupied.Count * 8);
            var triangles = new List<int>(occupied.Count * 36);

            foreach (var flattenedIndex in occupied)
            {
                var center = data.GetCellCenter(flattenedIndex);
                var baseIndex = vertices.Count;
                vertices.Add(center + new Vector3(-half.x, -half.y, -half.z));
                vertices.Add(center + new Vector3(half.x, -half.y, -half.z));
                vertices.Add(center + new Vector3(half.x, half.y, -half.z));
                vertices.Add(center + new Vector3(-half.x, half.y, -half.z));
                vertices.Add(center + new Vector3(-half.x, -half.y, half.z));
                vertices.Add(center + new Vector3(half.x, -half.y, half.z));
                vertices.Add(center + new Vector3(half.x, half.y, half.z));
                vertices.Add(center + new Vector3(-half.x, half.y, half.z));

                AddQuad(triangles, baseIndex, 0, 1, 2, 3);
                AddQuad(triangles, baseIndex, 5, 4, 7, 6);
                AddQuad(triangles, baseIndex, 4, 0, 3, 7);
                AddQuad(triangles, baseIndex, 1, 5, 6, 2);
                AddQuad(triangles, baseIndex, 3, 2, 6, 7);
                AddQuad(triangles, baseIndex, 4, 5, 1, 0);
            }

            var mesh = new Mesh
            {
                name = "HullSectionBakedCells",
                indexFormat = vertices.Count > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void AddQuad(
            List<int> triangles,
            int baseIndex,
            int a,
            int b,
            int c,
            int d)
        {
            triangles.Add(baseIndex + a);
            triangles.Add(baseIndex + b);
            triangles.Add(baseIndex + c);
            triangles.Add(baseIndex + a);
            triangles.Add(baseIndex + c);
            triangles.Add(baseIndex + d);
        }

        private static FloodVolumeData GetOrCreateBakedDataAsset()
        {
            var path = Path.Combine(SampleFolder, "HullSectionFloodVolumeData.asset")
                .Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<FloodVolumeData>(path);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            AssetDatabase.CreateAsset(data, path);
            return AssetDatabase.LoadAssetAtPath<FloodVolumeData>(path);
        }

        private static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();

            var loaded = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (loaded != null)
                return loaded;

            Debug.LogError(
                $"Failed to load mesh asset at '{path}' after CreateAsset.");
            return null;
        }

        private static void MirrorGeneratedAssetsToPackage()
        {
            Directory.CreateDirectory(PackageSampleFolder);

            string[] fileNames =
            {
                "BakedGeometry.unity",
                "HullSectionSourceMesh.asset",
                "HullSectionBakedCells.asset",
                "HullSectionFloodVolumeData.asset",
                "Hull Structure.mat",
                "Compartment Water.mat",
                "Baked Cells.mat",
                "BakedGeometrySampleBootstrap.cs",
                "README.md",
            };

            foreach (var fileName in fileNames)
            {
                var source = Path.Combine(SampleFolder, fileName);
                var destination = Path.Combine(PackageSampleFolder, fileName);
                if (!File.Exists(source))
                    continue;

                File.Copy(source, destination, overwrite: true);

                var sourceMeta = source + ".meta";
                var destinationMeta = destination + ".meta";
                if (File.Exists(sourceMeta))
                    File.Copy(sourceMeta, destinationMeta, overwrite: true);
            }

            DeletePackageFileIfExists("SlopedCompartmentFloodVolumeData.asset");
            DeletePackageFileIfExists("SlopedCompartmentFloodVolumeData.asset.meta");
            DeletePackageFileIfExists("BakedGeometryStructure.mat");
            DeletePackageFileIfExists("BakedGeometryStructure.mat.meta");
        }

        private static void DeletePackageFileIfExists(string fileName)
        {
            var path = Path.Combine(PackageSampleFolder, fileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void DeleteAssetIfExists(string fileName)
        {
            var path = Path.Combine(SampleFolder, fileName).Replace('\\', '/');
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static MonoScript LoadSampleScript(string fileName)
        {
            var packagePath = Path.Combine(SampleFolder, fileName)
                .Replace('\\', '/');
            var importedPath =
                "Assets/Samples/Flooding/0.9.1/Baked Geometry/" + fileName;
            return AssetDatabase.LoadAssetAtPath<MonoScript>(importedPath)
                ?? AssetDatabase.LoadAssetAtPath<MonoScript>(packagePath);
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
                UnityEngine.Object.DestroyImmediate(material);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
#endif
