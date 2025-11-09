using BepInEx.Logging;
using LitJson;
using Ostranauts.Bit.PersistentData;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ostranauts.Bit.Tasks
{
    /// <summary>
    /// Handles saving and loading of Task2 extra data via the PersistentData system
    /// </summary>
    public class TaskDataHandler : PersistentDataHandler
    {
        private const string DATA_FILENAME = "task_data.json";

        public override string ModuleName
        {
            get { return "LaunchControl.tasks"; }
        }

        public override bool CanSave()
        {
            // Always save task data if any exists
            return true;
        }

        public override bool CanLoad()
        {
            // Always try to load task data
            return true;
        }

        public override void Save(string moduleFolder)
        {
            try
            {
                // Get all task data from TaskDataProvider
                Dictionary<string, Dictionary<string, object>> allData = TaskDataProvider.Instance.GetAllData();

                if (ReferenceEquals(allData, null) || allData.Count == 0)
                {
                    Logger.LogInfo("[TaskDataHandler] No task data to save");
                    return;
                }

                // Serialize to JSON-compatible format
                // Convert inner dictionaries' objects to JSON strings
                Dictionary<string, Dictionary<string, string>> saveData = new Dictionary<string, Dictionary<string, string>>();
                
                foreach (KeyValuePair<string, Dictionary<string, object>> taskEntry in allData)
                {
                    Dictionary<string, string> serializedData = new Dictionary<string, string>();
                    
                    foreach (KeyValuePair<string, object> dataEntry in taskEntry.Value)
                    {
                        if (!ReferenceEquals(dataEntry.Value, null))
                        {
                            try
                            {
                                // Serialize each value to JSON
                                string jsonValue = JsonMapper.ToJson(dataEntry.Value);
                                serializedData[dataEntry.Key] = jsonValue;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(string.Format("[TaskDataHandler] Failed to serialize value for key '{0}': {1}", 
                                    dataEntry.Key, ex.Message));
                            }
                        }
                    }
                    
                    if (serializedData.Count > 0)
                    {
                        saveData[taskEntry.Key] = serializedData;
                    }
                }

                // Write to file
                string json = JsonMapper.ToJson(saveData);
                string filePath = Path.Combine(moduleFolder, DATA_FILENAME);
                File.WriteAllText(filePath, json);

                Logger.LogInfo(string.Format("[TaskDataHandler] Saved {0} task data entries", saveData.Count));
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("[TaskDataHandler] Error saving task data: {0}\n{1}", ex.Message, ex.StackTrace));
                throw;
            }
        }

        public override void Load(string moduleFolder)
        {
            try
            {
                string filePath = Path.Combine(moduleFolder, DATA_FILENAME);
                
                if (!File.Exists(filePath))
                {
                    Logger.LogInfo("[TaskDataHandler] No task data file found (this is normal for new saves)");
                    return;
                }

                // Read and deserialize
                string json = File.ReadAllText(filePath);
                Dictionary<string, Dictionary<string, string>> saveData = 
                    JsonMapper.ToObject<Dictionary<string, Dictionary<string, string>>>(json);

                if (ReferenceEquals(saveData, null))
                {
                    Logger.LogWarning("[TaskDataHandler] Failed to deserialize task data");
                    return;
                }

                // Convert back to objects
                Dictionary<string, Dictionary<string, object>> allData = new Dictionary<string, Dictionary<string, object>>();
                
                foreach (KeyValuePair<string, Dictionary<string, string>> taskEntry in saveData)
                {
                    Dictionary<string, object> deserializedData = new Dictionary<string, object>();
                    
                    foreach (KeyValuePair<string, string> dataEntry in taskEntry.Value)
                    {
                        try
                        {
                            // Deserialize each value from JSON
                            // Store as JsonData object which can be converted later
                            JsonData jsonValue = JsonMapper.ToObject(dataEntry.Value);
                            deserializedData[dataEntry.Key] = jsonValue;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(string.Format("[TaskDataHandler] Failed to deserialize value for key '{0}': {1}", 
                                dataEntry.Key, ex.Message));
                        }
                    }
                    
                    if (deserializedData.Count > 0)
                    {
                        allData[taskEntry.Key] = deserializedData;
                    }
                }

                // Set data in TaskDataProvider
                TaskDataProvider.Instance.SetAllData(allData);

                Logger.LogInfo(string.Format("[TaskDataHandler] Loaded {0} task data entries", allData.Count));
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("[TaskDataHandler] Error loading task data: {0}\n{1}", ex.Message, ex.StackTrace));
                throw;
            }
        }

        public override void LoadFromMemory(string saveFolderPath, Dictionary<string, byte[]> dictFiles)
        {
            try
            {
                // Build the expected file path within the zip
                string expectedPath = "mods/" + ModuleName + "/" + DATA_FILENAME;
                
                // Try to find the file (case-insensitive search)
                byte[] fileData = null;
                foreach (KeyValuePair<string, byte[]> kvp in dictFiles)
                {
                    if (kvp.Key.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        fileData = kvp.Value;
                        break;
                    }
                }

                if (ReferenceEquals(fileData, null))
                {
                    Logger.LogInfo("[TaskDataHandler] No task data file found in memory (this is normal for new saves)");
                    return;
                }

                // Convert bytes to string and deserialize
                string json = System.Text.Encoding.UTF8.GetString(fileData);
                Dictionary<string, Dictionary<string, string>> saveData = 
                    JsonMapper.ToObject<Dictionary<string, Dictionary<string, string>>>(json);

                if (ReferenceEquals(saveData, null))
                {
                    Logger.LogWarning("[TaskDataHandler] Failed to deserialize task data from memory");
                    return;
                }

                // Convert back to objects (same as Load method)
                Dictionary<string, Dictionary<string, object>> allData = new Dictionary<string, Dictionary<string, object>>();
                
                foreach (KeyValuePair<string, Dictionary<string, string>> taskEntry in saveData)
                {
                    Dictionary<string, object> deserializedData = new Dictionary<string, object>();
                    
                    foreach (KeyValuePair<string, string> dataEntry in taskEntry.Value)
                    {
                        try
                        {
                            // Deserialize each value from JSON
                            JsonData jsonValue = JsonMapper.ToObject(dataEntry.Value);
                            deserializedData[dataEntry.Key] = jsonValue;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(string.Format("[TaskDataHandler] Failed to deserialize value for key '{0}': {1}", 
                                dataEntry.Key, ex.Message));
                        }
                    }
                    
                    if (deserializedData.Count > 0)
                    {
                        allData[taskEntry.Key] = deserializedData;
                    }
                }

                // Set data in TaskDataProvider
                TaskDataProvider.Instance.SetAllData(allData);

                Logger.LogInfo(string.Format("[TaskDataHandler] Loaded {0} task data entries from memory", allData.Count));
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("[TaskDataHandler] Error loading task data from memory: {0}\n{1}", 
                    ex.Message, ex.StackTrace));
                throw;
            }
        }
    }
}

