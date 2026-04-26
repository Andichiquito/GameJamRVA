using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WebGLAssetOptimizer
{
    private static readonly string[] TargetFolders =
    {
        "Assets/tragamonedas"
    };

    [MenuItem("Casino/Optimize/Apply WebGL Asset Optimization")]
    public static void ApplyWebGLOptimization()
    {
        int texturesTouched = OptimizeTextures();
        int modelsTouched = OptimizeModels();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[WebGLAssetOptimizer] Done. Textures updated: {texturesTouched}, Models updated: {modelsTouched}.");
    }

    private static int OptimizeTextures()
    {
        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", TargetFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool dirty = false;

            if (importer.textureType == TextureImporterType.Default)
            {
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                dirty = true;
            }

            TextureImporterPlatformSettings webgl = importer.GetPlatformTextureSettings("WebGL");
            if (string.IsNullOrEmpty(webgl.name))
            {
                webgl.name = "WebGL";
            }

            int desiredMaxSize = GetMaxTextureSize(path);
            if (!webgl.overridden || webgl.maxTextureSize != desiredMaxSize)
            {
                dirty = true;
            }

            webgl.overridden = true;
            webgl.maxTextureSize = desiredMaxSize;
            webgl.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            webgl.format = TextureImporterFormat.Automatic;
            webgl.textureCompression = TextureImporterCompression.Compressed;
            webgl.compressionQuality = 50;

            importer.SetPlatformTextureSettings(webgl);

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        return changed;
    }

    private static int OptimizeModels()
    {
        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", TargetFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            bool dirty = false;

            if (importer.isReadable)
            {
                importer.isReadable = false;
                dirty = true;
            }

            if (importer.meshCompression != ModelImporterMeshCompression.Medium)
            {
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                dirty = true;
            }

            if (!importer.optimizeMeshPolygons)
            {
                importer.optimizeMeshPolygons = true;
                dirty = true;
            }

            if (!importer.optimizeMeshVertices)
            {
                importer.optimizeMeshVertices = true;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        return changed;
    }

    private static int GetMaxTextureSize(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("normal"))
        {
            return 1024;
        }

        if (lower.Contains("basecolor") || lower.Contains("albedo") || lower.Contains("diffuse"))
        {
            return 1024;
        }

        return 512;
    }
}
