bl_info = {
    "name": "UMA Converter",
    "blender": (2, 93, 0),
    "category": "Object",
    "version": (0, 1, 0),
    "author": "Valentin Winkelmann",
    "description": "will help you to convert a Character Creator 4 Character to a fully functional and ready to use UMA Asset. The Plugin will not only allow you to convert a naked Character as a new UMA Race, it will also allow you to convert clothed Characters to UMA Wardrobe Slots. The Plugin will take care of the necessary steps and keeps everything as simple as possible. The main goal is to let you focus on preparing your Character and Clothing in Character Creator 4 and build a easy to use Brdige to UMA. While you can of course modify the resulting UMA Assets directly in Unity, the Plugin is designed to let you handle everything in Blender."
}

import bpy
import importlib
import os
import shutil
from bpy_extras.io_utils import ImportHelper, ExportHelper
from bpy.types import Operator, Panel
from bpy.props import StringProperty, EnumProperty, BoolProperty

#from . import dataHandling # Contains the functions to handle the data
#from . import umaconverter # Containts the functions to convert the rig and meshes
#from . import gui # Contains the functions to handle the GUI





# only while developing
if "dataHandling" in locals():
    importlib.reload(dataHandling)
else:
    from . import dataHandling

if "umaconverter" in locals():
    importlib.reload(umaconverter)
else:
    from . import umaconverter

if "gui" in locals():
    importlib.reload(gui)
else:
    from . import gui






def init_blender_viewport():
    """This changes some Blender viewport settings to make the Plugin shows the imported meshes better"""
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            # Zugriff auf den 3D-Viewport
            space = area.spaces.active
            # Überprüft, ob der Shading-Typ geändert werden kann
            if hasattr(space, 'shading'):
                # Setzt den Shading-Typ auf 'MATERIAL'
                space.shading.type = 'MATERIAL'
                break  # Beendet die Schleife nach der Änderung





class UMA_PT_Panel(Panel):
    bl_label = "UMA Converter"
    bl_idname = "UMA_PT_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'UMA Converter'

    def draw(self, context):
        layout = self.layout
        rig_status = umaconverter.check_rig()

        layout.label(text="UMA Converter v0.2.0")

        if rig_status["is_cc3_4_rig"]:
            layout.label(text="CC3/4 Rig found", icon="INFO")

            layout.prop(context.scene, "rig_type", text="Rig Type")

            if context.scene.rig_type == 'race':
                box = layout.box()
                box.label(text="UMA Race Export", icon="INFO")


            if context.scene.rig_type == 'clothing':
                box_clothing = layout.box()
                box_clothing.label(text="Clothing Options")
                box_clothing.prop(context.scene, "json_file_path", text="JSON File Path")





            layout.operator("uma.convert", text="Convert")
        elif rig_status["is_uma_rig"]:
            if(context.scene.rig_type == 'race'):
                layout.prop(context.scene, "race_name")
            
            raceData = None
            if(context.scene.rig_type == 'clothing'):
                raceData = dataHandling.load_from_scene_properties('race_data', dataHandling.UMAData_Race)
                box = layout.box()
                box.label(text="UMA Race Info", icon="INFO")
                box.label(text="Clothing for: " + raceData.name)
            

            layout.separator()

            layout.label(text="Available Overlays:")
            # Display Overlay Items
            overlays = umaconverter.meshes_to_overlay([item.name for item in context.scene.mesh_items if item.selected])
            for overlay in overlays:
                row = layout.row()
                row.label(text=overlay, icon="MATERIAL")
            #Warn if materials have same name

            # Display Mesh Items
            layout.label(text="Available Meshes:")


 
            for item in context.scene.mesh_items:
                if(raceData != None and item.name in raceData.meshes):
                    continue
                box = layout.box()
                row = box.row()
                row.prop(item, "selected", text=item.name)
                if(item.selected):
                    row.prop(item, "slot_name", text="Slot Name")
                if(context.scene.rig_type == 'clothing' and item.selected):
                    box.prop(item, "wardrobe_slot")

            layout.operator("uma.export", text="Export")
        else:
            layout.label(text="Please import compatible CC3/4 Mesh", icon="INFO")
            layout.operator("uma.import", text="Import")

