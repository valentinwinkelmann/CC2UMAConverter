using UMA.CharacterSystem;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using UMA;
using System.Collections.Generic;
using System;
using GameCreator.Runtime.Characters;
using System.Linq;

#if UMAConverterGCInventory
using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using vwgamedev.GameCreator.Runtime.UMA;
#endif


namespace UMAConverter.integrations
{
    /// <summary>
    /// This class will add some functionality to integrate the UMA Converter with GameCreator and its Inventory System
    /// </summary>
    public static class GameCreatorInventory
    {

#if UMAConverterGCInventory

        /// <summary>
        /// We will generate a new GameCreator2 item from the UMA Wardrobe Recipe
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="path">Location to save the Item</param>
        public static void CreateItem(UMAWardrobeRecipe recipe, string path, string Name)
        {


            Equipment equipment =  null; // TODO: We should think of adding a Race Data structure to store all information as we start to do more complex things
            Item Parent = null;
            try
            {
                string equipmentPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(recipe.compatibleRaces[0] + "_Equipment")[0]);
                equipment = AssetDatabase.LoadAssetAtPath<Equipment>(equipmentPath);
                Parent = equipment.Slots.First((slot) => slot.Base.name == recipe.wardrobeSlot).Base;
            }
            catch (Exception e)
            {
                Debug.Log("We could not find the Equipment Asset: " + e.Message);
            }
            


            Item item = ScriptableObject.CreateInstance<Item>();
            FieldInfo parentField = item.GetType().GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (parentField != null)
            {
                parentField.SetValue(item, Parent);
            }
            FieldInfo infoField = item.GetType().GetField("m_Info", BindingFlags.NonPublic | BindingFlags.Instance);
            if (infoField != null)
            {
                var info = infoField.GetValue(item);
                

                FieldInfo nameField = info.GetType().GetField("m_Name", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    PropertyGetString propertyNameString = new PropertyGetString(Name);
                    nameField.SetValue(info, propertyNameString);
                }
                FieldInfo descriptionField = info.GetType().GetField("m_Description", BindingFlags.NonPublic | BindingFlags.Instance);
                if (descriptionField != null)
                {
                    PropertyGetString propertyDescriptionString = new PropertyGetString("Wardrobe Item for " + Name);
                    descriptionField.SetValue(info, propertyDescriptionString);
                }
                FieldInfo spriteField = info.GetType().GetField("m_Sprite", BindingFlags.NonPublic | BindingFlags.Instance);
                if (spriteField != null && recipe.wardrobeRecipeThumbs.Count != 0)
                {
                    Sprite sprite = recipe.wardrobeRecipeThumbs[0].thumb;
                    PropertyGetSprite propertySprite = new PropertyGetSprite(sprite);
                    spriteField.SetValue(info, propertySprite);
                }

                infoField.SetValue(item, info);
            }
            else
            {
                Debug.LogError("Field 'm_Info' not found in 'Item'");
            }

            FieldInfo equipField = item.GetType().GetField("m_Equip", BindingFlags.NonPublic | BindingFlags.Instance);
            if (equipField != null)
            {
                var equip = equipField.GetValue(item);
                FieldInfo IsEquippableField = equip.GetType().GetField("m_IsEquippable", BindingFlags.NonPublic | BindingFlags.Instance);
                if (IsEquippableField != null)
                {
                    IsEquippableField.SetValue(equip, true);
                }
                FieldInfo InstructionsOnEquipField = equip.GetType().GetField("m_InstructionsOnEquip", BindingFlags.NonPublic | BindingFlags.Instance);
                if (InstructionsOnEquipField != null)
                {
                    RunInstructionsList InstructionOnEquip = new RunInstructionsList(new Instruction_UMA_EquipWardrobeRecipe() { UMARecipe = recipe });
                    InstructionsOnEquipField.SetValue(equip, InstructionOnEquip);
                }
                FieldInfo InstructionsOnUnequipField = equip.GetType().GetField("m_InstructionsOnUnequip", BindingFlags.NonPublic | BindingFlags.Instance);
                if (InstructionsOnUnequipField != null)
                {
                    RunInstructionsList InstructionOnUnequip = new RunInstructionsList(new Instruction_UMA_UnequipWardrobeRecipe() { UMARecipe = recipe });
                    InstructionsOnUnequipField.SetValue(equip, InstructionOnUnequip);
                }
            }
            else
            {
                Debug.LogError("Field 'm_Equip' not found in 'Item'");
            }

