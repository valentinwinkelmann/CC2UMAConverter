import bpy
import importlib
import os
import glob
if "dataHandling" in locals():
    importlib.reload(dataHandling)
else:
    from . import dataHandling


def apply_transforms_rest_pose():
    """Applies all transforms and sets the armature to rest pose. This is necessary for CC3/4 rigs, as they are imported wrong in Blender."""
    armature_obj = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
    if not armature_obj:
        return
    bpy.context.view_layer.objects.active = armature_obj[0]
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in bpy.data.objects:
        if obj.type in {'ARMATURE', 'MESH'}:
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.context.view_layer.update()
    bpy.context.area.tag_redraw()

def add_uma_bones():
    """Adds UMA specific bones to the rig."""
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            if 'root' in obj.data.bones and any(child.name == 'CC_Base_Hip' for child in obj.data.bones['root'].children):
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.mode_set(mode='EDIT')
                bones = obj.data.edit_bones
                if 'root' in bones:
                    bones['root'].name = 'Position'
                if 'Global' not in bones:
                    global_bone = bones.new('Global')
                    global_bone.head = (0, 0, 0)
                    global_bone.tail = (0, 0, 1)
                    bones['Position'].parent = global_bone
                bpy.ops.object.mode_set(mode='OBJECT')
    for mesh_obj in bpy.data.objects:
        if mesh_obj.type == 'MESH':
            mesh_obj.rotation_euler = (0.0, 0.0, 0.0)

