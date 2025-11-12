using System;
using System.Collections.Generic;
using LitJson;

namespace Ostranauts.Bit.PatchSystem
{
    /// <summary>
    /// Represents the target specification for a patch file.
    /// Defines which JSON entries should be modified.
    /// </summary>
    public class PatchTarget
    {
        /// <summary>
        /// Optional: Relative path to the JSON file to patch (e.g., "data/interactions/file.json").
        /// If omitted, inferred from the patch file's location.
        /// </summary>
        public string File { get; set; }
        
        /// <summary>
        /// Required: The strName field value to match, supports wildcards.
        /// Can be a string like "InteractionName", "Interaction*", or "*Battery".
        /// </summary>
        public string StrName { get; set; }
        
        /// <summary>
        /// Optional: Support for array of strName patterns to match multiple entries.
        /// </summary>
        public List<string> StrNames { get; set; }
        
        /// <summary>
        /// Optional: Additional fields to match for more complex targeting.
        /// </summary>
        public Dictionary<string, object> AdditionalMatches { get; set; }

        /// <summary>
        /// Get all strName patterns (handles both single and array syntax).
        /// </summary>
        public List<string> GetStrNamePatterns()
        {
            var patterns = new List<string>();
            
            if (!string.IsNullOrEmpty(StrName))
            {
                patterns.Add(StrName);
            }
            
            if (StrNames != null && StrNames.Count > 0)
            {
                patterns.AddRange(StrNames);
            }
            
            return patterns;
        }

        /// <summary>
        /// Create a PatchTarget from JsonData.
        /// </summary>
        public static PatchTarget FromJsonData(JsonData json)
        {
            if (json == null || !json.IsObject)
            {
                throw new ArgumentException("Invalid target JSON");
            }

            var target = new PatchTarget();

            // Parse file path (optional)
            if (HasKey(json, "file"))
            {
                target.File = json["file"].ToString();
            }

            // Parse strName (can be string or array)
            if (HasKey(json, "strName"))
            {
                if (json["strName"].IsString)
                {
                    target.StrName = json["strName"].ToString();
                }
                else if (json["strName"].IsArray)
                {
                    target.StrNames = new List<string>();
                    foreach (JsonData item in json["strName"])
                    {
                        target.StrNames.Add(item.ToString());
                    }
                }
            }

            // Parse strNames array (alternative to strName)
            if (HasKey(json, "strNames") && json["strNames"].IsArray)
            {
                if (target.StrNames == null)
                {
                    target.StrNames = new List<string>();
                }
                foreach (JsonData item in json["strNames"])
                {
                    target.StrNames.Add(item.ToString());
                }
            }

            // Parse additional match fields
            target.AdditionalMatches = new Dictionary<string, object>();
            foreach (string key in json.Keys)
            {
                if (key != "file" && key != "strName" && key != "strNames")
                {
                    target.AdditionalMatches[key] = json[key];
                }
            }

            return target;
        }

        /// <summary>
        /// Validate that the target has required fields.
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;

            var patterns = GetStrNamePatterns();
            if (patterns.Count == 0)
            {
                error = "Target must specify at least one strName pattern";
                return false;
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

    /// <summary>
    /// Represents a complete patch file with target and operations.
    /// </summary>
    public class JsonPatchFile
    {
        /// <summary>
        /// The target specification defining which JSON entries to patch.
        /// </summary>
        public PatchTarget Target { get; set; }
        
        /// <summary>
        /// The list of operations to perform on matching entries.
        /// </summary>
        public List<PatchOperation> Operations { get; set; }

        /// <summary>
        /// The file path this patch was loaded from (for error reporting).
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// The inferred data type from the patch file location (e.g., "interactions", "pledges").
        /// </summary>
        public string InferredDataType { get; set; }

        /// <summary>
        /// Parse a patch file from JSON string.
        /// Supports both single patch objects and arrays of patches.
        /// </summary>
        public static List<JsonPatchFile> FromJson(string jsonString, string sourceFilePath = null)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentException("JSON string cannot be null or empty");
            }

            JsonData json;
            try
            {
                json = JsonMapper.ToObject(jsonString);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to parse JSON: {ex.Message}", ex);
            }

            var patches = new List<JsonPatchFile>();

            // Check if root is an array
            if (json.IsArray)
            {
                // Multiple patches in one file
                for (int i = 0; i < json.Count; i++)
                {
                    var patch = FromJsonData(json[i], sourceFilePath);
                    patches.Add(patch);
                }
            }
            else if (json.IsObject)
            {
                // Single patch
                var patch = FromJsonData(json, sourceFilePath);
                patches.Add(patch);
            }
            else
            {
                throw new ArgumentException("Patch file must be either an object or an array of objects");
            }

            return patches;
        }

        /// <summary>
        /// Create a JsonPatchFile from JsonData.
        /// </summary>
        public static JsonPatchFile FromJsonData(JsonData json, string sourceFilePath = null)
        {
            if (json == null || !json.IsObject)
            {
                throw new ArgumentException("Invalid patch file JSON");
            }

            var patchFile = new JsonPatchFile
            {
                SourceFilePath = sourceFilePath
            };

            // Parse target (required)
            if (!HasKey(json, "target"))
            {
                throw new ArgumentException("Patch file missing required 'target' field");
            }
            patchFile.Target = PatchTarget.FromJsonData(json["target"]);

            // Parse operations (required)
            if (!HasKey(json, "operations"))
            {
                throw new ArgumentException("Patch file missing required 'operations' field");
            }

            if (!json["operations"].IsArray)
            {
                throw new ArgumentException("'operations' field must be an array");
            }

            patchFile.Operations = new List<PatchOperation>();
            foreach (JsonData opJson in json["operations"])
            {
                patchFile.Operations.Add(PatchOperation.FromJsonData(opJson));
            }

            if (patchFile.Operations.Count == 0)
            {
                throw new ArgumentException("Patch file must have at least one operation");
            }

            return patchFile;
        }

        /// <summary>
        /// Validate the entire patch file.
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (Target == null)
            {
                errors.Add("Patch file has no target");
                return false;
            }

            string targetError;
            if (!Target.Validate(out targetError))
            {
                errors.Add($"Target validation failed: {targetError}");
            }

            if (Operations == null || Operations.Count == 0)
            {
                errors.Add("Patch file has no operations");
                return false;
            }

            for (int i = 0; i < Operations.Count; i++)
            {
                string opError;
                if (!Operations[i].Validate(out opError))
                {
                    errors.Add($"Operation {i} validation failed: {opError}");
                }
            }

            return errors.Count == 0;
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

