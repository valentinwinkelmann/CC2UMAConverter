using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using UMA;
using UMA.Editors;
using Unity.VisualScripting;
using System.Linq;

namespace UMAConverter
{
    /// <summary>
    /// UMAConverter takes a single .fbx file and looks for a UMAData_<T>.json file in the same directory.
    /// It will then convert the .fbx into a set of UMA compatible assets.
    /// </summary>
    /// <typeparam name="T">The Type of UMAConverting Data, Either UMAData_Race or UMAData_Cloth</typeparam>
    public class UMAConverter<T> where T : IUMAData
    {
        string meshPath; // The path to the mesh file, like a .fbx
        T data;
        string workingDirectory = null; // The directory in which we create our folder structure, is null as long no folder structure was created. Add / and the desired subfolder to get the full path.

        private List<UMAData_RaceSlots> raceSlots = new List<UMAData_RaceSlots>(); // We keep track of all Slots + Overlays

        GameObject model = null; // The imported model, we are taking our meshes from.

        private bool addToGlobalLibrary = true; // If true, the created assets will be added to the global library.

        private UMAMaterial defaultMaterial = AssetDatabase.LoadAssetAtPath<UMAMaterial>("Packages/com.vwgamedev.umaconverter/Runtime/UMAMaterials/CCMaterial.asset");

        /// <summary>
        /// Initializes a new instance of the <see cref="T:UMAConverter"/> class.
        /// A Mesh which has to be converted into UMA compatible assets needs to have a UMAData_<T>.json file in the same directory.
        /// It also have to pass the UMAConverter_PostProcessor first to be prepared for the conversion.
        /// </summary>
        /// <param name="MeshPath">The Path to our Mesh File, like a .fbx.</param>
        /// <exception cref="System.Exception"></exception>
        public UMAConverter(string MeshPath)
        {
            if (!System.IO.File.Exists(MeshPath)) throw new System.Exception("Mesh file not found at " + MeshPath);
            this.meshPath = MeshPath;
            string jsonPathSuffix = typeof(T).Name.ToLower().Split('_')[1];
            string jsonPath = MeshPath.Replace(".fbx", "_" + jsonPathSuffix + ".json");
            this.data = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(jsonPath));
            if (this.data == null) throw new System.Exception("UMAData not found for " + jsonPath);

            // Load the model
            this.model = AssetDatabase.LoadAssetAtPath<GameObject>(MeshPath);
            if(this.model == null) throw new System.Exception("Model not found at " + MeshPath);

        }


        /// <summary>
        /// Creates the folder structure for the UMA assets:
        /// - [name].fbx
        /// - [name]_[typeSuffix].json
        /// - [name]
        /// -- Overlays
        /// -- Slots
        /// -- Textures
        /// -- TPose (if IUMAData.type == race)
        /// -- Race (if IUMAData.type == race)
        /// -- Wardrobe (if IUMAData.type = cloth)
        /// </summary>
        private void CreateFolderStructure()
        {
            Debug.Log("Creating folder structure for " + meshPath);
            // Create the folder structure for the UMA assets
            string folderPath = Path.GetDirectoryName(meshPath) + "/" + Path.GetFileNameWithoutExtension(meshPath);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            Debug.Log("Creating folder structure at " + folderPath);
            string[] subFolders = new string[] { "Overlays", "Slots", "Textures" };
            foreach (string subFolder in subFolders)
            {
                if (!Directory.Exists(folderPath + "/" + subFolder)) Directory.CreateDirectory(folderPath + "/" + subFolder);
                Debug.Log("Creating subfolder " + subFolder);
            }
            if (this.data.type == UMADataType.cloth)
            {
                if (!Directory.Exists(folderPath + "/Wardrobe")) Directory.CreateDirectory(folderPath + "/Wardrobe");
            }
            else
            {
                if (!Directory.Exists(folderPath + "/TPose")) Directory.CreateDirectory(folderPath + "/TPose");
                if (!Directory.Exists(folderPath + "/Race")) Directory.CreateDirectory(folderPath + "/Race");
            }
            workingDirectory = folderPath;

            foreach (UMAData_Slot slot in data.slots)
            {
                // Each slot has a Folder in the Slots directory
                string slotPath = workingDirectory + "/Slots/" + slot.name;
                Debug.Log("Try to create slot folder at " + slotPath);
                if (!Directory.Exists(slotPath)) Directory.CreateDirectory(slotPath);
            }


            AssetDatabase.Refresh();

        }

