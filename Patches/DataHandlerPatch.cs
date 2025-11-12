using HarmonyLib;
using Ostranauts.Bit.PatchSystem;
using System;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patch to track mod loads and apply patches after all data is loaded.
    /// We queue mods during LoadMod calls, then process all patches when LoadComplete fires.
    /// </summary>
    [HarmonyPatch(typeof(DataHandler), "LoadMod")]
    public static class DataHandlerPatch
    {
        /// <summary>
        /// Postfix patch that runs after DataHandler.LoadMod completes.
        /// This queues mods for patching after data is fully loaded.
        /// </summary>
        [HarmonyPostfix]
        public static void LoadMod_Postfix(string strFolderPath, string[] aIgnorePatterns, JsonModInfo jmi)
        {
            try
            {
                // Check if LaunchControl is initialized and has a patch manager
                if (LaunchControl.Instance == null || LaunchControl.Instance.PatchManager == null)
                {
                    return;
                }

                // Skip the core game data folder - only process actual mods
                if (strFolderPath.Contains("StreamingAssets"))
                {
                    return;
                }

                // Only queue patches if the mod loaded successfully
                if (jmi != null && jmi.Status == GUIModRow.ModStatus.Missing)
                {
                    return;
                }

                // IMPORTANT: By the time this postfix runs, DataHandler.LoadMod has already
                // appended "data/" to strFolderPath (line 425). We need to get back to the
                // mod's root folder to look for patch files.
                string modRootPath = strFolderPath;
                if (modRootPath.EndsWith("data/") || modRootPath.EndsWith("data\\"))
                {
                    // Remove the "data/" suffix to get the mod's root folder
                    modRootPath = modRootPath.Substring(0, modRootPath.Length - 5);
                }

                // Queue this mod for patching (will be processed when LoadComplete fires)
                LaunchControl.Instance.PatchManager.QueueModForPatching(modRootPath, aIgnorePatterns);
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"JSON Patch System error: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }
    }
}

