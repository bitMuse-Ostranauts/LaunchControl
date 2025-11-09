using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ostranauts.Bit.PersistentData
{
    /// <summary>
    /// Generic persistent data handler for saving/loading a list of objects as JSON.
    /// Inherit from this class to quickly implement save/load for a collection of data objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the list (must be serializable by Unity's JsonUtility)</typeparam>
    public abstract class ListPersistentDataHandler<T> : PersistentDataHandler
    {
        /// <summary>
        /// The filename to use for saving the list (e.g., "mydata.json").
        /// Override this property to customize the filename.
        /// </summary>
        protected virtual string FileName => "data.json";

        /// <summary>
        /// Get the current list of data objects to save.
        /// Called during Save() operation.
        /// </summary>
        /// <returns>List of objects to save, or null/empty if nothing to save</returns>
        protected abstract List<T> GetDataList();

        /// <summary>
        /// Set the list of data objects after loading.
        /// Called during Load() operation after deserializing from JSON.
        /// </summary>
        /// <param name="dataList">The loaded list of objects</param>
        protected abstract void SetDataList(List<T> dataList);

        /// <summary>
        /// Optional: Clear existing data before loading.
        /// Override this if you need to clear state before loading.
        /// Default implementation does nothing.
        /// </summary>
        protected virtual void ClearData()
        {
            // Default: do nothing
        }

        /// <summary>
        /// Optional: Validate a data object before saving.
        /// Override to filter out invalid objects.
        /// </summary>
        /// <param name="item">The item to validate</param>
        /// <returns>True if item should be saved, false to skip it</returns>
        protected virtual bool ValidateItem(T item)
        {
            return item != null;
        }

        /// <summary>
        /// Optional: Process a data object after loading.
        /// Override to perform post-load initialization or validation.
        /// </summary>
        /// <param name="item">The loaded item</param>
        /// <returns>The processed item (can return modified version or null to skip)</returns>
        protected virtual T ProcessLoadedItem(T item)
        {
            return item;
        }

        public sealed override void Save(string modFolderPath)
        {
            try
            {
                List<T> dataList = GetDataList();
                string filePath = Path.Combine(modFolderPath, FileName);

                // If no data, delete the file if it exists
                if (dataList == null || dataList.Count == 0)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        LogInfo($"Deleted empty {FileName}");
                    }
                    else
                    {
                        LogInfo("No data to save");
                    }
                    return;
                }

                // Filter out invalid items
                List<T> validItems = new List<T>();
                foreach (T item in dataList)
                {
                    if (ValidateItem(item))
                    {
                        validItems.Add(item);
                    }
                }

                if (validItems.Count == 0)
                {
                    LogInfo("No valid items to save after filtering");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return;
                }

                // Serialize to JSON array
                string json = SerializeList(validItems);
                
                // Write to file
                File.WriteAllText(filePath, json);
                
                LogInfo($"Saved {validItems.Count} item(s) to {FileName}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save {FileName}: {ex.Message}");
                throw;
            }
        }

        public sealed override void Load(string modFolderPath)
        {
            try
            {
                // Always clear existing data first, even if there's no file to load
                // This ensures we don't keep stale data from a previous save
                ClearData();

                string filePath = Path.Combine(modFolderPath, FileName);

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    LogInfo($"No {FileName} found (this is normal for saves without data)");
                    return;
                }

                // Read JSON from file
                string json = File.ReadAllText(filePath);

                // Deserialize and process
                LoadFromJson(json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load {FileName}: {ex.Message}");
                throw;
            }
        }

        public sealed override void LoadFromMemory(string saveFolderPath, Dictionary<string, byte[]> dictFiles)
        {
            try
            {
                // Always clear existing data first
                ClearData();

                // Look for our file in the dictionary
                string fileKey = $"mods/{ModuleName}/{FileName}";
                
                if (!dictFiles.ContainsKey(fileKey))
                {
                    LogInfo($"No {FileName} found in compressed save (this is normal for saves without data)");
                    return;
                }

                // Read JSON from byte array
                byte[] fileBytes = dictFiles[fileKey];
                string json = Encoding.UTF8.GetString(fileBytes);

                // Deserialize and process
                LoadFromJson(json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load {FileName} from memory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Common logic for loading from JSON string (used by both Load and LoadFromMemory).
        /// </summary>
        private void LoadFromJson(string json)
        {
            // Deserialize from JSON array
            List<T> loadedList = DeserializeList(json);

            if (loadedList == null || loadedList.Count == 0)
            {
                LogWarning($"{FileName} exists but contains no valid data");
                return;
            }

            // Process loaded items
            List<T> processedList = new List<T>();
            foreach (T item in loadedList)
            {
                T processedItem = ProcessLoadedItem(item);
                if (processedItem != null)
                {
                    processedList.Add(processedItem);
                }
            }

            // Set the loaded data
            SetDataList(processedList);

            LogInfo($"Loaded {processedList.Count} item(s) from {FileName}");
        }

        /// <summary>
        /// Serialize a list of objects to JSON array format using LitJson.
        /// Override this if you need custom serialization logic.
        /// </summary>
        protected virtual string SerializeList(List<T> list)
        {
            StringBuilder stringBuilder = new StringBuilder();
            JsonWriter jsonWriter = new JsonWriter(stringBuilder);
            jsonWriter.PrettyPrint = true;
            jsonWriter.IndentValue = 2;
            
            JsonMapper.ToJson(list, jsonWriter);
            
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Deserialize a JSON array to a list of objects using LitJson.
        /// Override this if you need custom deserialization logic.
        /// </summary>
        protected virtual List<T> DeserializeList(string json)
        {
            try
            {
                // LitJson can deserialize arrays directly to List<T>
                T[] array = JsonMapper.ToObject<T[]>(json);
                return new List<T>(array);
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to deserialize using LitJson: {ex.Message}");
                return new List<T>();
            }
        }
    }
}