        /// <summary>
        /// This method generates the SlotDataAsset for the given slot.
        /// It will automatically generate the OverlayDataAsset and keep track 
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public SlotDataAsset GenerateSlotAsset(UMAData_Slot slot)
        {

            // ----------------- Slot -----------------
            string slotFolder = workingDirectory + "/Slots"; // General Slot folder, not the folder for the specific slot
            string assetFolder = ""; // Acording to reverse engineering it is empty
            string assetName = slot.name; // The Name of the Slot
            string slotName = slot.name; // The Name of the Slot
            bool nameByMaterial = false; // We are not using the material name as the asset name
            SkinnedMeshRenderer slotMesh = model.transform.Find(slot.mesh).GetComponent<SkinnedMeshRenderer>(); // The skinned mesh renderer for the slot
            UMAMaterial material = defaultMaterial; // TODO: Implement a way to decide which UMAMaterial should be used as default
            SkinnedMeshRenderer seamsMesh = null; //TODO: Implement a way that our Blender Plugin exports a seams mesh and tag it in the json, if a slot has seams
            List<string> keepBoneNames = new List<string>(); // The bones which should be kept, we are not using this feature
            string rootBone = "Global"; // Its by default "Global" and there is currently no need to change it
            bool binarySerialization = false; // We are not using binary serialization
            bool calcTangents = true; // We are calculating tangents by default
            string stripBones = ""; // We are not stripping bones



            SlotDataAsset slotAsset = UMASlotProcessingUtil.CreateSlotData(slotFolder,assetFolder,assetName,slotName,nameByMaterial,slotMesh,material,seamsMesh,keepBoneNames,rootBone,binarySerialization,calcTangents,stripBones);

            slotAsset.tags = new string[0]; // Currently we are not using tags
            UMAUpdateProcessor.UpdateSlot(slotAsset);

            if (addToGlobalLibrary)
            {
                UMAAssetIndexer.Instance.EvilAddAsset(typeof(SlotDataAsset), slotAsset);
            }

            // ----------------- Overlay -----------------

            OverlayDataAsset overlayAsset = null;

            if (!string.IsNullOrEmpty(slot.overlay))
            {
                // We have to create an overlay but first we have to check if the overlay is a shared one
                if (slot.isSharedOverlay(this.data))
                { // It is a shared one, check if there is one in general slot directory
                    string overlayPath = workingDirectory + "/Overlays/" + slot.overlay;
                    overlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(overlayPath + ".asset");

                    if (overlayAsset == null)
                    {
                        overlayAsset = CreateOverlay(overlayPath, slotAsset, slotName, slot.overlay);
                    }
                } else
                { // Its not Shared, so it belongs directly to the slot directory. Create a Overlay there.
                    string overlayPath = workingDirectory + "/Slots/" + slot.name + "/" + slot.overlay;
                    overlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(overlayPath + ".asset"); // Check if the overlay already exists
                    if (overlayAsset == null)
                    {
                        overlayAsset = CreateOverlay(overlayPath, slotAsset, slotName, slot.overlay);
                    }
                }
            } else
            {
                Debug.Log("No overlay found for slot " + slot.name);
            }

            // ----------------- Wardrobe Recipe -----------------
            // This is only needed for cloth. Every Cloth slot has a Wardrobe Recipe
            if (this.data.type == UMADataType.cloth)
            {
                string recipePath = workingDirectory + "/Wardrobe/" + slot.name + "_Recipe";
                CreateRecipe(recipePath, slotAsset, overlayAsset, addToGlobalLibrary, slot.wardrobeSlot);
            }
            if(slotAsset == null)
            {
                Debug.LogWarning("That should not happen.");
            }
            if(overlayAsset == null)
            {
                Debug.LogWarning("That should not happen.");
            }

            Debug.Log("We add Slot and overlay asset to our memory");
            raceSlots.Add(new UMAData_RaceSlots(slotAsset, overlayAsset));




            return slotAsset;

        }

