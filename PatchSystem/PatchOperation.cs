using System;
using System.Collections.Generic;
using LitJson;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Represents the type of patch operation to perform.
    /// </summary>
    public enum PatchOperationType
    {
        /// <summary>Add value to end of array</summary>
        Append,
        
        /// <summary>Add value to beginning of array</summary>
        Prepend,
        
        /// <summary>Insert value at specific index in array</summary>
        Insert,
        
        /// <summary>Remove value from array or delete field</summary>
        Remove,
        
        /// <summary>Set or replace field value</summary>
        Set,
        
        /// <summary>Replace specific value in array with new value</summary>
        Replace
    }

    /// <summary>
    /// Represents a single patch operation to perform on a JSON object.
    /// </summary>
    public class PatchOperation
    {
        /// <summary>
        /// The type of operation to perform.
        /// </summary>
        public PatchOperationType Op { get; set; }
        
        /// <summary>
        /// The field path using dot notation (e.g., "aCOs", "stats.combat.damage").
        /// </summary>
        public string Field { get; set; }
        
        /// <summary>
        /// The value to set, add, or insert.
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// The old value to replace (for Replace operation).
        /// </summary>
        public object OldValue { get; set; }
        
        /// <summary>
        /// The index for Insert or Remove operations.
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// Parse operation type from string.
        /// </summary>
        public static PatchOperationType ParseOperationType(string opString)
        {
            if (string.IsNullOrEmpty(opString))
            {
                throw new ArgumentException("Operation type cannot be null or empty");
            }

            switch (opString.ToLowerInvariant())
            {
                case "append":
                    return PatchOperationType.Append;
                case "prepend":
                    return PatchOperationType.Prepend;
                case "insert":
                    return PatchOperationType.Insert;
                case "remove":
                    return PatchOperationType.Remove;
                case "set":
                    return PatchOperationType.Set;
                case "replace":
                    return PatchOperationType.Replace;
                default:
                    throw new ArgumentException($"Unknown operation type: {opString}");
            }
        }

        /// <summary>
        /// Create a PatchOperation from a JsonData object.
        /// </summary>
        public static PatchOperation FromJsonData(JsonData json)
        {
            if (json == null || !json.IsObject)
            {
                throw new ArgumentException("Invalid operation JSON");
            }

            var operation = new PatchOperation();

            // Parse operation type (required)
            if (!HasKey(json, "op"))
            {
                throw new ArgumentException("Operation missing required 'op' field");
            }
            operation.Op = ParseOperationType(json["op"].ToString());

            // Parse field path (required for most operations)
            if (HasKey(json, "field"))
            {
                operation.Field = json["field"].ToString();
            }

            // Parse value (optional, depends on operation)
            if (HasKey(json, "value"))
            {
                operation.Value = json["value"];
            }

            // Parse old_value (for replace operation)
            if (HasKey(json, "old_value"))
            {
                operation.OldValue = json["old_value"];
            }

            // Parse index (for insert/remove operations)
            if (HasKey(json, "index"))
            {
                if (json["index"].IsInt)
                {
                    operation.Index = (int)json["index"];
                }
            }

            return operation;
        }

        /// <summary>
        /// Validate that the operation has all required fields.
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;

            // Field is required for all operations except some Remove operations
            if (string.IsNullOrEmpty(Field))
            {
                error = "Field path is required";
                return false;
            }

            // Validate operation-specific requirements
            switch (Op)
            {
                case PatchOperationType.Append:
                case PatchOperationType.Prepend:
                    if (Value == null)
                    {
                        error = $"{Op} operation requires 'value' field";
                        return false;
                    }
                    break;

                case PatchOperationType.Insert:
                    if (Value == null)
                    {
                        error = "Insert operation requires 'value' field";
                        return false;
                    }
                    if (!Index.HasValue)
                    {
                        error = "Insert operation requires 'index' field";
                        return false;
                    }
                    break;

                case PatchOperationType.Set:
                    if (Value == null)
                    {
                        error = "Set operation requires 'value' field";
                        return false;
                    }
                    break;

                case PatchOperationType.Replace:
                    if (Value == null || OldValue == null)
                    {
                        error = "Replace operation requires both 'value' and 'old_value' fields";
                        return false;
                    }
                    break;

                case PatchOperationType.Remove:
                    // Remove can work with value, index, or neither (to remove entire field)
                    break;
            }

            return true;
        }

        /// <summary>
        /// Check if a JsonData object has a specific key.
        /// </summary>
        private static bool HasKey(JsonData jsonData, string key)
        {
            if (jsonData == null || !jsonData.IsObject)
            {
                return false;
            }

            foreach (string existingKey in jsonData.Keys)
            {
                if (existingKey == key)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

