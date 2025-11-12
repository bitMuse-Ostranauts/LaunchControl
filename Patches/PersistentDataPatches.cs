using HarmonyLib;
using Ostranauts.Bit.PersistentData;
using Ostranauts.Bit.Tasks;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patches to integrate the PersistentData system with Ostranauts' save/load flow.
    /// </summary>
    [HarmonyPatch]
    public static class PersistentDataPatches
    {
        /// <summary>
        /// Patch LoadManager.SaveGameData to save persistent data before game data is saved.
        /// This runs at the start of SaveGameData after the save folder has been created
        /// by SaveGame, ensuring our files are included in the save folder.
        /// </summary>
        [HarmonyPatch(typeof(LoadManager), "SaveGameData", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static void SaveGameData_Prefix(string folderPath)
        {
            Debug.Log($"[PersistentData] SaveGameData_Prefix called! folderPath = {folderPath}");
            
            try
            {
                // Clean up orphaned task data before saving
                try
                {
                    TaskDataProvider.Instance.CleanupOrphanedData();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PersistentData] Error cleaning orphaned task data: {ex.Message}\n{ex.StackTrace}");
                }
                
                // Call PersistentDataManager to save all registered handlers
                PersistentDataManager.Instance.SaveAll(folderPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PersistentData] Exception in SaveGameData_Prefix: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Patch LoadManager.OnLoadSelectedSave to capture the dictFiles after zip extraction.
        /// This allows us to load mod data from both compressed and uncompressed saves
        /// using the game's own file loading infrastructure.
        /// </summary>
        [HarmonyPatch(typeof(LoadManager), "OnLoadSelectedSave")]
        public static class OnLoadSelectedSavePatch
        {
            [HarmonyPostfix]
            static void Postfix(SaveInfo saveInfo, LoadManager __instance)
            {
                Debug.Log("[PersistentData] OnLoadSelectedSave_Postfix called!");
                
                try
                {
                    if (saveInfo == null)
                    {
                        Debug.LogWarning("[PersistentData] OnLoadSelectedSave called with null saveInfo");
                        return;
                    }
                    
                    Debug.Log($"[PersistentData] SaveInfo.Path = {saveInfo.Path}");

                    // At this point, the game has already extracted the zip if needed
                    // Check if save is compressed or not
                    string[] files = Directory.GetFiles(saveInfo.Path);
                    bool isCompressed = false;
                    string zipFile = null;
                    
                    foreach (string file in files)
                    {
                        if (Path.GetExtension(file) == ".zip")
                        {
                            isCompressed = true;
                            zipFile = file;
                            break;
                        }
                    }

                    if (!isCompressed)
                    {
                        // Uncompressed save - load directly from disk
                        Debug.Log($"[PersistentData] Loading from uncompressed save: {saveInfo.Path}");
                        PersistentDataManager.Instance.LoadAll(saveInfo.Path);
                    }
                    else
                    {
                        // Compressed save - we need to extract and read mod files ourselves
                        // The game has already extracted to memory, but we need to do it again for mod files
                        Debug.Log($"[PersistentData] Loading from compressed save: {zipFile}");
                        LoadFromZip(zipFile, saveInfo.Path);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PersistentData] Exception in OnLoadSelectedSave_Postfix: {ex.Message}\n{ex.StackTrace}");
                }
            }

            private static void LoadFromZip(string zipPath, string savePath)
            {
                try
                {
                    // Use the game's own zip extractor to get all files from the zip
                    DotNetZipCompressor compressor = new DotNetZipCompressor();
                    Dictionary<string, byte[]> allFiles = compressor.ExtractArchive(zipPath);

                    if (allFiles == null || allFiles.Count == 0)
                    {
                        Debug.LogWarning("[PersistentData] Failed to extract zip archive or archive is empty");
                        return;
                    }

                    // Filter to only mod files
                    Dictionary<string, byte[]> modFiles = new Dictionary<string, byte[]>();
                    foreach (KeyValuePair<string, byte[]> kvp in allFiles)
                    {
                        if (kvp.Key.StartsWith("mods/", StringComparison.OrdinalIgnoreCase))
                        {
                            modFiles[kvp.Key] = kvp.Value;
                        }
                    }

                    if (modFiles.Count > 0)
                    {
                        Debug.Log($"[PersistentData] Found {modFiles.Count} mod files in compressed save");
                        PersistentDataManager.Instance.LoadAllFromMemory(savePath, modFiles);
                    }
                    else
                    {
                        Debug.Log("[PersistentData] No mod data found in compressed save");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PersistentData] Failed to load mod data from zip: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Initialize the load listener. Called by LaunchControl during startup.
        /// This is now a no-op since we load directly in OnLoadSelectedSave prefix.
        /// </summary>
        public static void InitializeLoadListener()
        {
            // No longer needed - we load directly in OnLoadSelectedSave prefix
            Debug.Log("[PersistentData] Load system initialized (using OnLoadSelectedSave prefix)");
        }

        /// <summary>
        /// Cleanup the load listener. Called when LaunchControl is destroyed.
        /// This is now a no-op since we load directly in OnLoadSelectedSave prefix.
        /// </summary>
        public static void CleanupLoadListener()
        {
            // No longer needed
        }
    }
}