        public OverlayDataAsset CreateOverlay(string overlayPath, SlotDataAsset slotAsset, string slotName, string overlayName = null)
        {
            OverlayDataAsset asset = ScriptableObject.CreateInstance<OverlayDataAsset>();
            asset.overlayName = slotName; // + "_Overlay";
            if(overlayName != null)
            {
                asset.overlayName = overlayName;
            }
            asset.material = slotAsset.material;
            Texture[] textures = GetOverlayTextureList(overlayName, slotAsset.material);
            foreach (Texture texture in textures)
            {
                Debug.Log("We found a dumb fat texture: " + texture.name);
            }
            asset.textureList = GetOverlayTextureList(overlayName, defaultMaterial);


            Debug.Log("Creating overlay for: " + slotName + " at " + overlayPath);
            AssetDatabase.CreateAsset(asset, overlayPath +".asset");
            AssetDatabase.SaveAssets();
            return asset;

        }

        /// <summary>
        /// This method takes a overlayName and will return a list of textures which are found in the overlay folder.
        /// Texture names are given by [OverlayName]_[Channel].ext
        /// The Channel is given by the umaMaterial.channels[0].materialPropertyName and will be added as suffix to the overlayName.
        /// Resulting in a texture like Body_BaseMap.* or Body_NormalMap.*.
        /// We will return the list in the order of the umaMaterial.channels
        /// </summary>
        /// <param name="overlayName"></param>
        /// <returns></returns>
        public Texture[] GetOverlayTextureList(string overlayName, UMAMaterial umaMaterial)
        {
            List<Texture> textures = new List<Texture>();
            foreach (UMAMaterial.MaterialChannel channel in umaMaterial.channels)
            {
                string textureName = overlayName + "_" + channel.materialPropertyName.Replace("_", "");
                Debug.Log("Trying to find texture: " + textureName);
                string[] textureGUIDs = AssetDatabase.FindAssets(textureName);
                if (textureGUIDs.Length > 0)
                {
                    string texturePath = AssetDatabase.GUIDToAssetPath(textureGUIDs[0]);
                    Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                    textures.Add(texture);
                }
            }
            return textures.ToArray();  
        }



        private void CreateRecipe(string path, SlotDataAsset slotData, OverlayDataAsset overlayData, bool addToGlobalLibrary, string wardrobeSlot)
        {
            path = path + ".asset";
            UMA.CharacterSystem.UMAWardrobeRecipe wardrobeRecipe = UMAEditorUtilities.CreateRecipe(path, slotData, overlayData, slotData.name, addToGlobalLibrary);
            wardrobeRecipe.wardrobeSlot = wardrobeSlot;
            wardrobeRecipe.compatibleRaces = (this.data as UMAData_Cloth).compatibleRaces;
            Debug.Log("have tried to set Compatible Races for: " + slotData.name + " to " + (this.data as UMAData_Cloth).compatibleRaces.First());
            Debug.Log("Recipe created for: " + slotData.name);
        }


        /// <summary>
        /// This Method takes all Slots and the UMAData_Race information and generates a UMA Compatible and Ready to use Race.
        /// It have to be called after all slots are generated or it will fail.
        /// </summary>
        /// <returns>State if the generation of a Race was a Succsess or Failed</returns>
        public bool GenerateRaceAssets()
        {
            if (this.data.type != UMADataType.race) throw new System.Exception("This method can only be called for Race Creation");

            // ---------------- Race Data ----------------

            RaceData raceData = ScriptableObject.CreateInstance<RaceData>();
            raceData.raceName = (this.data as UMAData_Race).name;
            raceData.TPose = GenerateTpose();
            raceData.FixupRotations = true;

            string raceDataPath = workingDirectory + "/Race/" + (this.data as UMAData_Race).name + "_RaceData.asset";

            AssetDatabase.CreateAsset(raceData, raceDataPath);

            if (addToGlobalLibrary)
            {
                UMAAssetIndexer.Instance.EvilAddAsset(typeof(RaceData), raceData);
            }

            // ----------------- Race Text Recipe -----------------

            UMATextRecipe asset = ScriptableObject.CreateInstance<UMATextRecipe>();
            UMAData.UMARecipe recipe = new UMAData.UMARecipe();
            recipe.ClearDna();



            int index = 0;
            foreach(UMAData_RaceSlots raceSlot in raceSlots)
            {
                Debug.Log("We found a Slot: " + raceSlot.slot.name + " with an Overlay: " + raceSlot.overlay.overlayName + ". Lets add them!");
                SlotData slotData = new SlotData(raceSlot.slot);
                OverlayData overlayData = new OverlayData(raceSlot.overlay);
                slotData.AddOverlay(overlayData);
                recipe.SetSlot(index, slotData);
                index++;
            }
            recipe.SetRace(raceData);
            asset.Save(recipe, UMAContextBase.Instance);
            asset.DisplayValue = (this.data as UMAData_Race).name + "_TextRecipe";

            string textRecipePath = workingDirectory + "/Race/" + (this.data as UMAData_Race).name + "_TextRecipe.asset";

            // Write the asset to disk
            AssetDatabase.CreateAsset(asset, textRecipePath);
            AssetDatabase.SaveAssets();
            if (addToGlobalLibrary)
            {
                // Add it to the global libary
                UMAAssetIndexer.Instance.EvilAddAsset(typeof(UMA.CharacterSystem.UMAWardrobeRecipe), asset);
                EditorUtility.SetDirty(UMAAssetIndexer.Instance);

            }


            // Was Trying to combine booth at the end without success
            //recipe.SetRace(raceData);
            raceData.baseRaceRecipe = asset;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            EditorUtility.SetDirty(raceData);
            AssetDatabase.SaveAssetIfDirty(raceData);

            AssetDatabase.Refresh();
            return true;

        }

