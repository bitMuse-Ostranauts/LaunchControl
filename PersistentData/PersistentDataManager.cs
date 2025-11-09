using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ostranauts.Bit.PersistentData
{
    /// <summary>
    /// Manager for persistent data handlers. Coordinates save/load operations across all registered mods.
    /// This is a singleton that gets initialized by LaunchControl.
    /// </summary>
    public class PersistentDataManager
    {
        private static PersistentDataManager _instance;

        private readonly Dictionary<string, PersistentDataHandler> _handlers;
        private ManualLogSource _logger;

        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static PersistentDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PersistentDataManager();
                }
                return _instance;
            }
        }

        private PersistentDataManager()
        {
            _handlers = new Dictionary<string, PersistentDataHandler>();
        }

        /// <summary>
        /// Set the logger for this manager. Called by LaunchControl during initialization.
        /// </summary>
        public void SetLogger(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register a persistent data handler for a mod.
        /// Should be called during mod initialization (typically in Plugin.Awake()).
        /// </summary>
        /// <param name="moduleName">Unique module name (must match handler.ModuleName)</param>
        /// <param name="handler">The handler instance to register</param>
        public void RegisterHandler(string moduleName, PersistentDataHandler handler)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                _logger?.LogError("[PersistentData] Cannot register handler with null or empty module name");
                return;
            }

            if (handler == null)
            {
                _logger?.LogError($"[PersistentData] Cannot register null handler for module '{moduleName}'");
                return;
            }

            if (moduleName != handler.ModuleName)
            {
                _logger?.LogError($"[PersistentData] Module name mismatch: parameter '{moduleName}' != handler.ModuleName '{handler.ModuleName}'");
                return;
            }

            if (_handlers.ContainsKey(moduleName))
            {
                _logger?.LogWarning($"[PersistentData] Handler for module '{moduleName}' is already registered. Replacing.");
            }

            _handlers[moduleName] = handler;
            _logger?.LogInfo($"[PersistentData] Registered handler for module '{moduleName}'");
        }

        /// <summary>
        /// Unregister a persistent data handler.
        /// </summary>
        /// <param name="moduleName">The module name to unregister</param>
        public void UnregisterHandler(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                return;
            }

            if (_handlers.Remove(moduleName))
            {
                _logger?.LogInfo($"[PersistentData] Unregistered handler for module '{moduleName}'");
            }
        }

        /// <summary>
        /// Get count of registered handlers
        /// </summary>
        public int GetHandlerCount()
        {
            return _handlers.Count;
        }

        /// <summary>
        /// Save all registered handlers' data to the save folder.
        /// Called during game save process. Runs synchronously on main thread.
        /// </summary>
        /// <param name="saveFolderPath">Path to the save folder (e.g., {SavesPath}/{saveName}/)</param>
        public void SaveAll(string saveFolderPath)
        {
            if (string.IsNullOrEmpty(saveFolderPath))
            {
                _logger?.LogError("[PersistentData] SaveAll called with null or empty save folder path");
                return;
            }

            // Create a copy of handlers to avoid modification during iteration
            List<PersistentDataHandler> handlersToSave = new List<PersistentDataHandler>(_handlers.Values);

            if (handlersToSave.Count == 0)
            {
                _logger?.LogInfo("[PersistentData] No handlers registered, skipping save");
                return;
            }

            _logger?.LogInfo($"[PersistentData] Saving data for {handlersToSave.Count} module(s)...");

            // Create mods folder if it doesn't exist
            string modsFolder = Path.Combine(saveFolderPath, "mods");
            try
            {
                if (!Directory.Exists(modsFolder))
                {
                    Directory.CreateDirectory(modsFolder);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[PersistentData] Failed to create mods folder: {ex.Message}");
                return;
            }

            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var handler in handlersToSave)
            {
                try
                {
                    // Check if handler can save
                    if (!handler.CanSave())
                    {
                        _logger?.LogInfo($"[PersistentData] Skipping save for module '{handler.ModuleName}' (CanSave returned false)");
                        skipCount++;
                        continue;
                    }

                    // Create module folder
                    string modFolder = Path.Combine(modsFolder, handler.ModuleName);
                    if (!Directory.Exists(modFolder))
                    {
                        Directory.CreateDirectory(modFolder);
                    }

                    // Call handler's save method
                    handler.Save(modFolder);
                    successCount++;
                    _logger?.LogInfo($"[PersistentData] Successfully saved data for module '{handler.ModuleName}'");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger?.LogError($"[PersistentData] Error saving data for module '{handler.ModuleName}': {ex.Message}\n{ex.StackTrace}");
                    handler.Logger?.LogError($"Save failed: {ex.Message}");
                }
            }

            _logger?.LogInfo($"[PersistentData] Save complete: {successCount} succeeded, {skipCount} skipped, {errorCount} errors");
        }

        /// <summary>
        /// Load all registered handlers' data from the save folder.
        /// Called after main game save is loaded. Runs synchronously on main thread.
        /// </summary>
        /// <param name="saveFolderPath">Path to the save folder (e.g., {SavesPath}/{saveName}/)</param>
        public void LoadAll(string saveFolderPath)
        {
            if (string.IsNullOrEmpty(saveFolderPath))
            {
                _logger?.LogError("[PersistentData] LoadAll called with null or empty save folder path");
                return;
            }

            // Create a copy of handlers to avoid modification during iteration
            List<PersistentDataHandler> handlersToLoad = new List<PersistentDataHandler>(_handlers.Values);

            if (handlersToLoad.Count == 0)
            {
                _logger?.LogInfo("[PersistentData] No handlers registered, skipping load");
                return;
            }

            _logger?.LogInfo($"[PersistentData] Loading data for {handlersToLoad.Count} module(s)...");

            string modsFolder = Path.Combine(saveFolderPath, "mods");
            if (!Directory.Exists(modsFolder))
            {
                _logger?.LogInfo("[PersistentData] No mods folder found in save (this is normal for older saves)");
                return;
            }

            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var handler in handlersToLoad)
            {
                try
                {
                    // Check if handler can load
                    if (!handler.CanLoad())
                    {
                        _logger?.LogInfo($"[PersistentData] Skipping load for module '{handler.ModuleName}' (CanLoad returned false)");
                        skipCount++;
                        continue;
                    }

                    // Check if module folder exists
                    string modFolder = Path.Combine(modsFolder, handler.ModuleName);
                    if (!Directory.Exists(modFolder))
                    {
                        _logger?.LogInfo($"[PersistentData] No save data found for module '{handler.ModuleName}' (folder doesn't exist)");
                        skipCount++;
                        continue;
                    }

                    // Call handler's load method
                    handler.Load(modFolder);
                    successCount++;
                    _logger?.LogInfo($"[PersistentData] Successfully loaded data for module '{handler.ModuleName}'");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger?.LogError($"[PersistentData] Error loading data for module '{handler.ModuleName}': {ex.Message}\n{ex.StackTrace}");
                    handler.Logger?.LogError($"Load failed: {ex.Message}");
                }
            }

            _logger?.LogInfo($"[PersistentData] Load complete: {successCount} succeeded, {skipCount} skipped, {errorCount} errors");
        }

        /// <summary>
        /// Load all registered handlers' data from memory (for compressed saves).
        /// Called when loading from a compressed save where files have been extracted to a dictionary.
        /// </summary>
        /// <param name="saveFolderPath">Path to the save folder (for reference)</param>
        /// <param name="dictFiles">Dictionary of file paths to file contents extracted from zip</param>
        public void LoadAllFromMemory(string saveFolderPath, Dictionary<string, byte[]> dictFiles)
        {
            if (string.IsNullOrEmpty(saveFolderPath))
            {
                _logger?.LogError("[PersistentData] LoadAllFromMemory called with null or empty save folder path");
                return;
            }

            if (dictFiles == null || dictFiles.Count == 0)
            {
                _logger?.LogInfo("[PersistentData] No files in dictFiles, skipping load from memory");
                return;
            }

            // Create a copy of handlers to avoid modification during iteration
            List<PersistentDataHandler> handlersToLoad = new List<PersistentDataHandler>(_handlers.Values);

            if (handlersToLoad.Count == 0)
            {
                _logger?.LogInfo("[PersistentData] No handlers registered, skipping load from memory");
                return;
            }

            _logger?.LogInfo($"[PersistentData] Loading data from memory for {handlersToLoad.Count} module(s)...");

            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var handler in handlersToLoad)
            {
                try
                {
                    // Check if handler can load
                    if (!handler.CanLoad())
                    {
                        _logger?.LogInfo($"[PersistentData] Skipping load from memory for module '{handler.ModuleName}' (CanLoad returned false)");
                        skipCount++;
                        continue;
                    }

                    // Check if there are any files for this module in dictFiles
                    string modPrefix = "mods/" + handler.ModuleName + "/";
                    bool hasModFiles = false;
                    foreach (string key in dictFiles.Keys)
                    {
                        if (key.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            hasModFiles = true;
                            break;
                        }
                    }

                    if (!hasModFiles)
                    {
                        _logger?.LogInfo($"[PersistentData] No save data found in memory for module '{handler.ModuleName}'");
                        skipCount++;
                        continue;
                    }

                    // Call handler's LoadFromMemory method
                    handler.LoadFromMemory(saveFolderPath, dictFiles);
                    successCount++;
                    _logger?.LogInfo($"[PersistentData] Successfully loaded data from memory for module '{handler.ModuleName}'");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger?.LogError($"[PersistentData] Error loading data from memory for module '{handler.ModuleName}': {ex.Message}\n{ex.StackTrace}");
                    handler.Logger?.LogError($"Load from memory failed: {ex.Message}");
                }
            }

            _logger?.LogInfo($"[PersistentData] Load from memory complete: {successCount} succeeded, {skipCount} skipped, {errorCount} errors");
        }
    }
}