class UMA_OT_Convert(Operator):
    bl_idname = "uma.convert"
    bl_label = "Convert"

    def execute(self, context):

        init_blender_viewport()

        umaconverter.apply_transforms_rest_pose()
        if(context.scene.rig_type == 'race'):
            hip_height = umaconverter.get_cc_base_hip_height_global()
            print("We had a Race, so we stored the hip height: ", hip_height)

            # We will need a function to catch all the mesh data and make it into a UMAData_race
            hip_height = umaconverter.get_cc_base_hip_height_global()
            raceData = dataHandling.UMAData_Race("NewRace", hip_height, [], [], []) # we will populate some data later
            
            # We will store the Race Data in our scene data
            dataHandling.save_to_scene_properties(raceData, "race_data")
            
        if(context.scene.rig_type == 'clothing'):
            print("try to load config from " + context.scene.json_file_path)
            raceData =  dataHandling.load_from_json_file(context.scene.json_file_path, dataHandling.UMAData_Race)
            print("Loaded... store config in scene")
            dataHandling.save_to_scene_properties(raceData, "race_data")

            current_hip_height = umaconverter.get_cc_base_hip_height_global()
            print("Current Hip Height:", current_hip_height)
            print("Race Hip Height:", raceData.hipHeight)
            difference = current_hip_height - raceData.hipHeight
            print("Difference:", difference)
            print("We can adjust height now")

            umaconverter.adjust_cc_base_hip_height(difference)


        umaconverter.add_uma_bones()
        self.report({'INFO'}, "Conversion process completed.")
        return {'FINISHED'}

class UMA_OT_Import(Operator, ImportHelper):
    bl_idname = "uma.import"
    bl_label = "Import FBX"
    filename_ext = ".fbx"
    filter_glob: StringProperty(default="*.fbx", options={'HIDDEN'})

    def execute(self, context):
        import_options = {
            'use_anim': False,
            'ignore_leaf_bones': True,
            'automatic_bone_orientation': True,
        }
        bpy.ops.import_scene.fbx(filepath=self.filepath, **import_options)
        # Update mesh items list after import
        bpy.context.scene.mesh_items.clear()
        for obj in bpy.data.objects:
            if obj.type == 'MESH':
                item = bpy.context.scene.mesh_items.add()
                item.name = obj.name
        self.report({'INFO'}, "FBX Imported successfully.")

        # After we imported the fbx, we check if we find a json file alongside the fbx. It will have the same name as the fbx with .json extension
        #json_file_path = self.filename_ext.replace(".fbx", ".json")

        # The folder where the fbx is located without filename and extension
        fileDir = os.path.dirname(self.filepath)
        print("FileDir: ", fileDir)
        umaconverter.setup_pbr_materials(fileDir)


        return {'FINISHED'}

