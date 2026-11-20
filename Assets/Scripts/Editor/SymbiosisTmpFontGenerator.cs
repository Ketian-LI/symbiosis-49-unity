using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UrbanWildlifeRooms.Editor
{
    /// <summary>
    /// Creates persistent, dynamic SDF assets from the fonts already licensed
    /// and shipped with SYMBIOSIS: 49. The Chinese source font remains embedded
    /// so future localization strings can populate the atlas at runtime.
    /// </summary>
    public static class SymbiosisTmpFontGenerator
    {
        private const string OutputFolder = "Assets/Resources/Fonts/TMP";
        private const string NotoSource = "Assets/Resources/Fonts/NotoSansSC-Regular.ttf";
        private const string NunitoRegularSource = "Assets/Resources/Fonts/Nunito-Regular.ttf";
        private const string NunitoBoldSource = "Assets/Resources/Fonts/Nunito-Bold.ttf";
        private const string NotoAsset = OutputFolder + "/NotoSansSC-Regular SDF.asset";
        private const string NunitoRegularAsset = OutputFolder + "/Nunito-Regular SDF.asset";
        private const string NunitoBoldAsset = OutputFolder + "/Nunito-Bold SDF.asset";
        private const string LatinGlyphs =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        [MenuItem("SYMBIOSIS/Fonts/1 Import TMP Essentials")]
        public static void ImportEssentials()
        {
            if (TMP_Settings.instance != null)
            {
                Debug.Log("[SYMBIOSIS TMP] Essential Resources already present.");
                return;
            }

            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(
                "com.unity.ugui");
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
                throw new InvalidOperationException(
                    "Unity UI package is unavailable; cannot import TMP Essentials.");

            var package = Path.Combine(info.resolvedPath, "Package Resources",
                "TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
                throw new FileNotFoundException("TMP Essentials package was not found.", package);

            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[SYMBIOSIS TMP] Imported TMP Essential Resources. " +
                "Run generation after Unity finishes importing.");
        }

        [MenuItem("SYMBIOSIS/Fonts/2 Generate and Check Font Assets")]
        public static void GenerateAndCheck()
        {
            if (TMP_Settings.instance == null)
                throw new InvalidOperationException(
                    "TMP Essential Resources are missing. Run step 1 first.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath,
                "Resources", "Fonts", "TMP"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var noto = GetOrCreate(NotoSource, NotoAsset);
            var regular = GetOrCreate(NunitoRegularSource, NunitoRegularAsset);
            var bold = GetOrCreate(NunitoBoldSource, NunitoBoldAsset);

            AddFallback(regular, noto);
            AddFallback(bold, noto);

            var projectGlyphs = CollectProjectUnicodeCharacters();
            var source = AssetDatabase.LoadAssetAtPath<Font>(NotoSource);
            var missingInSource = projectGlyphs.Where(c => !source.HasCharacter(c)).ToArray();
            if (missingInSource.Length != 0)
                throw new InvalidOperationException(
                    $"Noto Sans SC lacks {missingInSource.Length} project Unicode characters: " +
                    new string(missingInSource));

            var newUnicode = projectGlyphs.Where(c => !noto.HasCharacter(c, false)).ToArray();
            if (newUnicode.Length != 0 &&
                !noto.TryAddCharacters(new string(newUnicode), out var missingInAtlas))
                throw new InvalidOperationException(
                    "Noto Sans SC atlas could not add: " + missingInAtlas);
            var newRegular = LatinGlyphs.Where(c => !regular.HasCharacter(c, false)).ToArray();
            if (newRegular.Length != 0 &&
                !regular.TryAddCharacters(new string(newRegular), out var missingRegular))
                throw new InvalidOperationException(
                    "Nunito Regular atlas could not add: " + missingRegular);
            var newBold = LatinGlyphs.Where(c => !bold.HasCharacter(c, false)).ToArray();
            if (newBold.Length != 0 &&
                !bold.TryAddCharacters(new string(newBold), out var missingBold))
                throw new InvalidOperationException(
                    "Nunito Bold atlas could not add: " + missingBold);

            EnsureAtlasSubassets(noto);
            EnsureAtlasSubassets(regular);
            EnsureAtlasSubassets(bold);
            EditorUtility.SetDirty(noto);
            EditorUtility.SetDirty(regular);
            EditorUtility.SetDirty(bold);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CheckPersisted(NotoAsset, projectGlyphs);
            CheckPersisted(NunitoRegularAsset, LatinGlyphs.ToCharArray());
            CheckPersisted(NunitoBoldAsset, LatinGlyphs.ToCharArray());
            Debug.Log($"[SYMBIOSIS TMP] Saved 3 dynamic SDF assets. " +
                $"Project Unicode glyphs: {projectGlyphs.Length}; missing: 0. " +
                "Nunito Regular and Bold fall back to Noto Sans SC.");
        }

        [MenuItem("SYMBIOSIS/Fonts/3 Recheck Saved Font Assets")]
        public static void RecheckSaved()
        {
            CheckPersisted(NotoAsset, CollectProjectUnicodeCharacters());
            CheckPersisted(NunitoRegularAsset, LatinGlyphs.ToCharArray());
            CheckPersisted(NunitoBoldAsset, LatinGlyphs.ToCharArray());
            Debug.Log("[SYMBIOSIS TMP] Saved font assets and current UI glyphs verified.");
        }

        private static TMP_FontAsset GetOrCreate(string fontPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                if (existing.atlasPopulationMode != AtlasPopulationMode.Dynamic ||
                    existing.sourceFontFile == null)
                    throw new InvalidOperationException(
                        "Existing TMP font asset is not a dynamic asset: " + assetPath);
                PreserveGeneratedAtlas(existing);
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (source == null)
                throw new FileNotFoundException("Bundled source font is missing.", fontPath);

            var asset = TMP_FontAsset.CreateFontAsset(source, 72, 8,
                GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            if (asset == null || asset.atlasTexture == null || asset.material == null)
                throw new InvalidOperationException(
                    "TMP could not create an SDF asset from " + fontPath);

            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            asset.atlasTexture.name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            PreserveGeneratedAtlas(asset);
            AssetDatabase.CreateAsset(asset, assetPath);
            EnsureAtlasSubassets(asset);
            return asset;
        }

        private static void AddFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
        {
            if (primary.fallbackFontAssetTable == null)
                primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!primary.fallbackFontAssetTable.Contains(fallback))
                primary.fallbackFontAssetTable.Add(fallback);
        }

        private static void PreserveGeneratedAtlas(TMP_FontAsset asset)
        {
            // The current TMP package exposes this setting to its own Editor
            // assembly only, so set the serialized field through Unity's API.
            var serialized = new SerializedObject(asset);
            var clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild == null)
                throw new InvalidOperationException(
                    "TMP atlas preservation setting was not found.");
            clearOnBuild.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureAtlasSubassets(TMP_FontAsset asset)
        {
            foreach (var texture in asset.atlasTextures)
                if (texture != null && !AssetDatabase.Contains(texture))
                    AssetDatabase.AddObjectToAsset(texture, asset);
            if (asset.material != null && !AssetDatabase.Contains(asset.material))
                AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        private static char[] CollectProjectUnicodeCharacters()
        {
            var scripts = Path.Combine(Application.dataPath, "Scripts");
            var glyphs = new SortedSet<char>();
            foreach (var path in Directory.EnumerateFiles(scripts, "*.cs",
                         SearchOption.AllDirectories))
                foreach (var character in File.ReadAllText(path))
                    if (character > 127)
                        glyphs.Add(character);
            return glyphs.ToArray();
        }

        private static void CheckPersisted(string path, char[] characters)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            var resourcePath = path.Substring("Assets/Resources/".Length)
                .Replace(".asset", string.Empty);
            var runtimeAsset = Resources.Load<TMP_FontAsset>(resourcePath);
            if (asset == null || asset.sourceFontFile == null ||
                asset.atlasPopulationMode != AtlasPopulationMode.Dynamic ||
                asset.atlasTexture == null || asset.material == null ||
                runtimeAsset != asset)
                throw new InvalidOperationException(
                    "TMP font asset did not persist correctly: " + path);
            var missing = characters.Where(c => !asset.HasCharacter(c, true)).ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException(
                    $"TMP asset {path} is missing {missing.Length} glyphs: " +
                    new string(missing));
        }
    }
}
