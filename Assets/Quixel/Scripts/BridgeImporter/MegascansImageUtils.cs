#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;

namespace Quixel
{
    public class MegascansImageUtils : MonoBehaviour
    {

        public static int width = 0;
        public static int height = 0;

        /// <summary>
        /// reads a texture file straight from hard drive absolute path, converts it to a Unity texture.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D ImportJPG(string sourcePath)
        {
            try
            {
                if (sourcePath == null)
                {
                    return null;
                }
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning("Could not find " + sourcePath + "\nPlease make sure it is downloaded.");
                    return null;
                }
                byte[] texData = File.ReadAllBytes(sourcePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBAFloat, true);
                tex.LoadImage(texData);
                width = tex.width;
                height = tex.height;
                return tex;
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Import JPG:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
                return null;
            }
        }

        /// <summary>
        /// used for packing an alpha channel into an existing RGB texture. Uses native Unity API calls, can't be multithreaded, and is considerably slower than our own method.
        /// Currently has to run if using Mac OSX as there is no support for the system.imaging library on that operating system.
        /// Will fail if textures aren't the same resolution.
        /// </summary>
        /// <param name="cPixels"></param>
        /// <param name="aPixels"></param>
        /// <param name="invertAlpha"></param>
        /// <returns></returns>
        public static Texture2D PackTextures(string rgbPath, string aPath, string savePath, bool invertAlpha = false)
        {
            try
            {
                if ((rgbPath == null) && (aPath == null))
                {
                    return null;
                }
                UnityEngine.Color[] rgbCols = ImportJPG(rgbPath) != null ? ImportJPG(rgbPath).GetPixels() : null;
                UnityEngine.Color[] aCols = ImportJPG(aPath) != null ? ImportJPG(aPath).GetPixels() : null;
                UnityEngine.Color[] rgbaCols = new UnityEngine.Color[width * height];
                for (int i = 0; i < width * height; ++i)
                {
                    rgbaCols[i] = rgbCols != null ? rgbCols[i] : new UnityEngine.Color(1.0f, 1.0f, 1.0f);
                    rgbaCols[i].a = aCols != null ? ((aCols[i].r + aCols[i].g + aCols[i].b) / 3.0f) : 1.0f;
                    rgbaCols[i].a = invertAlpha ? 1.0f - rgbaCols[i].a : rgbaCols[i].a;
                }
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                tex.SetPixels(rgbaCols);
                File.WriteAllBytes(savePath, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(savePath);
                DestroyImmediate(tex);
                return TextureImportSetup(savePath, false);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Channel Pack RGB + A:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
                return null;
            }
        }

        /// <summary>
        /// used for packing multiple textures into an RGBA mask texture. Uses native Unity API calls, can't be multithreaded, and is considerably slower than our own method.
        /// Currently has to run if using Mac OSX as there is no support for the system.imaging library on that operating system.
        /// Will fail if textures aren't the same resolution.
        /// </summary>
        /// <param name="rPixels"></param>
        /// <param name="gPixels"></param>
        /// <param name="bPixels"></param>
        /// <param name="aPixels"></param>
        /// <param name="invertAlpha"></param>
        /// <returns></returns>
        public static Texture2D PackTextures(string rPath, string gPath, string bPath, string aPath, string savePath, bool invertAlpha = false)
        {
            try
            {
                if ((rPath == null) && (gPath == null) && (bPath == null) && (aPath == null))
                {
                    return null;
                }
                UnityEngine.Color[] rCols = ImportJPG(rPath) != null ? ImportJPG(rPath).GetPixels() : null;
                UnityEngine.Color[] gCols = ImportJPG(gPath) != null ? ImportJPG(gPath).GetPixels() : null;
                UnityEngine.Color[] bCols = ImportJPG(bPath) != null ? ImportJPG(bPath).GetPixels() : null;
                UnityEngine.Color[] aCols = ImportJPG(aPath) != null ? ImportJPG(aPath).GetPixels() : null;
                UnityEngine.Color[] rgbaCols = new UnityEngine.Color[width * height];
                for (int i = 0; i < width * height; ++i)
                {
                    rgbaCols[i].r = rCols != null ? (rCols[i].r + rCols[i].g + rCols[i].b) / 3.0f : 0.0f;
                    rgbaCols[i].g = gCols != null ? (gCols[i].r + gCols[i].g + gCols[i].b) / 3.0f : 1.0f;
                    rgbaCols[i].b = bCols != null ? (bCols[i].r + bCols[i].g + bCols[i].b) / 3.0f : 0.0f;
                    rgbaCols[i].a = aCols != null ? (aCols[i].r + aCols[i].g + aCols[i].b) / 3.0f : 1.0f;
                    rgbaCols[i].a = invertAlpha ? 1.0f - rgbaCols[i].a : rgbaCols[i].a;
                }
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                tex.SetPixels(rgbaCols);
                File.WriteAllBytes(savePath, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(savePath);
                DestroyImmediate(tex);
                return TextureImportSetup(savePath, false, false);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Channel Pack R+G+B+A:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
                return null;
            }
        }

        /// <summary>
        /// Sets the import settings for textures, normalmap, sRGB etc.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="normalMap"></param>
        /// <param name="sRGB"></param>
        public static Texture2D TextureImportSetup(string assetPath, bool normalMap, bool sRGB = true)
        {
            try
            {
                TextureImporter tImp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImp == null)
                {
                    return null;
                }
                int importResolution = Convert.ToInt32(Math.Pow(2, 9 + EditorPrefs.GetInt("QuixelDefaultImportResolution", 4)));
                tImp.maxTextureSize = importResolution;
                tImp.sRGBTexture = sRGB;
                tImp.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                AssetDatabase.ImportAsset(assetPath);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Texture Import Setup:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
                return null;
            }
        }

        /// <summary>
        /// Set texture properties for 3d plant here.
        /// </summary>
        public static void SetTexPropsPlants(string texPath, float alphaCutoff)
        {
            TextureImporter tImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (tImp == null)
            {
                return;
            }
            tImp.mipMapsPreserveCoverage = true;
            tImp.alphaIsTransparency = true;
            tImp.alphaTestReferenceValue = alphaCutoff;
            AssetDatabase.ImportAsset(texPath);
            AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        /// <summary>
        /// literally just write the file to disk.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="assetPath"></param>
        public static void CreateTexture(string tex, string assetPath)
        {
            try
            {
                if ((tex == null) || (File.Exists(tex) == false))
                {
                    return;
                }
                Texture2D t = ImportJPG(tex);
                File.WriteAllBytes(assetPath, t.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Create Texture:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
            }
        }

        /// <summary>
        /// used for packing an alpha channel into an existing RGB texture. Uses native Unity API calls, can't be multithreaded, and is considerably slower than our own method.
        /// Currently has to run if using Mac OSX as there is no support for the system.imaging library on that operating system.
        /// Will fail if textures aren't the same resolution.
        /// </summary>
        /// <param name="cPixels"></param>
        /// <param name="invertAlpha"></param>
        /// <returns></returns>
        public static void ImportTerrainNormal(string sourcepath, string destPath)
        {
            try
            {
                if (sourcepath == null)
                {
                    return;
                }
                UnityEngine.Color[] rgbCols = ImportJPG(sourcepath) != null ? ImportJPG(sourcepath).GetPixels() : null;
                UnityEngine.Color[] rgbaCols = new UnityEngine.Color[width * height];
                for (int i = 0; i < width * height; ++i)
                {
                    rgbaCols[i] = rgbCols != null ? rgbCols[i] : new UnityEngine.Color(1.0f, 1.0f, 1.0f);
                    rgbaCols[i].g = 1.0f - rgbaCols[i].g;
                    rgbaCols[i].a = 1.0f;
                }
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                tex.SetPixels(rgbaCols);
                File.WriteAllBytes(destPath, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(destPath);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Generate Terrain Normal:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
            }
        }

        /// <summary>
        /// Invert green channel of the selected texture 2D
        /// </summary>
        public static void FlipGreenChannel()
        {
            try
            {
                string sourcePath = MegascansUtilities.GetSelectedTexture();
                
                if (sourcePath == null)
                    return;

                EditorUtility.DisplayProgressBar("Bridge Plugin", "Flipping green channel...", 0.5f);
                UnityEngine.Color[] rgbCols = ImportJPG(sourcePath).GetPixels();
                UnityEngine.Color[] rgbaCols = new UnityEngine.Color[width * height];
                for (int i = 0; i < width * height; ++i)
                {
                    rgbaCols[i] = rgbCols != null ? rgbCols[i] : new UnityEngine.Color(1.0f, 1.0f, 1.0f);
                    rgbaCols[i].g = 1.0f - rgbaCols[i].g;
                    rgbaCols[i].a = 1.0f;
                }
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                tex.SetPixels(rgbaCols);
                File.WriteAllBytes(sourcePath, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(sourcePath);
                MegascansUtilities.HideProgressBar();
                Debug.Log("Successfully flipped green channel.");
            }
            catch (Exception ex)
            {
                Debug.Log("Exception::MegascansImageUtils::Flip Green Channel:: " + ex.ToString());
                MegascansUtilities.HideProgressBar();
            }
        }
    }

    public struct MSTextureData
    {
        public string path;
        public string type;
        public bool imported;

        public MSTextureData(string path, string type, bool imported = false)
        {
            this.path = path;
            this.type = type;
            this.imported = imported;
        }
    }
}

#endif
