import json
import bpy

class UMAData_Race:
    def __init__(self, name, hipHeight, meshes, overlays, slots, **kwargs):  # Fügen Sie **kwargs hinzu
        self.type = "race"
        self.name = name
        self.hipHeight = hipHeight
        self.meshes = meshes
        self.overlays = overlays
        self.slots = slots

class UMAData_Cloth:
    def __init__(self, compatibleRaces, meshes, overlays, slots, **kwargs):  # Fügen Sie **kwargs hinzu
        self.type = "cloth"
        self.compatibleRaces = compatibleRaces
        self.meshes = meshes
        self.overlays = overlays
        self.slots = slots

class UMAData_Slot:
    def __init__(self, name, mesh, overlay, **kwargs):  # Fügen Sie **kwargs hinzu
        self.name = name
        self.mesh = mesh
        self.overlay = overlay
        self.wardrobeSlot = ""

class UMAData_Overlay:
    def __init__(self, name, material, **kwargs):  # Fügen Sie **kwargs hinzu
        self.name = name
        self.material = material


def save_to_json_file(data, file_path):
    with open(file_path, 'w') as file:
        json.dump(data.__dict__, file, default=lambda o: o.__dict__, indent=4)

def load_from_json_file(file_path, data_class):
    with open(file_path, 'r') as file:
        data_dict = json.load(file)
        # Entfernen Sie 'type', da es nicht als Argument verwendet wird
        data_dict.pop('type', None)
        return data_class(**data_dict)


def save_to_scene_properties(data, prop_name):
    bpy.context.scene[prop_name] = json.dumps(data.__dict__, default=lambda o: o.__dict__, indent=4)

def load_from_scene_properties(prop_name, data_class):
    json_str = bpy.context.scene.get(prop_name, '{}')
    data_dict = json.loads(json_str)
    # Entfernen Sie 'type', da es nicht als Argument verwendet wird
    data_dict.pop('type', None)
    return data_class(**data_dict)





# # Speichern in JSON-Datei
# save_to_json_file(race_instance, 'race_data.json')
# save_to_json_file(cloth_instance, 'cloth_data.json')

# # Laden aus JSON-Datei
# loaded_race_instance = load_from_json_file('race_data.json', UMAData_Race)
# loaded_cloth_instance = load_from_json_file('cloth_data.json', UMAData_Cloth)

# # Speichern in Blender-Szeneneigenschaften
# save_to_scene_properties(race_instance, 'UMAData_Race')
# save_to_scene_properties(cloth_instance, 'UMAData_Cloth')

# # Laden aus Blender-Szeneneigenschaften
# loaded_race_instance_from_scene = load_from_scene_properties('UMAData_Race', UMAData_Race)
# loaded_cloth_instance_from_scene = load_from_scene_properties('UMAData_Cloth', UMAData_Cloth)



