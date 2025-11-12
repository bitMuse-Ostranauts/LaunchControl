using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LitJson;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Handles matching JSON entries based on wildcards and patterns.
    /// Supports patterns like "Interaction*", "*Battery", "Item*Tool", and "*".
    /// </summary>
    public static class TargetMatcher
    {
        /// <summary>
        /// Convert a wildcard pattern to a regular expression.
        /// Example: "Interaction*" -> "^Interaction.*$"
        /// Example: "*Battery" -> "^.*Battery$"
        /// Example: "*" -> "^.*$"
        /// </summary>
        public static string WildcardToRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return "^$";
            }

            // Escape regex special characters except *
            string escaped = Regex.Escape(pattern);
            
            // Replace escaped \* with .* for wildcard matching
            escaped = escaped.Replace("\\*", ".*");
            
            // Add anchors for exact matching
            return "^" + escaped + "$";
        }

        /// <summary>
        /// Check if a value matches a wildcard pattern.
        /// </summary>
        public static bool Matches(string value, string pattern)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            // Exact match (fast path)
            if (pattern.IndexOf('*') == -1)
            {
                return value == pattern;
            }

            // Wildcard match
            string regexPattern = WildcardToRegex(pattern);
            return Regex.IsMatch(value, regexPattern);
        }

        /// <summary>
        /// Find all entries in a dictionary that match a wildcard pattern against their strName field.
        /// </summary>
        public static List<string> FindMatchingKeys<T>(Dictionary<string, T> dictionary, string pattern)
        {
            var matchingKeys = new List<string>();

            if (dictionary == null || dictionary.Count == 0)
            {
                return matchingKeys;
            }

            foreach (var kvp in dictionary)
            {
                string key = kvp.Key;
                
                // For dictionaries keyed by strName, the key itself is what we match against
                if (Matches(key, pattern))
                {
                    matchingKeys.Add(key);
                }
            }

            return matchingKeys;
        }

        /// <summary>
        /// Find all entries matching any of the provided patterns.
        /// </summary>
        public static List<string> FindMatchingKeys<T>(Dictionary<string, T> dictionary, List<string> patterns)
        {
            var matchingKeys = new HashSet<string>(); // Use HashSet to avoid duplicates

            if (dictionary == null || dictionary.Count == 0 || patterns == null || patterns.Count == 0)
            {
                return new List<string>();
            }

            foreach (string pattern in patterns)
            {
                var matches = FindMatchingKeys(dictionary, pattern);
                foreach (string match in matches)
                {
                    matchingKeys.Add(match);
                }
            }

            return new List<string>(matchingKeys);
        }

        /// <summary>
        /// Find entries in a JsonData dictionary that match a pattern.
        /// This works with the game's DataHandler dictionaries which use JsonData as values.
        /// </summary>
        public static List<string> FindMatchingKeysInJsonDict(Dictionary<string, JsonData> dictionary, string pattern)
        {
            var matchingKeys = new List<string>();

            if (dictionary == null || dictionary.Count == 0)
            {
                return matchingKeys;
            }

            foreach (var kvp in dictionary)
            {
                string key = kvp.Key;
                
                // Match against the dictionary key
                if (Matches(key, pattern))
                {
                    matchingKeys.Add(key);
                    continue;
                }

                // Also try to match against strName field in the object if it exists
                JsonData obj = kvp.Value;
                if (obj != null && obj.IsObject && HasKey(obj, "strName"))
                {
                    string strName = obj["strName"].ToString();
                    if (Matches(strName, pattern))
                    {
                        matchingKeys.Add(key);
                    }
                }
            }

            return matchingKeys;
        }

        /// <summary>
        /// Find entries in a JsonData dictionary that match multiple patterns.
        /// </summary>
        public static List<string> FindMatchingKeysInJsonDict(Dictionary<string, JsonData> dictionary, List<string> patterns)
        {
            var matchingKeys = new HashSet<string>(); // Use HashSet to avoid duplicates

            if (dictionary == null || dictionary.Count == 0 || patterns == null || patterns.Count == 0)
            {
                return new List<string>();
            }

            foreach (string pattern in patterns)
            {
                var matches = FindMatchingKeysInJsonDict(dictionary, pattern);
                foreach (string match in matches)
                {
                    matchingKeys.Add(match);
                }
            }

            return new List<string>(matchingKeys);
        }

        /// <summary>
        /// Find all entries in a generic dictionary that match the target specification.
        /// This uses reflection to access the strName field from typed objects.
        /// </summary>
        public static List<string> FindMatchingKeysGeneric(object dictionary, List<string> patterns)
        {
            if (dictionary == null || patterns == null || patterns.Count == 0)
            {
                return new List<string>();
            }

            // Try to cast to Dictionary<string, JsonData> first
            var jsonDict = dictionary as Dictionary<string, JsonData>;
            if (jsonDict != null)
            {
                return FindMatchingKeysInJsonDict(jsonDict, patterns);
            }

            // Otherwise use reflection to iterate the dictionary
            var matchingKeys = new HashSet<string>();
            var dictType = dictionary.GetType();
            
            // Check if it's a generic dictionary
            if (dictType.IsGenericType && dictType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keysProperty = dictType.GetProperty("Keys");
                if (keysProperty != null)
                {
                    var keys = keysProperty.GetValue(dictionary, null) as System.Collections.IEnumerable;
                    if (keys != null)
                    {
                        foreach (var key in keys)
                        {
                            string keyStr = key.ToString();
                            
                            // Check if key matches any pattern
                            foreach (string pattern in patterns)
                            {
                                if (Matches(keyStr, pattern))
                                {
                                    matchingKeys.Add(keyStr);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return new List<string>(matchingKeys);
        }

        /// <summary>
        /// Check if any pattern contains a wildcard.
        /// </summary>
        public static bool HasWildcard(string pattern)
        {
            return !string.IsNullOrEmpty(pattern) && pattern.Contains("*");
        }

        /// <summary>
        /// Check if any of the patterns contain wildcards.
        /// </summary>
        public static bool HasWildcards(List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return false;
            }

            foreach (string pattern in patterns)
            {
                if (HasWildcard(pattern))
                {
                    return true;
                }
            }

            return false;
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

