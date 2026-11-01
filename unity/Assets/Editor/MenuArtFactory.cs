using UnityEditor;

namespace ColonQuest.EditorTools
{
    /// <summary>The import options of the picture of the main menu, made with Muse Image (server/scripts/generate_menu_art.py).</summary>
    public static class MenuArtFactory
    {
        public const string KeyArtPath = "Assets/Resources/Menu/KeyArt.png";

        [MenuItem("ColonQuest/Art/Import Menu Art")]
        public static void Import()
        {
            AssetDatabase.ImportAsset(KeyArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(KeyArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