class UMA_OT_Export(Operator, ExportHelper):
    bl_idname = "uma.export"
    bl_label = "Export Selected"
    filename_ext = ".fbx"

    export_textures: BoolProperty(
        name="Export Textures",
        description="Export the textures of the selected objects",
        default=True
    )

    rendering_pipeline: EnumProperty(
        name="Unity Rendering Pipeline",
        description="Wählen Sie den Typ des Exports",
        items=[
            ('default', "Default", ""),
            ('urp', "URP", ""),
            ('hdrp', "HDRP", "")
        ],
        default='urp',
    )

    # Definition eines StringProperty für zusätzliche Eingaben
    # custom_data: StringProperty(
    #     name="Custom Data",
    #     description="Geben Sie zusätzliche Daten ein",
    #     default=""
    # )

    def execute(self, context):


        export_type = self.export_type
        custom_data = self.custom_data


        # Deselect all
        bpy.ops.object.select_all(action='DESELECT')
        
        # Select meshes based on checkboxes
        for item in context.scene.mesh_items:
            if item.selected:
                mesh_obj = bpy.data.objects.get(item.name)
                if mesh_obj:
                    mesh_obj.select_set(True)
        
        # Ensure armature is also selected
        for obj in bpy.data.objects:
            if obj.type == 'ARMATURE':
                obj.select_set(True)
                context.view_layer.objects.active = obj  # Set armature as active for export
        
        # Export selected objects as FBX
        bpy.ops.export_scene.fbx(filepath=self.filepath, use_selection=True, global_scale=0.01, object_types={'MESH', 'ARMATURE'})


        if(context.scene.rig_type == "race"): # Export _race.json
            raceData = dataHandling.load_from_scene_properties('race_data', dataHandling.UMAData_Race)
            raceData.name = context.scene.race_name
            raceData.meshes = [item.name for item in context.scene.mesh_items if item.selected]
            raceData.overlays = umaconverter.meshes_to_overlay(raceData.meshes)
            raceData.slots = []
            for item in context.scene.mesh_items:
                if item.selected:
                    slot = dataHandling.UMAData_Slot(item.slot_name, item.name, umaconverter.mesh_to_overlay(item.name) )
                    raceData.slots.append(slot)



            filename = self.filepath.replace(".fbx", "")
            dataHandling.save_to_json_file(raceData, filename + "_race.json")

        if(context.scene.rig_type == "clothing"): # Export _clothing.json
            raceData = dataHandling.load_from_scene_properties('race_data', dataHandling.UMAData_Race)
            clothData = dataHandling.UMAData_Cloth([raceData.name], [], [], [])
            clothData.meshes = [item.name for item in context.scene.mesh_items if item.selected]
            clothData.overlays = umaconverter.meshes_to_overlay(clothData.meshes)
            clothData.slots = []
            for item in context.scene.mesh_items:
                if item.selected: 
                    print("Finding overlay for ", item.name)
                    print("Overlay: ", umaconverter.mesh_to_overlay(item.name))
                    slot = dataHandling.UMAData_Slot(item.slot_name, item.name, umaconverter.mesh_to_overlay(item.name))
                    clothData.slots.append(slot)

            filename = self.filepath.replace(".fbx", "")
            dataHandling.save_to_json_file(clothData, filename + "_cloth.json")

        if self.export_textures:
            selected_objects = [bpy.data.objects[item.name] for item in context.scene.mesh_items if item.selected]
            # Ruft die Funktion zum Speichern der Texturen auf
            save_textures_with_export(self.filepath, selected_objects, filename)

        self.report({'INFO'}, "Export successful.")
        return {'FINISHED'}
    
    def draw(self, context):
        layout = self.layout
        layout.prop(self, 'export_textures')
        # if self.export_textures: TODO: Implement Texture packing for Rendering Pipelines
        #     layout.prop(self, 'rendering_pipeline')
        # layout.prop(self, 'custom_data')


def save_textures_with_export(filepath, selected_objects, custom_folder_name="Exported_Textures"):
    # Basisverzeichnis für die exportierten Dateien
    base_directory = os.path.dirname(filepath)
    # Erstellt den benutzerdefinierten Ordner und den Textures-Unterordner
    custom_directory = os.path.join(base_directory, custom_folder_name, "Textures")
    os.makedirs(custom_directory, exist_ok=True)
    
    for obj in selected_objects:
        if obj.type == 'MESH' and obj.material_slots:
            for slot in obj.material_slots:
                if slot.material and slot.material.use_nodes:
                    for node in slot.material.node_tree.nodes:
                        if node.type == 'TEX_IMAGE' and node.image:
                            original_texture_path = bpy.path.abspath(node.image.filepath)
                            if os.path.isfile(original_texture_path):
                                # Extrahiert das Suffix aus dem Originaltexturnamen
                                texture_suffix = os.path.basename(original_texture_path).split("_")[-1]
                                # Bildet den neuen Namen: [MATERIAL_NAME]_[SUFFIX].[EXT]
                                new_texture_name = f"{slot.material.name}_{texture_suffix}"
                                # Zielort für die Textur
                                target_path = os.path.join(custom_directory, new_texture_name)
                                # Kopiert die Textur unter neuem Namen
                                shutil.copy(original_texture_path, target_path)
                                print(f"Texture copied and renamed to {target_path}")

def register():
    gui.register_rig_type_selector()
    gui.register_json_file_field()
    gui.register_race_wizard()

    gui.register_mesh_items()
    bpy.utils.register_class(UMA_PT_Panel)
    bpy.utils.register_class(UMA_OT_Convert)
    bpy.utils.register_class(UMA_OT_Import)
    bpy.utils.register_class(UMA_OT_Export)

def unregister():
    gui.unregister_rig_type_selector()
    gui.unregister_json_file_field()
    gui.unregister_race_wizard()

    gui.unregister_mesh_items()
    bpy.utils.unregister_class(UMA_PT_Panel)
    bpy.utils.unregister_class(UMA_OT_Convert)
    bpy.utils.unregister_class(UMA_OT_Import)
    bpy.utils.unregister_class(UMA_OT_Export)

if __name__ == "__main__":
    register()
