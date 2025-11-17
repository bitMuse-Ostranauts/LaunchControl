using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Ostranauts.Bit.Items.Categories;
using UnityEngine;

namespace Ostranauts.Bit
{
    /// <summary>
    /// LaunchControl - Core library providing command registration and utility functions for Ostranauts mods
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class LaunchControlPlugin : BaseUnityPlugin
    {
        // Delegate declaration compatible with Unity's older Mono runtime
        private delegate void InitDelegate();
        internal static new ManualLogSource Logger;
        private Harmony _harmony;
        private GameObject _launchControlObject;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} is loading...");

            // Initialize LaunchControl singleton
            _launchControlObject = new GameObject("LaunchControl");
            DontDestroyOnLoad(_launchControlObject);

            _launchControlObject.AddComponent<LaunchControl>();

            Logger.LogInfo("LaunchControl singleton initialized");

            // Apply Harmony patches
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(LaunchControlPlugin).Assembly);
            
            // Log all patches applied
            var patches = _harmony.GetPatchedMethods();
            int patchCount = 0;
            foreach (var method in patches)
            {
                patchCount++;
                Logger.LogInfo($"Patched method: {method.DeclaringType?.Name}.{method.Name}");
            }
            
            Logger.LogInfo($"LaunchControl patches applied successfully! ({patchCount} methods patched)");

            // Register built-in commands
            RegisterBuiltInCommands();

            Logger.LogInfo("LaunchControl loaded successfully!");
            Logger.LogInfo("  Command registration system available");
            Logger.LogInfo("  Call LaunchControl.RegisterCommand(name, callback) from your mods");
        }

        private void Start()
        {
            // Use a coroutine to wait for DataHandler.bLoaded since delegate subscription is causing issues
            StartCoroutine(WaitForDataHandlerLoad());
        }

        private System.Collections.IEnumerator WaitForDataHandlerLoad()
        {
            // Wait until DataHandler finishes loading
            while (!DataHandler.bLoaded)
            {
                yield return null;
            }
            
            Logger.LogInfo("DataHandler loading complete, initializing item categories...");
            InitializeItemCategories();
        }

        private void InitializeItemCategories()
        {

            if (LaunchControl.Instance == null)
            {
                Logger.LogError("Cannot initialize item categories: LaunchControl instance or ItemCategoryManager is null");
                return;
            }

            if (DataHandler.dictCOs == null || DataHandler.dictCOs.Count == 0)
            {
                Logger.LogWarning("DataHandler.dictCOs is not yet available. Skipping item category initialization.");
                return;
            }

            try
            {
                Logger.LogInfo("Initializing item categories after DataHandler load...");
                DefaultCategories.Initialize(LaunchControl.Instance.Items.Categories);
                
                // Populate categories from predicates now that all items are loaded
                LaunchControl.Instance.Items.Categories.PopulateFromPredicates();
                
                Logger.LogInfo("Item category system initialized successfully");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to initialize item categories: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }

        private void RegisterBuiltInCommands()
        {
            // ListSprites command for discovering available sprites
            _launchControlObject.AddComponent<Commands.ListSpritesCommand>();

            Logger.LogInfo("Built-in commands registered");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Logger.LogInfo("LaunchControl unloaded");
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.ostranauts.LaunchControl";
        public const string PLUGIN_NAME = "LaunchControl";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
