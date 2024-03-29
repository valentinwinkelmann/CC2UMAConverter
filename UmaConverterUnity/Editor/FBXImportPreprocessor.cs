using UnityEngine;
using UnityEditor;
using System.IO;

namespace UMAConverter
{
    // AssetPostprocessor bietet eine Möglichkeit, auf verschiedene Importereignisse für Assets zuzugreifen.
    public class FBXImportPreprocessor : AssetPostprocessor
    {
        // Diese Methode wird aufgerufen, bevor ein Modell importiert wird.
        private void OnPreprocessModel()
        {
            if (assetPath.ToLower().EndsWith(".fbx"))
            {
                string basePath = assetPath.Substring(0, assetPath.LastIndexOf('.'));
                string raceJsonFilePath = basePath + "_race.json";
                string clothJsonFilePath = basePath + "_cloth.json";

                // Initialisiere assetType als leeren String.
                string assetType = string.Empty;

                // Überprüfe, ob die entsprechenden JSON-Dateien existieren.
                if (File.Exists(raceJsonFilePath))
                {
                    assetType = "_race";
                }
                else if (File.Exists(clothJsonFilePath))
                {
                    assetType = "_cloth";
                }

                if (!string.IsNullOrEmpty(assetType))
                {
                    string jsonFilePath = assetPath.Replace(".fbx", $"{assetType}.json");
                    Debug.Log(jsonFilePath);

                    // Überprüfe, ob die JSON-Datei existiert.
                    if (File.Exists(jsonFilePath))
                    {
                        // Gib eine Erfolgsmeldung in der Unity Debug-Konsole aus.


                        ModelImporter modelImporter = assetImporter as ModelImporter;
                        modelImporter.useFileUnits = false;
                        modelImporter.useFileScale = false;

                        modelImporter.animationType = ModelImporterAnimationType.Human;
                        modelImporter.autoGenerateAvatarMappingIfUnspecified = true;


                        // Call the convert method

                        if (assetType == "_race")
                        {
                            EditorApplication.delayCall = delegate ()
                            {
                                Convert(assetPath, convertType.race);
                            };
                        }
                        else
                        {
                            EditorApplication.delayCall = delegate ()
                            {
                                Convert(assetPath, convertType.cloth);
                            };
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Warning: No JSON file found for {Path.GetFileName(assetPath)} with type {assetType}");
                    }
                }
            }
        }

        /// <summary>
        /// This method fixes the normal textures of the imported model automatically.
        /// This is done by checking recrusively at the assetPath for textures and fix every *_normal. texture
        /// </summary>
        private static void FixNormalTextures(string assetPath)
        {
            string[] textureGUIDs = AssetDatabase.FindAssets("t:Texture", new string[] { assetPath.Replace(".fbx", "") });
            foreach (string textureGUID in textureGUIDs)
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(textureGUID);
                if (texturePath.ToLower().Contains("_normal") || texturePath.ToLower().Contains("_norm") || texturePath.ToLower().Contains("_bump"))
                {
                    TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    textureImporter.textureType = TextureImporterType.NormalMap;
                    textureImporter.SaveAndReimport();
                }
            }
        }


        // Diese Methode wird aufgerufen, nachdem ein Modell importiert wurde.
        private void OnPostprocessModel(GameObject g)
        {

        }
        public enum convertType
        {
            race, cloth
        }

        /// <summary>
        /// Call it with EditorApplication.delayCall = delegate () { Convert(assetPath); };
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        public static void Convert(string path, convertType type)
        {
            FixNormalTextures(path);

            switch (type)
            {
                case convertType.race:
                    UMAConverter.UMAConverter<UMAConverter.UMAData_Race> raceConverter = new UMAConverter.UMAConverter<UMAConverter.UMAData_Race>(path);
                    raceConverter.convert();
                    break;
                case convertType.cloth:
                    UMAConverter.UMAConverter<UMAConverter.UMAData_Cloth> clothConverter = new UMAConverter.UMAConverter<UMAConverter.UMAData_Cloth>(path);
                    clothConverter.convert();
                    break;
            }
        }
    }

}