def adjust_cc_base_hip_height(y_height):
    """
    Passt die Y-Position des CC_Base_Hip Bones an.
    :param y_height: Die neue Y-Position für den CC_Base_Hip Bone.
    """
    bpy.ops.object.mode_set(mode='OBJECT')  # Stelle sicher, dass wir im Object Mode starten

    # Armatur auswählen und aktivieren
    armature = next((obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE'), None)
    if armature:
        bpy.context.view_layer.objects.active = armature
        armature.select_set(True)

        # In den Edit Mode wechseln und den CC_Base_Hip Bone auswählen
        bpy.ops.object.mode_set(mode='EDIT')
        bone = armature.data.edit_bones.get('CC_Base_Hip')
        if bone:
            bone.select = True
            bone.select_head = True
            bone.select_tail = True

            # Bone vom Elternteil trennen
            bpy.ops.armature.parent_clear(type='DISCONNECT')

            # In den Pose Mode wechseln
            bpy.ops.object.mode_set(mode='POSE')
            pose_bone = armature.pose.bones.get('CC_Base_Hip')
            if pose_bone:
                pose_bone.location[1] = y_height  # Y-Position anpassen

            # Zurück in den Object Mode
            bpy.ops.object.mode_set(mode='OBJECT')
        else:
            print("CC_Base_Hip Bone wurde nicht gefunden.")
    else:
        print("Keine Armatur gefunden.")

def get_cc_base_hip_height():
    """
    Gibt die aktuelle Y-Position des CC_Base_Hip Bones zurück.
    :return: Die Y-Position des CC_Base_Hip Bones, oder None, falls der Bone nicht gefunden wurde.
    """
    bpy.ops.object.mode_set(mode='OBJECT')  # Stelle sicher, dass wir im Object Mode starten

    # Armatur auswählen und aktivieren
    armature = next((obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE'), None)
    if armature:
        bpy.context.view_layer.objects.active = armature
        armature.select_set(True)

        # In den Pose Mode wechseln
        bpy.ops.object.mode_set(mode='POSE')
        pose_bone = armature.pose.bones.get('CC_Base_Hip')
        if pose_bone:
            y_position = pose_bone.location[1]  # Y-Position auslesen

            # Zurück in den Object Mode
            bpy.ops.object.mode_set(mode='OBJECT')
            return y_position
        else:
            print("CC_Base_Hip Bone wurde nicht gefunden.")
            return None
    else:
        print("Keine Armatur gefunden.")
        return None

def get_cc_base_hip_height_global():
    """
    Gibt die Weltkoordinaten des CC_Base_Hip Bones zurück.
    :return: Die Weltkoordinaten des CC_Base_Hip Bones als Vector, oder None, falls der Bone oder die Armatur nicht gefunden wurde.
    """
    bpy.ops.object.mode_set(mode='OBJECT')  # Stelle sicher, dass wir im Object Mode starten

    # Armatur auswählen und aktivieren
    armature = next((obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE'), None)
    if armature:
        bpy.context.view_layer.objects.active = armature
        armature.select_set(True)

        # In den Pose Mode wechseln, um die Bone-Position zu erhalten
        bpy.ops.object.mode_set(mode='POSE')
        pose_bone = armature.pose.bones.get('CC_Base_Hip')
        if pose_bone:
            # Umwandlung der lokalen Bone-Position in Weltkoordinaten
            world_position = armature.matrix_world @ pose_bone.head

            # Zurück in den Object Mode
            bpy.ops.object.mode_set(mode='OBJECT')
            return world_position.z
        else:
            print("CC_Base_Hip Bone wurde nicht gefunden.")
            return None
    else:
        print("Keine Armatur gefunden.")
        return None





def check_rig():
    """Checks if the current scene contains a CC3/4 or UMA rig. Returns a dictionary with the results."""
    rig_status = {"is_cc3_4_rig": False, "is_uma_rig": False}
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            bones = obj.data.bones
            if 'root' in bones and any(child.name == 'CC_Base_Hip' for child in bones['root'].children):
                rig_status["is_cc3_4_rig"] = True
                return rig_status
            if 'Global' in bones and any(child.name == 'Position' for child in bones['Global'].children):
                rig_status["is_uma_rig"] = True
                return rig_status
    return rig_status



def meshes_to_overlay(mesh_names):
    """Takes a list of mesh names and returns a list of unique materials used by these meshes. Those are our overlays, UMA needs to generate them."""
    unique_materials = set()  # Ein Set, um Duplikate zu vermeiden
    for mesh_name in mesh_names:
        mesh = bpy.data.objects.get(mesh_name)
        if mesh and mesh.type == 'MESH':
            for mat_slot in mesh.material_slots:
                if mat_slot.material:  # Überprüfe, ob das Material existiert
                    unique_materials.add(mat_slot.material.name)
    return list(unique_materials)  # Konvertiere das Set zurück in eine Liste"""

def mesh_to_overlay(mesh_name):
    """Takes a mesh name and returns a single material name as string used by this mesh. Those are our overlay, UMA needs to generate them."""
    mesh = bpy.data.objects.get(mesh_name)
    if mesh and mesh.type == 'MESH':
        for mat_slot in mesh.material_slots:
            if mat_slot.material:  # Überprüfe, ob das Material existiert
                return mat_slot.material.name
    return ""  # Konvertiere das Set zurück in eine Liste"""



def find_textures_custom_path(base_path, search_pattern):
    pattern = os.path.join(base_path, "**", search_pattern + "*.*")
    files = glob.glob(pattern, recursive=True)
    return files

def add_texture_to_material(material, texture_path, texture_type):
    # Stellt sicher, dass das Material Nodes verwendet
    if not material.use_nodes:
        material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links

    # Erstellt einen neuen Image Texture Node
    texture_node = nodes.new(type='ShaderNodeTexImage')
    texture_node.image = bpy.data.images.load(texture_path)
    
    if texture_type in ['roughness', 'metallic']:
        texture_node.image.colorspace_settings.name = 'Non-Color'
        
        
    # Positioniert den neuen Node
    texture_node.location = (0, 0)

    # Benennt den Node basierend auf dem Typ der Textur
    texture_node.name = texture_node.label = texture_type

    # Verbindet den Node mit dem Principled BSDF, falls vorhanden
    bsdf = next((node for node in nodes if node.type == 'BSDF_PRINCIPLED'), None)
    if bsdf:
        if texture_type == 'metallic':
            links.new(texture_node.outputs['Color'], bsdf.inputs['Metallic'])
        elif texture_type == 'roughness':
            links.new(texture_node.outputs['Color'], bsdf.inputs['Roughness'])


def setup_pbr_materials(search_base_path):
    """This function will find the textures from the file directory of the imported fbx file and assign them to the materials. Only works with CC4-Maya Export Preset."""
    for material in bpy.data.materials:
        print(f"Material: {material.name}")
        
        if material.use_nodes:
            material_base_name = "_".join(material.name.split("_")[:-1])
            
            if material_base_name:
                found_files = find_textures_custom_path(search_base_path, material_base_name)
                
                for file in found_files:
                    if '_roughness.png' in file:
                        print(f"    Gefunden Roughness: {file}")
                        add_texture_to_material(material, file, 'roughness')
                    elif '_metallic.png' in file:
                        print(f"    Gefunden Metallic: {file}")
                        add_texture_to_material(material, file, 'metallic')
        else:
            print("  Material verwendet keine Nodes.")