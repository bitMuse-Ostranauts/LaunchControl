using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Applies patch operations to JSON objects.
    /// Handles all operation types: append, prepend, insert, remove, set, replace.
    /// </summary>
    public class PatchApplier
    {
        private readonly BepInEx.Logging.ManualLogSource _logger;

        public PatchApplier(BepInEx.Logging.ManualLogSource logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Apply a single operation to a JSON object.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool ApplyOperation(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            if (jsonObject == null || !jsonObject.IsObject)
            {
                LogError($"Cannot apply operation to non-object JSON for target '{targetName}'");
                return false;
            }

            try
            {
                switch (operation.Op)
                {
                    case PatchOperationType.Append:
                        return ApplyAppend(jsonObject, operation, targetName);
                    
                    case PatchOperationType.Prepend:
                        return ApplyPrepend(jsonObject, operation, targetName);
                    
                    case PatchOperationType.Insert:
                        return ApplyInsert(jsonObject, operation, targetName);
                    
                    case PatchOperationType.Remove:
                        return ApplyRemove(jsonObject, operation, targetName);
                    
                    case PatchOperationType.Set:
                        return ApplySet(jsonObject, operation, targetName);
                    
                    case PatchOperationType.Replace:
                        return ApplyReplace(jsonObject, operation, targetName);
                    
                    default:
                        LogError($"Unknown operation type: {operation.Op}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception applying {operation.Op} operation to '{targetName}' field '{operation.Field}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply multiple operations to a JSON object in sequence.
        /// Returns the number of successful operations.
        /// </summary>
        public int ApplyOperations(JsonData jsonObject, List<PatchOperation> operations, string targetName)
        {
            int successCount = 0;

            for (int i = 0; i < operations.Count; i++)
            {
                PatchOperation op = operations[i];
                
                if (ApplyOperation(jsonObject, op, targetName))
                {
                    successCount++;
                }
                else
                {
                    LogWarning($"Operation {i} ({op.Op}) failed for target '{targetName}'");
                }
            }

            return successCount;
        }

        #region Operation Implementations

        private bool ApplyAppend(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            JsonData field = FieldPathResolver.GetNestedField(jsonObject, operation.Field);
            
            if (field == null)
            {
                LogError($"Field '{operation.Field}' not found in target '{targetName}' for append operation");
                return false;
            }

            if (!field.IsArray)
            {
                LogError($"Field '{operation.Field}' in target '{targetName}' is not an array (type: {field.GetJsonType()})");
                return false;
            }

            // Convert value to JsonData
            JsonData valueToAdd = ConvertToJsonData(operation.Value);
            field.Add(valueToAdd);
            
            LogInfo($"Appended value to '{operation.Field}' in target '{targetName}'");
            return true;
        }

        private bool ApplyPrepend(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            JsonData field = FieldPathResolver.GetNestedField(jsonObject, operation.Field);
            
            if (field == null)
            {
                LogError($"Field '{operation.Field}' not found in target '{targetName}' for prepend operation");
                return false;
            }

            if (!field.IsArray)
            {
                LogError($"Field '{operation.Field}' in target '{targetName}' is not an array (type: {field.GetJsonType()})");
                return false;
            }

            // Convert value to JsonData
            JsonData valueToAdd = ConvertToJsonData(operation.Value);
            
            // LitJson doesn't have Insert for arrays, so we need to rebuild the array
            var newArray = new JsonData();
            newArray.SetJsonType(JsonType.Array);
            newArray.Add(valueToAdd);
            
            for (int i = 0; i < field.Count; i++)
            {
                newArray.Add(field[i]);
            }

            // Replace the field with the new array
            if (!FieldPathResolver.SetNestedField(jsonObject, operation.Field, newArray, false))
            {
                LogError($"Failed to set field '{operation.Field}' after prepend");
                return false;
            }
            
            LogInfo($"Prepended value to '{operation.Field}' in target '{targetName}'");
            return true;
        }

        private bool ApplyInsert(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            if (!operation.Index.HasValue)
            {
                LogError($"Insert operation requires index for field '{operation.Field}' in target '{targetName}'");
                return false;
            }

            JsonData field = FieldPathResolver.GetNestedField(jsonObject, operation.Field);
            
            if (field == null)
            {
                LogError($"Field '{operation.Field}' not found in target '{targetName}' for insert operation");
                return false;
            }

            if (!field.IsArray)
            {
                LogError($"Field '{operation.Field}' in target '{targetName}' is not an array (type: {field.GetJsonType()})");
                return false;
            }

            int index = operation.Index.Value;
            if (index < 0 || index > field.Count)
            {
                LogError($"Insert index {index} out of range for field '{operation.Field}' (length: {field.Count}) in target '{targetName}'");
                return false;
            }

            // Convert value to JsonData
            JsonData valueToAdd = ConvertToJsonData(operation.Value);
            
            // Rebuild array with inserted value
            var newArray = new JsonData();
            newArray.SetJsonType(JsonType.Array);
            
            for (int i = 0; i < field.Count; i++)
            {
                if (i == index)
                {
                    newArray.Add(valueToAdd);
                }
                newArray.Add(field[i]);
            }
            
            // Handle insertion at end
            if (index == field.Count)
            {
                newArray.Add(valueToAdd);
            }

            // Replace the field with the new array
            if (!FieldPathResolver.SetNestedField(jsonObject, operation.Field, newArray, false))
            {
                LogError($"Failed to set field '{operation.Field}' after insert");
                return false;
            }
            
            LogInfo($"Inserted value at index {index} in '{operation.Field}' in target '{targetName}'");
            return true;
        }

        private bool ApplyRemove(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            JsonData field = FieldPathResolver.GetNestedField(jsonObject, operation.Field);
            
            if (field == null)
            {
                // If removing entire field and it doesn't exist, that's okay
                if (operation.Value == null && !operation.Index.HasValue)
                {
                    LogInfo($"Field '{operation.Field}' doesn't exist in target '{targetName}', nothing to remove");
                    return true;
                }
                
                LogError($"Field '{operation.Field}' not found in target '{targetName}' for remove operation");
                return false;
            }

            // Remove entire field
            if (operation.Value == null && !operation.Index.HasValue)
            {
                if (FieldPathResolver.RemoveNestedField(jsonObject, operation.Field))
                {
                    LogInfo($"Removed field '{operation.Field}' from target '{targetName}'");
                    return true;
                }
                else
                {
                    LogError($"Failed to remove field '{operation.Field}' from target '{targetName}'");
                    return false;
                }
            }

            // Remove from array
            if (!field.IsArray)
            {
                LogError($"Field '{operation.Field}' in target '{targetName}' is not an array for remove operation");
                return false;
            }

            // Remove by index
            if (operation.Index.HasValue)
            {
                int index = operation.Index.Value;
                if (index < 0 || index >= field.Count)
                {
                    LogError($"Remove index {index} out of range for field '{operation.Field}' (length: {field.Count}) in target '{targetName}'");
                    return false;
                }

                // Rebuild array without the element at index
                var newArray = new JsonData();
                newArray.SetJsonType(JsonType.Array);
                
                for (int i = 0; i < field.Count; i++)
                {
                    if (i != index)
                    {
                        newArray.Add(field[i]);
                    }
                }

                if (!FieldPathResolver.SetNestedField(jsonObject, operation.Field, newArray, false))
                {
                    LogError($"Failed to set field '{operation.Field}' after remove by index");
                    return false;
                }
                
                LogInfo($"Removed element at index {index} from '{operation.Field}' in target '{targetName}'");
                return true;
            }

            // Remove by value
            if (operation.Value != null)
            {
                string valueToRemove = ConvertToString(operation.Value);
                bool found = false;

                var newArray = new JsonData();
                newArray.SetJsonType(JsonType.Array);
                
                for (int i = 0; i < field.Count; i++)
                {
                    string currentValue = ConvertToString(field[i]);
                    if (currentValue == valueToRemove && !found)
                    {
                        // Skip this element (remove it)
                        found = true;
                    }
                    else
                    {
                        newArray.Add(field[i]);
                    }
                }

                if (!found)
                {
                    LogWarning($"Value '{valueToRemove}' not found in field '{operation.Field}' of target '{targetName}'");
                    return false;
                }

                if (!FieldPathResolver.SetNestedField(jsonObject, operation.Field, newArray, false))
                {
                    LogError($"Failed to set field '{operation.Field}' after remove by value");
                    return false;
                }
                
                LogInfo($"Removed value '{valueToRemove}' from '{operation.Field}' in target '{targetName}'");
                return true;
            }

            LogError($"Remove operation must specify either value or index");
            return false;
        }

        private bool ApplySet(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            if (FieldPathResolver.SetNestedField(jsonObject, operation.Field, operation.Value, createIfMissing: true))
            {
                LogInfo($"Set field '{operation.Field}' in target '{targetName}'");
                return true;
            }
            else
            {
                LogError($"Failed to set field '{operation.Field}' in target '{targetName}'");
                return false;
            }
        }

        private bool ApplyReplace(JsonData jsonObject, PatchOperation operation, string targetName)
        {
            JsonData field = FieldPathResolver.GetNestedField(jsonObject, operation.Field);
            
            if (field == null)
            {
                LogError($"Field '{operation.Field}' not found in target '{targetName}' for replace operation");
                return false;
            }

            if (!field.IsArray)
            {
                LogError($"Field '{operation.Field}' in target '{targetName}' is not an array for replace operation");
                return false;
            }

            string oldValue = ConvertToString(operation.OldValue);
            bool found = false;

            var newArray = new JsonData();
            newArray.SetJsonType(JsonType.Array);
            
            for (int i = 0; i < field.Count; i++)
            {
                string currentValue = ConvertToString(field[i]);
                if (currentValue == oldValue && !found)
                {
                    // Replace with new value
                    newArray.Add(ConvertToJsonData(operation.Value));
                    found = true;
                }
                else
                {
                    newArray.Add(field[i]);
                }
            }

            if (!found)
            {
                LogWarning($"Old value '{oldValue}' not found in field '{operation.Field}' of target '{targetName}'");
                return false;
            }

            if (!FieldPathResolver.SetNestedField(jsonObject, operation.Field, newArray, false))
            {
                LogError($"Failed to set field '{operation.Field}' after replace");
                return false;
            }
            
            LogInfo($"Replaced '{oldValue}' with new value in '{operation.Field}' in target '{targetName}'");
            return true;
        }

        #endregion

        #region Utility Methods

        private JsonData ConvertToJsonData(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JsonData)
            {
                return (JsonData)value;
            }

            // Convert to JSON string and parse back
            string jsonString = JsonMapper.ToJson(value);
            return JsonMapper.ToObject(jsonString);
        }

        private string ConvertToString(object value)
        {
            if (value == null)
            {
                return "";
            }

            if (value is JsonData)
            {
                JsonData json = (JsonData)value;
                if (json.IsString)
                {
                    return json.ToString();
                }
                // For non-string JsonData, convert to JSON string
                return JsonMapper.ToJson(json);
            }

            return value.ToString();
        }

        private void LogInfo(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo($"[PatchApplier] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning($"[PatchApplier] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError($"[PatchApplier] {message}");
            }
        }

        #endregion
    }
}

