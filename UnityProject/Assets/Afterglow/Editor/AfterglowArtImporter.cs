using UnityEditor;
using UnityEngine;

namespace Afterglow.Editor
{
    public sealed class AfterglowArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Afterglow/Resources/Art/")) return;
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Default;
            texture.sRGBTexture = true;
            texture.alphaIsTransparency = true;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.mipmapEnabled = assetPath.Contains("World_");
            texture.maxTextureSize = assetPath.Contains("World_") ? 8192 : 4096;
            texture.textureCompression = TextureImporterCompression.CompressedHQ;
            texture.compressionQuality = 100;
        }
    }
}
