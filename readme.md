# 🩻Character Creator 4 To UMA
## 📖Description
This set of Two Plugins for Blender and Unity will help you to convert a Character Creator 4 Character to a fully functional and ready to use UMA Asset. The Plugin will not only allow you to convert a naked Character as a new UMA Race, it will also allow you to convert clothed Characters to UMA Wardrobe Slots. The Plugin will take care of the necessary steps and keeps everything as simple as possible. The main goal is to let you focus on preparing your Character and Clothing in Character Creator 4 and build a easy to use Brdige to UMA. While you can of course modify the resulting UMA Assets directly in Unity, the Plugin is designed to let you handle everything in Blender.

## 🎮Unity Plugin
The Plugin comes with an Unity Asset Postprocessor that will take the UMA Compatible outputs from Blender and automatically create a race or wardrobe slots for you. This Plugin will do a lot of steps in the Background but won't bother you with any of it.


## 🛠️Installation
### Blender Plugin
1. Download the latest release from the [Releases](https://github.com/...) page.
2. Open Blender and go to Edit > Preferences > Add-ons > Install
3. Select the downloaded zip file and click Install Add-on
4. Enable the Add-on by checking the box next to it
5. Click File > Defaults > Save Startup File to make sure the Add-on is enabled by default

### Unity Plugin
1. Simply open your Unity Package Manager and click on the + Button in the top left corner. Choose "Add package from git URL" and paste the following URL: ``https://github.com...``. That's it. The Plugin will be installed and ready to use.

## 🎛️Usage

### 📦Character Creator 4 Export
Exporting your Character from Character Creator 4 is pretty simple, but needs to follow a few very important steps, so the Blender Plugin can understant it's structure.
1. ```File > Export > Clothed Character```
2. Set Target Tool Preset to ```Maya```. (*Not Unity*)
3. Set FBX Options to ```Mesh```.
4. Set Default Pose to ```T-Pose```. (*if you don't use InstaLOD, yes you can*)

### 🩻Create a new UMA Race
To convert a naked CC4 Character to a new UMA Race, you will import the character in blender and Choose Rig Type: Race. When you press converting the Plugin will calculate some information about the rig in Background.
Inside the UMA Tab you will now be able to select which of the meshes should be exported. When you select one you will see a Slot Name field for the selected mesh. You have to fill this and it should be unique for each mesh. This will be the UMA Slot Name in Unity.
You Will Also see Available Overlays list of Material names. This materials are the one which UMA will Create and are defined by the Blender Material Names. You can Rename them using the default Blender Material Tab.

Use the Export button to Choose a location for your Character. Along your exported FBX file you will find a JSON file that contains some information about your character.
This JSON file is important when you want to convert new Clothings for your Character, so keep it safe.
You can now Drag and Drop booth files into your Unity Project and the Postprocessor will take care of the rest.

### 👕Create a new UMA Clothing
To convert a clothed CC4 Character to a new UMA Wardrobe Slot, you will import the character in blender and Choose Rig Type: Clothing. Before you can press the convert button you have to select a Race JSON File, that you created before.
Now you can press the convert button and the Plugin will do the rest. Like the UMA Race workflow you can now choose which meshes should be exported ( Race Meshes will be ignored and while they are in the Scene you dont have to worry about them ). As with the Race Creation you will have to fill the Slot Names and can rename the Material Names to your liking. You also have to Choose your desired Wardrobe Slot Type. The Plugin will give you a predefined list of Wardrobe Slots which UMA uses by Default but you can freely type any Wardrobe Slot Type you want, just make sure this is consistent with your other Clothing conversions.
Aft. You will find a JSON file next to the FBX file that contains some information about your clothing. This is the *_Cloth.json file.

### ⚠️Important Notes
- If you Export clothing for a Race you created before, you have to use exactly the same Character for the Clothing Exports. If you plan to export a set of multiple clothings over time, you should save your Race Template Character in Character Creator 4.
- The Plugin Exports JSON Files, which are simple but better keep them if you plan to create new clothings for your character in the future.
- The Plugin is not meant to let you modifiy and adjust the Mesh in Blender. You can do that, but it may break the scene and make the Plugin not work as expected with your modified meshes.
- The Plugin works currently only with PBR Materials. before Exporting your Character from Character Creator 4, it's therefore necessary to set the Material Type to PBR. At the moment there are no plans to extend the Plugin to handle other Material Types.

## 📜License
This Blender Plugin and Unity Plugin are licensed under the MIT License and are developed by [Valentin Winkelmann](https://vwgame.dev/).This project is not affiliated with Reallusion, Unity or the UMA Project.