            AssetDatabase.CreateAsset(item, path + Name + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Item CreateParentItem(string path, string name)
        {
            Item item = ScriptableObject.CreateInstance<Item>();
            item.name = name;

            FieldInfo infoField = item.GetType().GetField("m_Info", BindingFlags.NonPublic | BindingFlags.Instance);
            if (infoField != null)
            {
                var info = infoField.GetValue(item);

                FieldInfo nameField = info.GetType().GetField("m_Name", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    PropertyGetString propertyNameString = new PropertyGetString(name);
                    nameField.SetValue(info, propertyNameString);
                }
                FieldInfo descriptionField = info.GetType().GetField("m_Description", BindingFlags.NonPublic | BindingFlags.Instance);
                if (descriptionField != null)
                {
                    PropertyGetString propertyDescriptionString = new PropertyGetString("Parent Slot for " + name);
                    descriptionField.SetValue(info, propertyDescriptionString);
                }

                infoField.SetValue(item, info);
            }
            else
            {
                Debug.LogError("Field 'm_Info' not found in 'Item'");
            }

            AssetDatabase.CreateAsset(item, path + name + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return item;
        }

        /// <summary>
        /// Every GameCreator character who wants to wear Clothes, Armor, etc. needs to have a list of Slots for it,
        /// Those slots are defined in the Eqipment Asset which is listing Items as Parent Slots. Each of those Items is a normal Item Asset
        /// But fullfilling a role of a Placeholder for the Slot. Means we Create a new Item Asset for each Wardrobe Slot our UMA Race has,
        /// then we add those Items to the Equipment Slot Asset, and finally, when we create wardrobe items, we will assign the appropriate
        /// Parrent Item to it according to its Wardrobe Slot Name.
        /// </summary>
        /// <param name="data">The RaceData we import from our Blender Plugin</param>
        /// <param name="path">Location to save the Equipment Asset</param>
        /// <returns></returns>
        public static void CreateEquipmentAsset(RaceData data, string path)
        {

            List<Item> items = new List<Item>();

            foreach(string slot in UMADefaultWardrobeSlots)
            {
                if (AssetDatabase.LoadAssetAtPath<Item>(UMAConverterSettings.Instance.ParentItemLocation + slot + ".asset") == null)
                {
                    items.Add(CreateParentItem(UMAConverterSettings.Instance.ParentItemLocation, slot));
                } else
                {
                    items.Add(AssetDatabase.LoadAssetAtPath<Item>(UMAConverterSettings.Instance.ParentItemLocation + slot + ".asset"));
                }
            }

            Equipment equipment = ScriptableObject.CreateInstance<Equipment>();

            FieldInfo equipmentSlotsField = equipment.GetType().GetField("m_Slots", BindingFlags.NonPublic | BindingFlags.Instance);
            if (equipmentSlotsField != null)
            {
                EquipmentSlots equipmentSlots = new EquipmentSlots();
                FieldInfo slotsField = equipmentSlots.GetType().GetField("m_Slots", BindingFlags.NonPublic | BindingFlags.Instance);
                if (slotsField != null)
                {
                    List<EquipmentSlot> m_Slots = new List<EquipmentSlot>();
                    foreach (Item item in items)
                    {
                        EquipmentSlot slot = new EquipmentSlot();
                        FieldInfo slotBase = slot.GetType().GetField("m_Base", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (slotBase != null)
                        {
                            slotBase.SetValue(slot, item);
                        }
                        FieldInfo handleField = slot.GetType().GetField("m_Handle", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (handleField != null)
                        {
                            HandleField handle = new HandleField();

                            FieldInfo handleBone = handle.GetType().GetField("m_Bone", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (handleBone != null)
                            {
                                handleBone.SetValue(handle, null);
                            }

                            handleField.SetValue(slot, handle);
                        }


                        m_Slots.Add(slot);
                    }

                    slotsField.SetValue(equipmentSlots, m_Slots.ToArray());
                }
                else
                {
                    Debug.LogError("Field 'm_Slots' not found in 'EquipmentSlots'");
                }
                equipmentSlotsField.SetValue(equipment, equipmentSlots);
            }
            else
            {
                Debug.LogError("Field 'm_Slots' not found in 'Equipment'");
            }


            Debug.Log("Created: " + equipment + " on " + path + "Equipment.asset");
            AssetDatabase.CreateAsset(equipment, path + "Equipment.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


        }

        /// <summary>
        /// Fallback untill we Update the Blender Exporter to allow customized Wardrobe Slots on Race Exports. Maybe next Time...
        /// </summary>
        public static string[] UMADefaultWardrobeSlots = new string[] { "Face", "Hair", "Complexion", "Eyebrows", "Beard", "Ears", "Helmet", "Shoulders", "Chest", "Arms", "Hands", "Waist", "Legs", "Feet" }; 
#endif
                }
}