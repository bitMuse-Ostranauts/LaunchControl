using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LitJson;
using UnityEngine;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Main coordinator for the JSON patch system.
    /// Manages loading, targeting, and applying patches to game data.
    /// </summary>
    public class PatchManager
    {
        private readonly BepInEx.Logging.ManualLogSource _logger;
        private readonly PatchLoader _loader;
        private readonly PatchApplier _applier;

        // Statistics
        private int _totalPatchesLoaded = 0;
        private int _totalPatchesApplied = 0;
        private int _totalOperationsApplied = 0;

        // Mod queue for deferred patching
        private class QueuedMod
        {
            public string FolderPath;
            public string[] IgnorePatterns;
        }
        private List<QueuedMod> _queuedMods = new List<QueuedMod>();
        private bool _loadCompleteHooked = false;

        public PatchManager(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
            _loader = new PatchLoader(logger);
            _applier = new PatchApplier(logger);
        }

        /// <summary>
        /// Queue a mod for patching. Patches will be applied when DataHandler.LoadComplete fires.
        /// </summary>
        public void QueueModForPatching(string modFolderPath, string[] ignorePatterns = null)
        {
            // Hook into LoadComplete on first queue
            if (!_loadCompleteHooked)
            {
                DataHandler.LoadComplete += OnLoadComplete;
                _loadCompleteHooked = true;
            }

            _queuedMods.Add(new QueuedMod
            {
                FolderPath = modFolderPath,
                IgnorePatterns = ignorePatterns
            });
        }

        /// <summary>
        /// Called when DataHandler.LoadComplete fires - all async loading is done.
        /// </summary>
        private void OnLoadComplete()
        {
            try
            {
                LogInfo($"JSON Patch System: Processing {_queuedMods.Count} mod(s)");

                // Process all queued mods
                foreach (QueuedMod mod in _queuedMods)
                {
                    ApplyPatchesForMod(mod.FolderPath, mod.IgnorePatterns);
                }

                // Clear queue
                _queuedMods.Clear();

                // Unsubscribe
                DataHandler.LoadComplete -= OnLoadComplete;
                _loadCompleteHooked = false;

                if (_totalPatchesApplied > 0)
                {
                    LogInfo($"JSON Patch System: Applied {_totalPatchesApplied} patch file(s) with {_totalOperationsApplied} operation(s)");
                }
            }
            catch (Exception ex)
            {
                LogError($"JSON Patch System error: {ex.Message}");
                LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Apply all patches for a specific mod.
        /// This is called after the mod's JSON files have been loaded.
        /// </summary>
        public void ApplyPatchesForMod(string modFolderPath, string[] ignorePatterns = null)
        {
            try
            {
                // Load all patch files from the mod
                List<JsonPatchFile> patches = _loader.LoadAllPatchFiles(modFolderPath, ignorePatterns);
                
                if (patches.Count == 0)
                {
                    return; // Silent if no patches
                }

                _totalPatchesLoaded += patches.Count;

                // Apply each patch in order (already sorted by PatchLoader)
                foreach (JsonPatchFile patch in patches)
                {
                    ApplyPatch(patch);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing patches for mod {modFolderPath}: {ex.Message}");
                LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Apply a single patch file.
        /// </summary>
        private void ApplyPatch(JsonPatchFile patch)
        {
            if (patch == null || patch.Target == null || patch.Operations == null)
            {
                LogError("Invalid patch file");
                return;
            }

            try
            {
                string patchName = System.IO.Path.GetFileName(patch.SourceFilePath);

                // Determine which dictionary to patch
                string dictionaryName = DetermineTargetDictionary(patch);
                if (string.IsNullOrEmpty(dictionaryName))
                {
                    LogError($"Patch '{patchName}': Could not determine target dictionary");
                    return;
                }

                // Get the dictionary from DataHandler using reflection
                object dictionary = GetDataHandlerDictionary(dictionaryName);
                if (dictionary == null)
                {
                    LogError($"Patch '{patchName}': Could not access {dictionaryName}");
                    return;
                }

                // Find matching targets
                List<string> targetKeys = FindMatchingTargets(dictionary, patch.Target);
                
                if (targetKeys.Count == 0)
                {
                    LogWarning($"Patch '{patchName}': No matching targets found");
                    return;
                }

                // Apply operations to each matching target
                int successCount = 0;
                foreach (string targetKey in targetKeys)
                {
                    if (ApplyPatchToTarget(dictionary, targetKey, patch.Operations))
                    {
                        successCount++;
                    }
                }

                if (successCount > 0)
                {
                    _totalPatchesApplied++;
                    LogInfo($"Patch '{patchName}': Applied to {successCount} target(s)");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error applying patch: {ex.Message}");
                LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Determine which DataHandler dictionary should be patched.
        /// </summary>
        private string DetermineTargetDictionary(JsonPatchFile patch)
        {
            // If file is specified in target, try to parse it
            if (!string.IsNullOrEmpty(patch.Target.File))
            {
                string dataType = _loader.InferDataTypeFromPath(patch.Target.File);
                if (!string.IsNullOrEmpty(dataType))
                {
                    return _loader.MapDataTypeToDictionary(dataType);
                }
            }

            // Otherwise use the inferred data type from patch location
            if (!string.IsNullOrEmpty(patch.InferredDataType))
            {
                return _loader.MapDataTypeToDictionary(patch.InferredDataType);
            }

            return null;
        }

        /// <summary>
        /// Get a dictionary from DataHandler using reflection.
        /// </summary>
        private object GetDataHandlerDictionary(string dictionaryName)
        {
            try
            {
                Type dataHandlerType = typeof(DataHandler);
                FieldInfo field = dataHandlerType.GetField(dictionaryName, BindingFlags.Public | BindingFlags.Static);
                
                if (field == null)
                {
                    LogError($"DataHandler.{dictionaryName} field not found");
                    return null;
                }

                return field.GetValue(null);
            }
            catch (Exception ex)
            {
                LogError($"Error accessing DataHandler.{dictionaryName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find all entries in a dictionary that match the target specification.
        /// </summary>
        private List<string> FindMatchingTargets(object dictionary, PatchTarget target)
        {
            var patterns = target.GetStrNamePatterns();
            
            // Try Dictionary<string, JsonData> first
            var jsonDict = dictionary as Dictionary<string, JsonData>;
            if (jsonDict != null)
            {
                return TargetMatcher.FindMatchingKeysInJsonDict(jsonDict, patterns);
            }

            // Handle typed dictionaries (Dictionary<string, T>)
            Type dictType = dictionary.GetType();
            
            if (dictType.IsGenericType && dictType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type[] genericArgs = dictType.GetGenericArguments();
                
                if (genericArgs[0] == typeof(string))
                {
                    // It's a Dictionary<string, T>, we can work with it
                    var keysProperty = dictType.GetProperty("Keys");
                    if (keysProperty == null)
                    {
                        LogError("Keys property not found on dictionary");
                        return new List<string>();
                    }
                    
                    var keys = keysProperty.GetValue(dictionary, null);
                    
                    if (keys == null)
                    {
                        LogError("Keys collection is null");
                        return new List<string>();
                    }
                    
                    var keysEnumerable = keys as IEnumerable;
                    if (keysEnumerable != null)
                    {
                        List<string> matchingKeys = new List<string>();
                        foreach (var key in keysEnumerable)
                        {
                            string keyStr = key.ToString();
                            foreach (string pattern in patterns)
                            {
                                if (TargetMatcher.Matches(keyStr, pattern))
                                {
                                    matchingKeys.Add(keyStr);
                                    break;
                                }
                            }
                        }
                        
                        return matchingKeys;
                    }
                    
                    LogError("Keys is not IEnumerable");
                    return new List<string>();
                }
            }

            // Fallback
            LogWarning($"Unsupported dictionary type: {dictType.Name}");
            return new List<string>();
        }

        /// <summary>
        /// Apply operations to a specific target in a dictionary.
        /// </summary>
        private bool ApplyPatchToTarget(object dictionary, string targetKey, List<PatchOperation> operations)
        {
            try
            {
                // Try Dictionary<string, JsonData> first
                var jsonDict = dictionary as Dictionary<string, JsonData>;
                if (jsonDict != null)
                {
                    if (!jsonDict.ContainsKey(targetKey))
                    {
                        LogError($"Target '{targetKey}' not found in dictionary");
                        return false;
                    }

                    JsonData targetObject = jsonDict[targetKey];
                    int successCount = _applier.ApplyOperations(targetObject, operations, targetKey);
                    _totalOperationsApplied += successCount;

                    return successCount > 0;
                }

                // Handle typed dictionaries (Dictionary<string, T>)
                Type dictType = dictionary.GetType();
                if (dictType.IsGenericType && dictType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] genericArgs = dictType.GetGenericArguments();
                    if (genericArgs[0] == typeof(string))
                    {
                        // Get the value from the dictionary
                        var indexer = dictType.GetProperty("Item");
                        if (indexer == null)
                        {
                            LogError($"Cannot access dictionary indexer for typed dictionary");
                            return false;
                        }

                        object typedValue = indexer.GetValue(dictionary, new object[] { targetKey });
                        if (typedValue == null)
                        {
                            LogError($"Target '{targetKey}' not found in typed dictionary");
                            return false;
                        }

                        // Apply patches directly using reflection on the typed object
                        int successCount = ApplyOperationsToTypedObject(typedValue, operations, targetKey);
                        _totalOperationsApplied += successCount;
                        return successCount > 0;
                    }
                }

                LogError($"Unsupported dictionary type: {dictType.Name}");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error applying patch to target '{targetKey}': {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Apply operations directly to a typed object using reflection.
        /// </summary>
        private int ApplyOperationsToTypedObject(object targetObject, List<PatchOperation> operations, string targetName)
        {
            int successCount = 0;
            Type objectType = targetObject.GetType();

            foreach (PatchOperation op in operations)
            {
                try
                {
                    // Get the field or property
                    FieldInfo field = objectType.GetField(op.Field, BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo property = null;
                    
                    if (field == null)
                    {
                        property = objectType.GetProperty(op.Field, BindingFlags.Public | BindingFlags.Instance);
                    }

                    if (field == null && property == null)
                    {
                        LogError($"Field or property '{op.Field}' not found on type {objectType.Name}");
                        continue;
                    }

                    // Get current value
                    object currentValue = field != null ? field.GetValue(targetObject) : property.GetValue(targetObject, null);

                    // Handle array operations
                    if (op.Op == PatchOperationType.Append && currentValue is Array)
                    {
                        Array arrayValue = (Array)currentValue;
                        Type elementType = arrayValue.GetType().GetElementType();
                        
                        // Convert the patch value to the element type
                        object newElement = ConvertValue((JsonData)op.Value, elementType);
                        
                        // Create new array with one more element
                        Array newArray = Array.CreateInstance(elementType, arrayValue.Length + 1);
                        Array.Copy(arrayValue, newArray, arrayValue.Length);
                        newArray.SetValue(newElement, arrayValue.Length);
                        
                        // Set the new array
                        if (field != null)
                            field.SetValue(targetObject, newArray);
                        else
                            property.SetValue(targetObject, newArray, null);
                        
                        successCount++;
                    }
                    else if (op.Op == PatchOperationType.Set)
                    {
                        Type targetType = field != null ? field.FieldType : property.PropertyType;
                        object newValue = ConvertValue((JsonData)op.Value, targetType);
                        
                        if (field != null)
                            field.SetValue(targetObject, newValue);
                        else
                            property.SetValue(targetObject, newValue, null);
                        
                        successCount++;
                    }
                    else
                    {
                        LogWarning($"Operation '{op.Op}' not yet supported for typed objects");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error applying operation to typed object: {ex.Message}");
                }
            }

            return successCount;
        }

        /// <summary>
        /// Convert a JsonData value to the target type.
        /// </summary>
        private object ConvertValue(JsonData value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle string values
            if (value.IsString)
            {
                string strValue = (string)value;
                if (targetType == typeof(string))
                    return strValue;
                
                // Try to convert to target type
                return Convert.ChangeType(strValue, targetType);
            }

            // Handle numeric values
            if (value.IsInt)
            {
                int intValue = (int)value;
                return Convert.ChangeType(intValue, targetType);
            }

            if (value.IsLong)
            {
                long longValue = (long)value;
                return Convert.ChangeType(longValue, targetType);
            }

            if (value.IsDouble)
            {
                double doubleValue = (double)value;
                return Convert.ChangeType(doubleValue, targetType);
            }

            if (value.IsBoolean)
            {
                bool boolValue = (bool)value;
                return Convert.ChangeType(boolValue, targetType);
            }

            // Default: try to serialize and deserialize
            string jsonString = JsonMapper.ToJson(value);
            MethodInfo toObjectMethod = typeof(JsonMapper).GetMethod("ToObject", new[] { typeof(string) });
            MethodInfo genericMethod = toObjectMethod.MakeGenericMethod(targetType);
            return genericMethod.Invoke(null, new object[] { jsonString });
        }

        /// <summary>
        /// Get statistics about patch application.
        /// </summary>
        public void LogStatistics()
        {
            LogInfo("=== JSON Patch System Statistics ===");
            LogInfo($"Total patches loaded: {_totalPatchesLoaded}");
            LogInfo($"Total patches applied: {_totalPatchesApplied}");
            LogInfo($"Total operations applied: {_totalOperationsApplied}");
        }

        private void LogInfo(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo($"[PatchManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning($"[PatchManager] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError($"[PatchManager] {message}");
            }
        }
    }
}

