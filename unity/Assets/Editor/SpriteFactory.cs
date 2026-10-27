using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Generates the white, anti-aliased base shapes (rounded rectangles, circle, ring)
    /// that the Among Us style map tints with <see cref="SpriteRenderer.color"/>.
    /// </summary>
    public static class SpriteFactory
    {
        public const string Folder = "Assets/Art/Sprites";
        public const string RoundedRectPath = Folder + "/RoundedRect.png";
        public const string RoundedRectSmallPath = Folder + "/RoundedRectSmall.png";
        public const string CirclePath = Folder + "/Circle.png";
        public const string RingPath = Folder + "/Ring.png";

        [MenuItem("Colon Quest/Art/Generate Sprites")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);

            // Rounded rectangles are 9-sliced: the corner radius stays fixed at any size.
            Write(RoundedRectPath, 64, 64, 20, (x, y) => RoundedRectAlpha(x, y, 64, 20));
            Write(RoundedRectSmallPath, 32, 64, 8, (x, y) => RoundedRectAlpha(x, y, 32, 8));
            Write(CirclePath, 128, 128, 0, (x, y) => CircleAlpha(x, y, 128));
            Write(RingPath, 128, 128, 0, (x, y) => RingAlpha(x, y, 128, 12f));
            AssetDatabase.Refresh();
        }

        public static void EnsureGenerated()
        {
            if (!File.Exists(RoundedRectPath) || !File.Exists(RoundedRectSmallPath)
                || !File.Exists(CirclePath) || !File.Exists(RingPath))
            {
                Generate();
            }
        }

        private static float RoundedRectAlpha(float x, float y, int size, float radius)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(x - half) - (half - 1f - radius);
            float qy = Mathf.Abs(y - half) - (half - 1f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static float CircleAlpha(float x, float y, int size)
        {
            float half = size * 0.5f;
            float distance = new Vector2(x - half, y - half).magnitude - (half - 1f);
            return Mathf.Clamp01(0.5f - distance);
        }

        private static float RingAlpha(float x, float y, int size, float thickness)
        {
            float half = size * 0.5f;
            float middle = half - 1f - thickness * 0.5f;
            float distance = Mathf.Abs(new Vector2(x - half, y - half).magnitude - middle) - thickness * 0.5f;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static void Write(string path, int size, int pixelsPerUnit, int border, Func<float, float, float> alphaAt)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alphaAt(x + 0.5f, y + 0.5f)));
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
