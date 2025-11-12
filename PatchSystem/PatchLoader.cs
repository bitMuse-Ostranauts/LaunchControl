using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitJson;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Discovers and loads patch files from mod directories.
    /// Handles file scanning, sorting, and parsing.
    /// </summary>
    public class PatchLoader
    {
        private readonly BepInEx.Logging.ManualLogSource _logger;

        public PatchLoader(BepInEx.Logging.ManualLogSource logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Find all .patch files in a mod's Data directory.
        /// Returns files sorted alphabetically by filename.
        /// </summary>
        public List<string> FindPatchFiles(string modFolderPath, string[] ignorePatterns = null)
        {
            var patchFiles = new List<string>();

            string dataPath = Path.Combine(modFolderPath, "Data");
            if (!Directory.Exists(dataPath))
            {
                dataPath = Path.Combine(modFolderPath, "data");
            }

            if (!Directory.Exists(dataPath))
            {
                return patchFiles; // Silent if no Data folder
            }

            try
            {
                // Find all .patch files recursively
                string[] files = Directory.GetFiles(dataPath, "*.patch", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string normalizedPath = file.Replace('\\', '/');
                    
                    // Check ignore patterns
                    bool shouldIgnore = false;
                    if (ignorePatterns != null)
                    {
                        foreach (string pattern in ignorePatterns)
                        {
                            if (normalizedPath.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                shouldIgnore = true;
                                break;
                            }
                        }
                    }

                    if (!shouldIgnore)
                    {
                        patchFiles.Add(file);
                    }
                }

                // Sort alphabetically by filename (not full path)
                patchFiles.Sort((a, b) => 
                {
                    string fileA = Path.GetFileName(a);
                    string fileB = Path.GetFileName(b);
                    return string.Compare(fileA, fileB, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex)
            {
                LogError($"Error scanning for patch files in {dataPath}: {ex.Message}");
            }

            return patchFiles;
        }

        /// <summary>
        /// Load and parse a patch file.
        /// Returns list of patches (supports both single and array formats).
        /// Returns empty list if the file cannot be loaded or parsed.
        /// </summary>
        public List<JsonPatchFile> LoadPatchFile(string filePath)
        {
            var result = new List<JsonPatchFile>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LogError($"Patch file not found: {filePath}");
                return result;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                List<JsonPatchFile> patchFiles = JsonPatchFile.FromJson(jsonContent, filePath);

                foreach (JsonPatchFile patchFile in patchFiles)
                {
                    // Infer data type from file location if not specified
                    if (string.IsNullOrEmpty(patchFile.Target.File))
                    {
                        patchFile.InferredDataType = InferDataTypeFromPath(filePath);
                    }

                    // Validate the patch file
                    List<string> errors;
                    if (!patchFile.Validate(out errors))
                    {
                        LogError($"Patch validation failed in {Path.GetFileName(filePath)}");
                        foreach (string error in errors)
                        {
                            LogError($"  - {error}");
                        }
                        continue; // Skip this patch but continue with others
                    }

                    result.Add(patchFile);
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error loading patch file {filePath}: {ex.Message}");
                LogError(ex.StackTrace);
                return result;
            }
        }

        /// <summary>
        /// Load all patch files from a mod directory.
        /// Returns list of successfully loaded patches sorted by filename.
        /// Each .patch file can contain one or multiple patches.
        /// </summary>
        public List<JsonPatchFile> LoadAllPatchFiles(string modFolderPath, string[] ignorePatterns = null)
        {
            var patches = new List<JsonPatchFile>();

            List<string> patchFiles = FindPatchFiles(modFolderPath, ignorePatterns);
            
            foreach (string filePath in patchFiles)
            {
                List<JsonPatchFile> loadedPatches = LoadPatchFile(filePath);
                if (loadedPatches != null && loadedPatches.Count > 0)
                {
                    patches.AddRange(loadedPatches);
                }
            }

            return patches;
        }

        /// <summary>
        /// Infer the data type (e.g., "interactions", "pledges") from the patch file path.
        /// Example: "ModPath/Data/interactions/patch.patch" -> "interactions"
        /// Example: "ModPath/Data/pledges/subfolder/patch.patch" -> "pledges"
        /// </summary>
        public string InferDataTypeFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            try
            {
                string normalizedPath = filePath.Replace('\\', '/');
                
                // Find "data/" or "Data/" in the path
                int dataIndex = normalizedPath.IndexOf("/data/", StringComparison.OrdinalIgnoreCase);
                if (dataIndex < 0)
                {
                    dataIndex = normalizedPath.IndexOf("/Data/", StringComparison.OrdinalIgnoreCase);
                }

                if (dataIndex >= 0)
                {
                    // Get the part after data/
                    string afterData = normalizedPath.Substring(dataIndex + 6); // "/data/" is 6 chars
                    
                    // Get the first directory after data/
                    int nextSlash = afterData.IndexOf('/');
                    if (nextSlash > 0)
                    {
                        string dataType = afterData.Substring(0, nextSlash);
                        return dataType.ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error inferring data type from path {filePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Map a data type folder name to the corresponding DataHandler dictionary name.
        /// </summary>
        public string MapDataTypeToDictionary(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
            {
                return null;
            }

            // Map common folder names to DataHandler dictionary names
            switch (dataType.ToLowerInvariant())
            {
                case "ads":
                    return "dictAds";
                case "ai_training":
                    return "dictAIPersonalities";
                case "attackmodes":
                    return "dictAModes";
                case "audioemitters":
                    return "dictAudioEmitters";
                case "careers":
                    return "dictCareers";
                case "chargeprofiles":
                    return "dictChargeProfiles";
                case "colors":
                    return "dictJsonColors";
                case "conditions":
                    return "dictConds";
                case "cos":
                case "condowners":
                    return "dictCOs";
                case "condrules":
                    return "dictCondRules";
                case "condtrigs":
                    return "dictCTs";
                case "context":
                    return "dictContext";
                case "cooverlays":
                    return "dictCOOverlays";
                case "crime":
                    return "dictCrimes";
                case "dialogue":
                    return "dictDialogue";
                case "factions":
                    return "dictFactions";
                case "flaws":
                    return "dictFlaws";
                case "gasrespires":
                    return "dictGasRespires";
                case "guipropmaps":
                    return "dictGUIPropMapUnparsed";
                case "headlines":
                    return "dictHeadlines";
                case "homeworlds":
                    return "dictHomeworlds";
                case "info":
                    return "dictInfoNodes";
                case "installables":
                    return "dictInstallables";
                case "interaction_overrides":
                    return "dictIAOverrides";
                case "interactions":
                    return "dictInteractions";
                case "items":
                    return "dictItemDefs";
                case "jobitems":
                    return "dictJobitems";
                case "jobs":
                    return "dictJobs";
                case "ledgerdefs":
                    return "dictLedgerDefs";
                case "lifeevents":
                    return "dictLifeEvents";
                case "lights":
                    return "dictLights";
                case "loot":
                    return "dictLoot";
                case "music":
                    return "dictMusic";
                case "parallax":
                    return "dictParallax";
                case "pda_apps":
                    return "dictPDAAppIcons";
                case "personspecs":
                    return "dictPersonSpecs";
                case "pledges":
                    return "dictPledges";
                case "plot_beat_overrides":
                    return "dictPlotBeatOverrides";
                case "plot_beats":
                    return "dictPlotBeats";
                case "plot_manager":
                    return "dictPlotManager";
                case "plots":
                    return "dictPlots";
                case "powerinfos":
                    return "dictPowerInfo";
                case "rooms":
                    return "dictRoomSpecsTemp";
                case "ships":
                    return "dictShips";
                case "shipspecs":
                    return "dictShipSpecs";
                case "slot_effects":
                    return "dictSlotEffects";
                case "slots":
                    return "dictSlots";
                case "star_systems":
                    return "dictStarSystems";
                case "strings":
                    return "dictStrings";
                case "tickers":
                    return "dictTickers";
                case "tips":
                    return "dictTips";
                case "tokens":
                    return "dictJsonTokens";
                case "transit":
                    return "dictTransit";
                case "verbs":
                    return "dictJsonVerbs";
                case "wounds":
                    return "dictWounds";
                case "zone_triggers":
                    return "dictZoneTriggers";
                // Note: Some folders like conditions_simple, crewskins, manpages, names_*, traitscores
                // use temporary local dictionaries that are processed immediately and not stored in DataHandler
                // These cannot be patched via this system
                default:
                    return null;
            }
        }

        private void LogInfo(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo($"[PatchLoader] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning($"[PatchLoader] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError($"[PatchLoader] {message}");
            }
        }
    }
}

