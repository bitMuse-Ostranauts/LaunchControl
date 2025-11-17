using HarmonyLib;
using Ostranauts.Bit.PatchSystem;
using System;
using System.Collections;
using System.IO;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Patch for DataHandler.Init to handle both vanilla and FFU loading.
    /// This runs after Init completes and detects whether FFU has modified the loading process.
    /// </summary>
    [HarmonyPatch(typeof(DataHandler), "Init")]
    public static class DataHandlerInitPatch
    {
        /// <summary>
        /// Postfix that runs after DataHandler.Init completes.
        /// This handles patch application for both vanilla and FFU-modded loading.
        /// </summary>
        [HarmonyPostfix]
        public static void Init_Postfix()
        {
            try
            {
                // Check if LaunchControl is initialized
                if (LaunchControl.Instance == null || LaunchControl.Instance.PatchManager == null)
                {
                    return;
                }

                // Check if this is FFU's synchronous loading by looking for FFU-specific indicators
                bool isFFULoading = DetectFFUSyncLoading();

                if (isFFULoading)
                {
                    LaunchControlPlugin.Logger.LogInfo("FFU synchronous loading detected, processing patches...");
                    ProcessFFUPatches();
                }
                else
                {
                    // Vanilla async loading - patches will be handled by DataHandlerPatch.LoadMod_Postfix
                    LaunchControlPlugin.Logger.LogInfo("Vanilla async loading detected, patches will be processed via LoadComplete");
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in DataHandler.Init postfix: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Detect if FFU mod is loaded by checking for FFU-specific types/fields.
        /// </summary>
        private static bool DetectFFUSyncLoading()
        {
            try
            {
                // Check for FFU's patch_DataHandler type
                var dataHandlerType = typeof(DataHandler);
                var assembly = dataHandlerType.Assembly;
                
                // FFU adds a static field called strModsPath to DataHandler
                var strModsPathField = dataHandlerType.GetField("strModsPath", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (strModsPathField != null)
                {
                    LaunchControlPlugin.Logger.LogInfo("FFU detected via strModsPath field");
                    return true;
                }

                // Also check for patch_DataHandler type in any loaded assembly
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var patchType = loadedAssembly.GetType("patch_DataHandler");
                        if (patchType != null)
                        {
                            LaunchControlPlugin.Logger.LogInfo($"FFU detected via patch_DataHandler type in {loadedAssembly.GetName().Name}");
                            return true;
                        }
                    }
                    catch
                    {
                        // Some assemblies can't be inspected, skip
                        continue;
                    }
                }

                // FFU not detected
                return false;
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error detecting FFU: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Process patches for all mods when FFU sync loading is detected.
        /// </summary>
        private static void ProcessFFUPatches()
        {
            try
            {
                if (DataHandler.dictModInfos == null || DataHandler.dictModInfos.Count == 0)
                {
                    LaunchControlPlugin.Logger.LogWarning("dictModInfos is empty");
                    return;
                }

                // Get the mods path
                string modsPath = GetModsPath();
                if (string.IsNullOrEmpty(modsPath))
                {
                    LaunchControlPlugin.Logger.LogWarning("Could not determine mods path");
                    return;
                }

                LaunchControlPlugin.Logger.LogInfo($"Mods path: {modsPath}");
                LaunchControlPlugin.Logger.LogInfo($"Processing {DataHandler.dictModInfos.Count} mod(s) for patches");

                // Process each mod
                int patchedModCount = 0;
                foreach (var modEntry in DataHandler.dictModInfos)
                {
                    string modKey = modEntry.Key;
                    
                    // Skip core game data
                    if (modKey == "core" || modKey == "Core")
                    {
                        continue;
                    }

                    // Construct the mod's root path
                    string modRootPath = Path.Combine(modsPath, modKey);

                    if (!Directory.Exists(modRootPath))
                    {
                        LaunchControlPlugin.Logger.LogWarning($"Mod path does not exist: {modRootPath}");
                        continue;
                    }

                    // Apply patches for this mod
                    LaunchControl.Instance.PatchManager.ApplyPatchesForMod(modRootPath, null);
                    patchedModCount++;
                }

                if (patchedModCount > 0)
                {
                    LaunchControlPlugin.Logger.LogInfo($"Processed patches for {patchedModCount} mod(s) via FFU compatibility");
                }
                else
                {
                    LaunchControlPlugin.Logger.LogInfo("No mods with patches found");
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error processing FFU patches: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Get the mods folder path.
        /// </summary>
        private static string GetModsPath()
        {
            try
            {
                // Try to get from DataHandler.strModFolder
                if (!string.IsNullOrEmpty(DataHandler.strModFolder))
                {
                    string path = DataHandler.strModFolder.Replace("loading_order.json", "");
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }

                // Fallback: check for FFU's strModsPath field
                var dataHandlerType = typeof(DataHandler);
                var strModsPathField = dataHandlerType.GetField("strModsPath", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (strModsPathField != null)
                {
                    string ffuPath = (string)strModsPathField.GetValue(null);
                    if (!string.IsNullOrEmpty(ffuPath) && Directory.Exists(ffuPath))
                    {
                        LaunchControlPlugin.Logger.LogInfo("Using FFU's strModsPath");
                        return ffuPath;
                    }
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error getting mods path: {ex.Message}");
            }

            return null;
        }
    }
}

