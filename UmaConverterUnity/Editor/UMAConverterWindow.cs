using UnityEngine;
using UnityEditor;
namespace UMAConverter
{
    public class UMAConverterWindow : EditorWindow
    {
        private bool integrationGameCreatorInventory = false;

        // Füge hier weitere Booleans hinzu, wie benötigt.

        // Menüpunkt hinzufügen
        [MenuItem("Tools/Character Creator 4 To UMA")]
        public static void ShowWindow()
        {
            // Fenster öffnen
            EditorWindow window = GetWindow<UMAConverterWindow>("UMA Converter Configuration");
            window.minSize = new Vector2(400, 400);
            window.maxSize = new Vector2(400, 800);
        }

        private void OnGUI()
        {
            bool currentIntegrationGameCreatorInventory = CheckScriptingSymbol("UMAConverterGCInventory");


            // Credits and Links
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation", GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL("https://github.com/valentinwinkelmann/CC2UMAConverter/blob/main/readme.md");
            }
            if (GUILayout.Button("@VWGAMEDEV", GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL("https://twitter.com/VWGAMEDEV");
            }
            GUILayout.EndHorizontal();


            // Show a HelpBox while compiling
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("The Editor is currently compiling. Please wait until it's finished.", MessageType.Info);
                return;
            }

            UMAConverterSettings settings = UMAConverterSettings.Instance;
            // GUI Elemente
            GUILayout.Label("UMA Converter Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can change the default behavior of the Character Creator 4 to UMA Converter here and enable integrations with other assets.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Integrations:", EditorStyles.largeLabel);
            // Speichere die Änderung der Integration beim Ändern des Toggles
            bool newIntegrationGameCreatorInventory = EditorGUILayout.Toggle("GameCreator Inventory", currentIntegrationGameCreatorInventory);
            if (newIntegrationGameCreatorInventory != currentIntegrationGameCreatorInventory)
            {
                UpdateScriptingSymbol("UMAConverterGCInventory", newIntegrationGameCreatorInventory);
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("UMAConverterSettings konnte nicht geladen oder erstellt werden.", MessageType.Error);
                return;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UMA Converter Settings", EditorStyles.largeLabel);
            // Editor für das ScriptableObject darstellen
            Editor editor = Editor.CreateEditor(settings);
            editor.OnInspectorGUI();
        }

        private void UpdateScriptingSymbol(string symbol, bool enable)
        {
            // Hole die aktuellen Scripting Define Symbols
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Prüfe, ob das Symbol bereits gesetzt ist
            if (enable && !symbols.Contains(symbol))
            {
                // Füge das Symbol hinzu
                symbols += ";" + symbol;
            }
            else if (!enable && symbols.Contains(symbol))
            {
                // Entferne das Symbol
                symbols = symbols.Replace(symbol + ";", "").Replace(symbol, "");
            }

            // Aktualisiere die Scripting Define Symbols
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
        }
        private bool CheckScriptingSymbol(string symbol)
        {
            // Hole die aktuellen Scripting Define Symbols
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Prüfe, ob das Symbol gesetzt ist
            return symbols.Contains(symbol);
        }
    }


}