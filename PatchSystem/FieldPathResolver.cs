using System;
using System.Collections.Generic;
using System.Text;
using LitJson;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Utility for navigating and modifying nested JSON structures using dot notation.
    /// Handles field paths like "stats.combat.damage" and supports escaped dots.
    /// </summary>
    public static class FieldPathResolver
    {
        /// <summary>
        /// Parse a field path into individual segments, handling escaped dots.
        /// Example: "stats.combat.damage" -> ["stats", "combat", "damage"]
        /// Example: "my\\.field.other" -> ["my.field", "other"]
        /// </summary>
        public static List<string> ParsePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new List<string>();
            }

            var segments = new List<string>();
            var currentSegment = new StringBuilder();
            bool escaped = false;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (escaped)
                {
                    // Previous character was backslash, add current char literally
                    currentSegment.Append(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    // Start escape sequence
                    escaped = true;
                }
                else if (c == '.')
                {
                    // Unescaped dot - end current segment
                    if (currentSegment.Length > 0)
                    {
                        segments.Add(currentSegment.ToString());
                        currentSegment.Length = 0;  // .NET 3.5 compatible
                    }
                }
                else
                {
                    currentSegment.Append(c);
                }
            }

            // Add final segment
            if (currentSegment.Length > 0)
            {
                segments.Add(currentSegment.ToString());
            }

            return segments;
        }

        /// <summary>
        /// Get a nested field value from a JsonData object using dot notation.
        /// Returns null if the path doesn't exist.
        /// </summary>
        public static JsonData GetNestedField(JsonData root, string fieldPath)
        {
            if (root == null || string.IsNullOrEmpty(fieldPath))
            {
                return null;
            }

            var segments = ParsePath(fieldPath);
            if (segments.Count == 0)
            {
                return null;
            }

            JsonData current = root;
            foreach (string segment in segments)
            {
                if (current == null || !current.IsObject)
                {
                    return null;
                }

                if (!HasKey(current, segment))
                {
                    return null;
                }

                current = current[segment];
            }

            return current;
        }

        /// <summary>
        /// Set a nested field value, creating intermediate objects if needed.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public static bool SetNestedField(JsonData root, string fieldPath, object value, bool createIfMissing = true)
        {
            if (root == null || !root.IsObject || string.IsNullOrEmpty(fieldPath))
            {
                return false;
            }

            var segments = ParsePath(fieldPath);
            if (segments.Count == 0)
            {
                return false;
            }

            // Navigate to parent of final field
            JsonData current = root;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                string segment = segments[i];

                if (!HasKey(current, segment))
                {
                    if (!createIfMissing)
                    {
                        return false;
                    }

                    // Create intermediate object
                    current[segment] = new JsonData();
                    current[segment].SetJsonType(JsonType.Object);
                }

                current = current[segment];

                if (!current.IsObject)
                {
                    // Path blocked by non-object value
                    return false;
                }
            }

            // Set the final field
            string finalSegment = segments[segments.Count - 1];
            current[finalSegment] = ConvertToJsonData(value);

            return true;
        }

        /// <summary>
        /// Remove a nested field from a JsonData object.
        /// Returns true if the field existed and was removed, false otherwise.
        /// </summary>
        public static bool RemoveNestedField(JsonData root, string fieldPath)
        {
            if (root == null || !root.IsObject || string.IsNullOrEmpty(fieldPath))
            {
                return false;
            }

            var segments = ParsePath(fieldPath);
            if (segments.Count == 0)
            {
                return false;
            }

            // Navigate to parent of final field
            JsonData current = root;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                string segment = segments[i];

                if (!HasKey(current, segment))
                {
                    return false;
                }

                current = current[segment];

                if (!current.IsObject)
                {
                    return false;
                }
            }

            // Remove the final field
            string finalSegment = segments[segments.Count - 1];
            if (!HasKey(current, finalSegment))
            {
                return false;
            }

            // LitJson doesn't have a direct Remove method, but we can use the IDictionary interface
            if (current.IsObject)
            {
                var dict = current as System.Collections.IDictionary;
                if (dict != null)
                {
                    dict.Remove(finalSegment);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the parent object and field name for a given path.
        /// Useful for operations that need to modify the parent.
        /// </summary>
        public static bool GetParentAndField(JsonData root, string fieldPath, out JsonData parent, out string fieldName)
        {
            parent = null;
            fieldName = null;

            if (root == null || !root.IsObject || string.IsNullOrEmpty(fieldPath))
            {
                return false;
            }

            var segments = ParsePath(fieldPath);
            if (segments.Count == 0)
            {
                return false;
            }

            // If only one segment, parent is root
            if (segments.Count == 1)
            {
                parent = root;
                fieldName = segments[0];
                return true;
            }

            // Navigate to parent
            JsonData current = root;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                string segment = segments[i];

                if (!HasKey(current, segment))
                {
                    return false;
                }

                current = current[segment];

                if (!current.IsObject)
                {
                    return false;
                }
            }

            parent = current;
            fieldName = segments[segments.Count - 1];
            return true;
        }

        /// <summary>
        /// Check if a JsonData object has a specific key.
        /// LitJson in .NET 3.5 doesn't have ContainsKey, so we check the Keys collection.
        /// </summary>
        private static bool HasKey(JsonData jsonData, string key)
        {
            if (jsonData == null || !jsonData.IsObject)
            {
                return false;
            }

            // JsonData.Keys returns ICollection<string>
            foreach (string existingKey in jsonData.Keys)
            {
                if (existingKey == key)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert a .NET object to JsonData for storage.
        /// </summary>
        private static JsonData ConvertToJsonData(object value)
        {
            if (value == null)
            {
                return null;
            }

            // If already JsonData, return as-is
            if (value is JsonData)
            {
                return (JsonData)value;
            }

            // Convert to JSON string and parse back
            // This handles all types correctly
            string jsonString = JsonMapper.ToJson(value);
            return JsonMapper.ToObject(jsonString);
        }

        /// <summary>
        /// Check if a field path exists in a JsonData object.
        /// </summary>
        public static bool FieldExists(JsonData root, string fieldPath)
        {
            return GetNestedField(root, fieldPath) != null;
        }

        /// <summary>
        /// Get the type of a nested field.
        /// </summary>
        public static JsonType? GetFieldType(JsonData root, string fieldPath)
        {
            JsonData field = GetNestedField(root, fieldPath);
            if (field == null)
            {
                return null;
            }

            return field.GetJsonType();
        }
    }
}

