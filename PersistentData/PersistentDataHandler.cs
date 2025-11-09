using BepInEx.Logging;

namespace Ostranauts.Bit.PersistentData
{
    /// <summary>
    /// Abstract base class for persistent data handlers that save/load custom mod data with game saves.
    /// Inherit from this class to implement custom save/load logic for your mod.
    /// </summary>
    public abstract class PersistentDataHandler
    {
        /// <summary>
        /// Unique module identifier for this handler (e.g., "smarterhauling").
        /// This will be used as the folder name under {savepath}/mods/{ModuleName}/
        /// Must be lowercase, alphanumeric with no spaces.
        /// </summary>
        public abstract string ModuleName { get; }

        /// <summary>
        /// Logger instance for this handler. Set by the mod when registering.
        /// Use this to log messages during save/load operations.
        /// </summary>
        public ManualLogSource Logger { get; set; }

        /// <summary>
        /// Validate whether this handler can save data.
        /// Called before Save() to check if saving is possible/needed.
        /// Return false to skip saving for this handler.
        /// </summary>
        /// <returns>True if save should proceed, false to skip</returns>
        public abstract bool CanSave();

        /// <summary>
        /// Validate whether this handler can load data.
        /// Called before Load() to check if loading is possible.
        /// Return false to skip loading for this handler.
        /// </summary>
        /// <returns>True if load should proceed, false to skip</returns>
        public abstract bool CanLoad();

        /// <summary>
        /// Save custom mod data to the specified folder.
        /// This method is called synchronously during the game save process.
        /// Write your files to modFolderPath (already created for you).
        /// Example: Path.Combine(modFolderPath, "mydata.json")
        /// </summary>
        /// <param name="modFolderPath">Full path to your mod's save folder: {savepath}/mods/{ModuleName}/</param>
        public abstract void Save(string modFolderPath);

        /// <summary>
        /// Load custom mod data from the specified folder.
        /// This method is called after the main game save is loaded.
        /// Read your files from modFolderPath.
        /// Handle missing files gracefully (they may not exist in older saves).
        /// </summary>
        /// <param name="modFolderPath">Full path to your mod's save folder: {savepath}/mods/{ModuleName}/</param>
        public abstract void Load(string modFolderPath);

        /// <summary>
        /// Load custom mod data from memory (for compressed saves).
        /// This method is called when loading from a compressed save where files are extracted to memory.
        /// The default implementation looks for files in dictFiles with keys like "mods/{ModuleName}/filename".
        /// Override this if you need custom behavior for loading from memory.
        /// </summary>
        /// <param name="saveFolderPath">The save folder path (for reference)</param>
        /// <param name="dictFiles">Dictionary of file paths to file contents (byte arrays)</param>
        public virtual void LoadFromMemory(string saveFolderPath, System.Collections.Generic.Dictionary<string, byte[]> dictFiles)
        {
            // Default implementation: look for files with our module name prefix
            string modPrefix = "mods/" + ModuleName + "/";
            bool foundAnyFiles = false;

            foreach (var kvp in dictFiles)
            {
                if (kvp.Key.StartsWith(modPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundAnyFiles = true;
                    string relativePath = kvp.Key.Substring(modPrefix.Length);
                    string fileName = System.IO.Path.GetFileName(relativePath);
                    
                    LogInfo($"Loading file from memory: {relativePath}");
                    
                    // Create a temporary file for handlers that expect file paths
                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Ostranauts_ModLoad_" + System.Guid.NewGuid());
                    System.IO.Directory.CreateDirectory(tempPath);
                    string tempFile = System.IO.Path.Combine(tempPath, fileName);
                    
                    try
                    {
                        System.IO.File.WriteAllBytes(tempFile, kvp.Value);
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Failed to write temp file: {ex.Message}");
                    }
                }
            }

            if (!foundAnyFiles)
            {
                LogInfo("No files found in compressed save (this is normal for saves without mod data)");
            }
            else
            {
                // Note: The default implementation doesn't call Load() because handlers expect folder paths
                // Subclasses should override this method if they want to load from memory
                LogWarning("LoadFromMemory was called but handler does not override it. " +
                          "Consider overriding LoadFromMemory() for better compressed save support.");
            }
        }

        /// <summary>
        /// Helper method to log info messages with mod name prefix.
        /// </summary>
        protected void LogInfo(string message)
        {
            Logger?.LogInfo($"[{ModuleName}] {message}");
        }

        /// <summary>
        /// Helper method to log warning messages with mod name prefix.
        /// </summary>
        protected void LogWarning(string message)
        {
            Logger?.LogWarning($"[{ModuleName}] {message}");
        }

        /// <summary>
        /// Helper method to log error messages with mod name prefix.
        /// </summary>
        protected void LogError(string message)
        {
            Logger?.LogError($"[{ModuleName}] {message}");
        }
    }
}