        /// <summary>
        /// Generates a TPose for the Race and saves it to its folder.
        /// It should be called only for Race Creation.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public UmaTPose GenerateTpose()
        {
            if (this.data.type != UMADataType.race) throw new System.Exception("This method can only be called for Race Creation");
            string TPosePath = workingDirectory + "/TPose/" + (this.data as UMAData_Race).name + "_TPose.asset";

            ModelImporter modelImporter = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            if(modelImporter != null)
            {
                var asset = UmaTPose.CreateInstance<UMA.UmaTPose>();
                asset.ReadFromHumanDescription(modelImporter.humanDescription);
                AssetDatabase.CreateAsset(asset, TPosePath);
                return asset;
            } else
            {
                throw new System.Exception("Failed to load ModelImporter for " + meshPath);
            }

        }

        public void convert()
        {
            if (data == null) throw new System.Exception("UMAData not found for " + meshPath);
            CreateFolderStructure();
            foreach (UMAData_Slot slot in data.slots)
            {
                Debug.Log("Generating Slot: " + slot.name);
                GenerateSlotAsset(slot);
            }
            if(data.type == UMADataType.race)
            {
                Debug.Log("We have a Race, so we generate all needed assets for it.");
                GenerateRaceAssets();
            }
            
        }






    }


    [System.Serializable]
    public enum UMADataType
    {
        cloth = 0,
        race = 1
    }


    public interface IUMAData
    {
        UMADataType type { get; set; }
        List<string> meshes { get; set; }
        List<string> overlays { get; set; }
        List<UMAData_Slot> slots { get; set; }
    }


    [System.Serializable]
    public class UMAData_Race : IUMAData
    {
        public UMADataType type { get; set; }
        public string name { get; set; }
        public float hipHeight { get; set; }
        public List<string> meshes { get; set; }
        public List<string> overlays { get; set; }
        public List<UMAData_Slot> slots { get; set; }
    }
    [System.Serializable]
    public class UMAData_Cloth : IUMAData
    {
        public UMADataType type { get; set; }
        public List<string> compatibleRaces { get; set; }
        public List<string> meshes { get; set; }
        public List<string> overlays { get; set; }
        public List<UMAData_Slot> slots { get; set; }
    }
    [System.Serializable]
    public class UMAData_Slot
    {
        public string name;
        public string mesh;
        public string overlay;
        public string wardrobeSlot = "";


        /// <summary>
        /// Checks if the slot is sharing its overlay with another slot.
        /// </summary>
        /// <param name="data">The data to itterate over the others...</param>
        /// <returns></returns>
        public bool isSharedOverlay(IUMAData data)
        {
            foreach (UMAData_Slot slot in data.slots)
            {
                if (slot.name != this.name && slot.overlay == this.overlay) return true;
            }
            return false;
        }
    }



    public class UMAData_RaceSlots
    {
        public SlotDataAsset slot;
        public OverlayDataAsset overlay;

        public UMAData_RaceSlots(SlotDataAsset slot, OverlayDataAsset overlay)
        {
            this.slot = slot;
            this.overlay = overlay;
        }
    }
}