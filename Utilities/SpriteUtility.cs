using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Bit
{
    /// <summary>
    /// Utility class for loading and caching sprites from Unity prefabs and resources
    /// </summary>
    public static class SpriteUtility
    {
        private static Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();
        private static Dictionary<string, GameObject> _cachedPrefabs = new Dictionary<string, GameObject>();

        #region Public API

        /// <summary>
        /// Find a sprite from a prefab using keywords and optional path hint
        /// </summary>
        /// <param name="prefabName">Name of the prefab (e.g., "ValueModule", "ToggleMoreModule")</param>
        /// <param name="keywords">Keywords to search for in sprite name (all must match)</param>
        /// <param name="pathHint">Optional path hint to narrow search (e.g., "Image", "Button/Image")</param>
        /// <param name="useCache">Whether to use cached sprites (default: true)</param>
        /// <returns>The sprite, or null if not found</returns>
        /// <example>
        /// // Find gradient bordered sprite in ValueModule prefab
        /// Sprite chip = SpriteUtility.FindSprite("ValueModule", new[] { "gradient", "bordered" }, "Image");
        /// 
        /// // Find button sprite in ToggleMoreModule
        /// Sprite button = SpriteUtility.FindSprite("ToggleMoreModule", new[] { "crewbar" }, "Button");
        /// 
        /// // Search without path hint
        /// Sprite arrow = SpriteUtility.FindSprite("ToggleMoreModule", new[] { "arrow" });
        /// </example>
        public static Sprite FindSprite(string prefabName, string[] keywords, string pathHint = null, bool useCache = true)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                LaunchControlPlugin.Logger.LogWarning("FindSprite: prefabName is null or empty");
                return null;
            }

            if (keywords == null || keywords.Length == 0)
            {
                LaunchControlPlugin.Logger.LogWarning("FindSprite: keywords is null or empty");
                return null;
            }

            string keywordString = string.Join(",", keywords);
            string cacheKey = $"{prefabName}::{keywordString}::{pathHint ?? "any"}";

            // Check cache
            if (useCache && _cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            // Load the prefab
            GameObject prefab = LoadPrefab(prefabName, useCache);
            if (prefab == null)
            {
                LaunchControlPlugin.Logger.LogWarning($"FindSprite: Could not load prefab '{prefabName}'");
                return null;
            }

            // Determine search root
            GameObject searchRoot = prefab;
            if (!string.IsNullOrEmpty(pathHint))
            {
                GameObject hinted = FindChildByPath(prefab, pathHint);
                if (hinted != null)
                {
                    searchRoot = hinted;
                }
                else
                {
                    LaunchControlPlugin.Logger.LogWarning($"FindSprite: Path hint '{pathHint}' not found in prefab '{prefabName}', searching entire prefab");
                }
            }

            // Search for sprite matching keywords
            Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
            
            foreach (Image img in images)
            {
                if (img == null || img.sprite == null)
                    continue;

                string spriteName = img.sprite.name.ToLower();
                
                // Check if all keywords match
                bool allMatch = true;
                foreach (string keyword in keywords)
                {
                    if (!spriteName.Contains(keyword.ToLower().Trim()))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    if (useCache)
                    {
                        _cachedSprites[cacheKey] = img.sprite;
                    }
                    LaunchControlPlugin.Logger.LogInfo($"FindSprite: Found sprite '{img.sprite.name}' in prefab '{prefabName}' at path '{GetGameObjectPath(img.gameObject, prefab)}'");
                    return img.sprite;
                }
            }

            LaunchControlPlugin.Logger.LogWarning($"FindSprite: No sprite matching keywords '{keywordString}' found in prefab '{prefabName}'");
            return null;
        }

        /// <summary>
        /// Get a sprite from a prefab's Image component at a specific path
        /// </summary>
        /// <param name="prefabPath">Path to the prefab in Resources (e.g., "Prefabs/UI/Button")</param>
        /// <param name="childPath">Optional path to child GameObject (e.g., "Background/Icon")</param>
        /// <param name="useCache">Whether to use cached sprites (default: true)</param>
        /// <returns>The sprite, or null if not found</returns>
        /// <example>
        /// // Get sprite from: GUICrewStatus/bgBorder/Image
        /// Sprite border = SpriteUtility.GetSpriteFromPrefab("GUICrewStatus", "bgBorder");
        /// 
        /// // Get sprite from: pnlFillBars/physio01/bmpFill/Image
        /// Sprite fill = SpriteUtility.GetSpriteFromPrefab("pnlFillBars", "physio01/bmpFill");
        /// </example>
        public static Sprite GetSpriteFromPrefab(string prefabPath, string childPath = null, bool useCache = true)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                LaunchControlPlugin.Logger.LogWarning("GetSpriteFromPrefab: prefabPath is null or empty");
                return null;
            }

            string cacheKey = $"{prefabPath}::{childPath ?? "root"}";

            // Check cache
            if (useCache && _cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            // Load or get cached prefab
            GameObject prefab = LoadPrefab(prefabPath, useCache);
            if (prefab == null)
            {
                LaunchControlPlugin.Logger.LogWarning($"GetSpriteFromPrefab: Could not load prefab '{prefabPath}'");
                return null;
            }

            // Navigate to target GameObject
            GameObject target = string.IsNullOrEmpty(childPath) 
                ? prefab 
                : FindChildByPath(prefab, childPath);

            if (target == null)
            {
                LaunchControlPlugin.Logger.LogWarning($"GetSpriteFromPrefab: Could not find child at path '{childPath}' in prefab '{prefabPath}'");
                return null;
            }

            // Get the sprite from Image component
            Image img = target.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                if (useCache)
                {
                    _cachedSprites[cacheKey] = img.sprite;
                }
                LaunchControlPlugin.Logger.LogInfo($"GetSpriteFromPrefab: Found sprite '{img.sprite.name}' at '{prefabPath}/{childPath}'");
                return img.sprite;
            }

            LaunchControlPlugin.Logger.LogWarning($"GetSpriteFromPrefab: No Image component with sprite found at '{prefabPath}/{childPath}'");
            return null;
        }

        /// <summary>
        /// Find a GameObject at a path within a parent (supports "/" separated paths)
        /// </summary>
        /// <param name="parent">The parent GameObject to search from</param>
        /// <param name="path">Path like "child/grandchild/target"</param>
        /// <returns>The GameObject at the path, or null if not found</returns>
        public static GameObject FindChildByPath(GameObject parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path))
                return null;

            Transform current = parent.transform;
            string[] pathParts = path.Split('/');

            foreach (string part in pathParts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                Transform found = current.Find(part);
                if (found == null)
                {
                    return null;
                }
                current = found;
            }

            return current.gameObject;
        }

        /// <summary>
        /// Find a sprite by exact name in all loaded sprites
        /// </summary>
        public static Sprite FindSpriteByName(string spriteName, bool useCache = true)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            string cacheKey = $"name::{spriteName}";

            if (useCache && _cachedSprites.TryGetValue(cacheKey, out Sprite cached))
            {
                return cached;
            }

            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in allSprites)
            {
                if (sprite != null && sprite.name == spriteName)
                {
                    if (useCache)
                    {
                        _cachedSprites[cacheKey] = sprite;
                    }
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a sprite by keyword matching (all keywords must match)
        /// Searches all loaded sprites in memory - use as fallback when prefab name is unknown
        /// </summary>
        /// <param name="keywords">Keywords that must all be present in the sprite name (case-insensitive)</param>
        /// <param name="useCache">Whether to use cached sprites (default: true)</param>
        /// <returns>First matching sprite, or null</returns>
        /// <example>
        /// // Find "Rounded Corner Rect POT 9-Sliced x16"
        /// Sprite rounded = SpriteUtility.FindSpriteByKeywords(new[] { "sliced", "rounded", "16" });
        /// </example>
        public static Sprite FindSpriteByKeywords(string[] keywords, bool useCache = true)
        {
            if (keywords == null || keywords.Length == 0)
                return null;

            string keywordString = string.Join(",", keywords);
            string cacheKey = $"global::{keywordString}";

            // Check cache
            if (useCache && _cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            
            foreach (Sprite sprite in allSprites)
            {
                if (sprite == null) continue;
                
                string spriteName = sprite.name.ToLower();
                bool allMatch = true;
                
                foreach (string keyword in keywords)
                {
                    if (!spriteName.Contains(keyword.ToLower()))
                    {
                        allMatch = false;
                        break;
                    }
                }
                
                if (allMatch)
                {
                    if (useCache)
                    {
                        _cachedSprites[cacheKey] = sprite;
                    }
                    LaunchControlPlugin.Logger.LogInfo($"FindSpriteByKeywords: Found sprite '{sprite.name}'");
                    return sprite;
                }
            }
            
            LaunchControlPlugin.Logger.LogWarning($"FindSpriteByKeywords: No sprite matching keywords '{keywordString}' found");
            return null;
        }

        /// <summary>
        /// Load a sprite directly from Resources
        /// </summary>
        public static Sprite LoadSprite(string resourcePath, bool useCache = true)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;

            string cacheKey = $"resource::{resourcePath}";

            if (useCache && _cachedSprites.TryGetValue(cacheKey, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null && useCache)
            {
                _cachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// Get all sprites from a UI hierarchy dump path
        /// Example: Extract all physio indicator sprites from "pnlFillBars"
        /// </summary>
        /// <param name="root">Root GameObject to search from</param>
        /// <param name="childPath">Optional path within to search for Images</param>
        /// <returns>List of all sprites found</returns>
        public static List<Sprite> GetAllSpritesInHierarchy(GameObject root, string childPath = null)
        {
            List<Sprite> sprites = new List<Sprite>();
            
            GameObject target = string.IsNullOrEmpty(childPath) 
                ? root 
                : FindChildByPath(root, childPath);

            if (target == null)
                return sprites;

            Image[] images = target.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img != null && img.sprite != null)
                {
                    sprites.Add(img.sprite);
                }
            }

            return sprites;
        }

        /// <summary>
        /// Clear all cached sprites and prefabs
        /// </summary>
        public static void ClearCache()
        {
            _cachedSprites.Clear();
            _cachedPrefabs.Clear();
            LaunchControlPlugin.Logger.LogInfo("SpriteUtility: Cache cleared");
        }

        /// <summary>
        /// Get cache statistics for debugging
        /// </summary>
        public static string GetCacheStats()
        {
            return $"SpriteUtility Cache: {_cachedSprites.Count} sprites, {_cachedPrefabs.Count} prefabs";
        }

        /// <summary>
        /// Get all loaded sprites (from Resources.FindObjectsOfTypeAll)
        /// Useful for discovering available sprites
        /// </summary>
        /// <param name="maxResults">Maximum number of results to return (default: 100)</param>
        /// <returns>List of sprite names</returns>
        public static List<string> GetAllLoadedSprites(int maxResults = 100)
        {
            List<string> spriteNames = new List<string>();
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            
            int count = 0;
            foreach (Sprite sprite in allSprites)
            {
                if (sprite != null)
                {
                    spriteNames.Add(sprite.name);
                    count++;
                    if (count >= maxResults)
                        break;
                }
            }
            
            return spriteNames;
        }

        /// <summary>
        /// List all GameObjects that can be loaded from Resources
        /// </summary>
        /// <param name="maxResults">Maximum number of results to return (default: 50)</param>
        /// <returns>List of GameObject names from Resources</returns>
        public static List<string> GetAllResourcePrefabs(int maxResults = 50)
        {
            List<string> prefabNames = new List<string>();
            GameObject[] allObjects = Resources.LoadAll<GameObject>("");
            
            int count = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    prefabNames.Add(obj.name);
                    count++;
                    if (count >= maxResults)
                        break;
                }
            }
            
            return prefabNames;
        }

        #endregion

        #region Helper Methods

        private static GameObject LoadPrefab(string prefabPath, bool useCache)
        {
            if (useCache && _cachedPrefabs.TryGetValue(prefabPath, out GameObject cached))
            {
                return cached;
            }

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab != null && useCache)
            {
                _cachedPrefabs[prefabPath] = prefab;
            }

            return prefab;
        }

        private static string GetGameObjectPath(GameObject obj, GameObject root)
        {
            if (obj == null || obj == root)
                return "";

            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null && current.gameObject != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for building GameObject paths - makes path construction more readable
    /// </summary>
    public class GameObjectPath
    {
        private List<string> _pathParts = new List<string>();

        public GameObjectPath() { }

        public GameObjectPath(string initialPath)
        {
            if (!string.IsNullOrEmpty(initialPath))
            {
                _pathParts.AddRange(initialPath.Split('/'));
            }
        }

        /// <summary>
        /// Add a path segment
        /// </summary>
        public GameObjectPath Child(string childName)
        {
            _pathParts.Add(childName);
            return this;
        }

        /// <summary>
        /// Convert to path string
        /// </summary>
        public override string ToString()
        {
            // Convert to array manually to be safe
            string[] parts = new string[_pathParts.Count];
            for (int i = 0; i < _pathParts.Count; i++)
            {
                parts[i] = _pathParts[i];
            }
            return string.Join("/", parts);
        }

        /// <summary>
        /// Implicit conversion to string
        /// </summary>
        public static implicit operator string(GameObjectPath path)
        {
            return path.ToString();
        }

        /// <summary>
        /// Build a path fluently
        /// </summary>
        /// <example>
        /// var path = GameObjectPath.Build("GUICrewStatus").Child("bgBorder");
        /// // Results in: "GUICrewStatus/bgBorder"
        /// 
        /// var complexPath = GameObjectPath.Build()
        ///     .Child("pnlFillBars")
        ///     .Child("physio01")
        ///     .Child("bmpFill");
        /// // Results in: "pnlFillBars/physio01/bmpFill"
        /// </example>
        public static GameObjectPath Build(string root = null)
        {
            return new GameObjectPath(root);
        }
    }
}

