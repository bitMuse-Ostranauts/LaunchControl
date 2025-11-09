using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Bit.Items.Categories
{
    /// <summary>
    /// Delegate for item matching functions (runtime matching using CondOwner)
    /// </summary>
    public delegate bool ItemMatcher(CondOwner condOwner);

    /// <summary>
    /// Manager for item categories providing CRUD operations and matching functionality
    /// </summary>
    public class ItemCategoryManager
    {
        private readonly Dictionary<string, ItemCategory> _categories;
        private readonly Dictionary<string, HashSet<string>> _itemToCategories; // itemId -> set of categoryIds

        /// <summary>
        /// Create a new ItemCategoryManager
        /// </summary>
        public ItemCategoryManager()
        {
            _categories = new Dictionary<string, ItemCategory>();
            _itemToCategories = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Add a new category
        /// </summary>
        /// <param name="category">Category to add</param>
        /// <param name="parentId">Optional parent category ID</param>
        public void AddCategory(ItemCategory category, string parentId = null)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (_categories.ContainsKey(category.Id))
            {
                LaunchControlPlugin.Logger.LogWarning($"Category '{category.Id}' already exists. Overwriting.");
            }

            _categories[category.Id] = category;

            // Add to parent if specified
            if (!string.IsNullOrEmpty(parentId))
            {
                if (_categories.TryGetValue(parentId, out ItemCategory parent))
                {
                    parent.AddSubcategory(category);
                }
                else
                {
                    LaunchControlPlugin.Logger.LogWarning($"Parent category '{parentId}' not found when adding '{category.Id}'. Category added without parent.");
                }
            }

            LaunchControlPlugin.Logger.LogInfo($"Added category: {category.Id}");
        }

        /// <summary>
        /// Remove a category
        /// </summary>
        /// <param name="categoryId">Category ID to remove</param>
        public bool RemoveCategory(string categoryId)
        {
            if (!_categories.TryGetValue(categoryId, out ItemCategory category))
            {
                return false;
            }

            // Remove from parent
            if (category.Parent != null)
            {
                category.Parent.RemoveSubcategory(category);
            }

            // Remove all subcategories (create copy to avoid modifying collection during iteration)
            var subcategories = new List<ItemCategory>();
            for (int i = 0; i < category.Subcategories.Count; i++)
            {
                subcategories.Add(category.Subcategories[i]);
            }
            foreach (var subcategory in subcategories)
            {
                RemoveCategory(subcategory.Id);
            }

            // Remove items from tracking
            foreach (var item in category.Items)
            {
                // Remove primary ID
                if (_itemToCategories.TryGetValue(item.Id, out HashSet<string> categorySet))
                {
                    categorySet.Remove(categoryId);
                    if (categorySet.Count == 0)
                    {
                        _itemToCategories.Remove(item.Id);
                    }
                }
                
                // Remove alternative IDs
                if (item.AlternativeIds != null)
            {
                    foreach (var altId in item.AlternativeIds)
                {
                        if (_itemToCategories.TryGetValue(altId, out categorySet))
                    {
                        categorySet.Remove(categoryId);
                        if (categorySet.Count == 0)
                        {
                                _itemToCategories.Remove(altId);
                            }
                        }
                    }
                }
            }

            _categories.Remove(categoryId);
            LaunchControlPlugin.Logger.LogInfo($"Removed category: {categoryId}");

            return true;
        }

        /// <summary>
        /// Rename a category
        /// </summary>
        /// <param name="categoryId">Current category ID</param>
        /// <param name="newId">New category ID</param>
        /// <param name="newDisplayName">Optional new display name</param>
        public bool RenameCategory(string categoryId, string newId, string newDisplayName = null)
        {
            if (string.IsNullOrEmpty(newId))
            {
                throw new ArgumentException("New category ID cannot be null or empty", nameof(newId));
            }

            if (!_categories.TryGetValue(categoryId, out ItemCategory category))
            {
                return false;
            }

            if (_categories.ContainsKey(newId) && newId != categoryId)
            {
                throw new InvalidOperationException($"Category '{newId}' already exists");
            }

            // Update ID
            var oldId = category.Id;
            _categories.Remove(oldId);
            
            // Use reflection to update the private Id field, or create a new category with new ID
            // Since Id is private set, we'll need to recreate the category structure
            var newCategory = new ItemCategory(newId, newDisplayName ?? category.DisplayName, category.Description);
            
            // Copy items
            foreach (var item in category.Items)
            {
                newCategory.AddItem(new ItemHintId(item.Id, item.AlternativeIds));
            }

            // Copy predicates
            foreach (var predicate in category.Predicates)
            {
                newCategory.AddPredicate(predicate);
            }

            // Update parent relationship
            if (category.Parent != null)
            {
                var parent = category.Parent;
                parent.RemoveSubcategory(category);
                parent.AddSubcategory(newCategory);
            }

            // Update subcategories (create copy to avoid modifying collection during iteration)
            var subcategories = new List<ItemCategory>();
            for (int i = 0; i < category.Subcategories.Count; i++)
            {
                subcategories.Add(category.Subcategories[i]);
            }
            foreach (var subcategory in subcategories)
            {
                category.RemoveSubcategory(subcategory);
                newCategory.AddSubcategory(subcategory);
            }

            _categories[newId] = newCategory;

            // Update item tracking (create copy to avoid modifying collection during iteration)
            var itemTrackingCopy = new List<KeyValuePair<string, HashSet<string>>>();
            foreach (var kvp in _itemToCategories)
            {
                itemTrackingCopy.Add(kvp);
            }
            foreach (var kvp in itemTrackingCopy)
            {
                if (kvp.Value.Contains(oldId))
                {
                    kvp.Value.Remove(oldId);
                    kvp.Value.Add(newId);
                }
            }

            LaunchControlPlugin.Logger.LogInfo($"Renamed category '{oldId}' to '{newId}'");

            return true;
        }

        /// <summary>
        /// Get a category by ID
        /// </summary>
        public ItemCategory GetCategory(string categoryId)
        {
            _categories.TryGetValue(categoryId, out ItemCategory category);
            return category;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        public IEnumerable<ItemCategory> GetAllCategories()
        {
            return _categories.Values;
        }

        /// <summary>
        /// Get root categories (categories without parents)
        /// </summary>
        public List<ItemCategory> GetRootCategories()
        {
            var rootCategories = new List<ItemCategory>();
            foreach (var category in _categories.Values)
            {
                if (category.Parent == null)
                {
                    rootCategories.Add(category);
                }
            }
            return rootCategories;
        }

        /// <summary>
        /// Register an item to a category
        /// </summary>
        /// <param name="itemId">Item ID (strCODef or strName)</param>
        /// <param name="categoryId">Category ID</param>
        public void RegisterItem(string itemId, string categoryId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("Item ID cannot be null or empty", nameof(itemId));
            }

            if (!_categories.TryGetValue(categoryId, out ItemCategory category))
            {
                throw new ArgumentException($"Category '{categoryId}' not found", nameof(categoryId));
            }

            category.AddItem(itemId);

            // Track item to category mapping
            if (!_itemToCategories.TryGetValue(itemId, out HashSet<string> categorySet))
            {
                categorySet = new HashSet<string>();
                _itemToCategories[itemId] = categorySet;
            }
            categorySet.Add(categoryId);

            LaunchControlPlugin.Logger.LogDebug($"Registered item '{itemId}' to category '{categoryId}'");
        }

        /// <summary>
        /// Unregister an item from a category
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <param name="categoryId">Category ID</param>
        public bool UnregisterItem(string itemId, string categoryId)
        {
            if (!_categories.TryGetValue(categoryId, out ItemCategory category))
            {
                return false;
            }

            bool removed = category.RemoveItem(itemId);

            if (removed && _itemToCategories.TryGetValue(itemId, out HashSet<string> categorySet))
            {
                categorySet.Remove(categoryId);
                if (categorySet.Count == 0)
                {
                    _itemToCategories.Remove(itemId);
                }
            }

            return removed;
        }

        /// <summary>
        /// Check if a string is a category ID
        /// </summary>
        private bool IsCategoryId(string id)
        {
            return _categories.ContainsKey(id);
        }

        /// <summary>
        /// Create a matcher function from an array of category IDs and item IDs
        /// The returned function checks if a CondOwner matches any of the provided IDs (runtime check)
        /// </summary>
        /// <param name="categoryOrItemIds">Array of category IDs or item IDs</param>
        /// <returns>Function that returns true if CondOwner matches any ID in the array</returns>
        public ItemMatcher CreateMatcher(string[] categoryOrItemIds)
        {
            if (categoryOrItemIds == null || categoryOrItemIds.Length == 0)
            {
                return delegate(CondOwner co) { return false; }; // Empty array matches nothing
            }

            var categoryIds = new HashSet<string>();
            var itemIds = new HashSet<string>();

            // Separate category IDs from item IDs
            foreach (var id in categoryOrItemIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (IsCategoryId(id))
                {
                    categoryIds.Add(id);
                }
                else
                {
                    itemIds.Add(id);
                }
            }

            return delegate(CondOwner co)
            {
                if (co == null)
                {
                    return false;
                }

                // Check direct item ID match
                if (itemIds.Count > 0)
                {
                    if (!string.IsNullOrEmpty(co.strCODef) && itemIds.Contains(co.strCODef))
                    {
                        return true;
                    }

                    if (!string.IsNullOrEmpty(co.strItemDef) && itemIds.Contains(co.strItemDef))
                    {
                        return true;
                    }
                }

                // Check category matches
                if (categoryIds.Count > 0)
                {
                    foreach (var categoryId in categoryIds)
                    {
                        if (_categories.TryGetValue(categoryId, out ItemCategory category))
                        {
                            // Check if CondOwner matches this category (including recursive parent checks)
                            if (category.MatchesRecursive(co))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            };
        }

        /// <summary>
        /// Get all categories that contain a specific item
        /// </summary>
        public IEnumerable<ItemCategory> GetCategoriesForItem(string itemId)
        {
            if (_itemToCategories.TryGetValue(itemId, out HashSet<string> categoryIds))
            {
                foreach (var categoryId in categoryIds)
                {
                    if (_categories.TryGetValue(categoryId, out ItemCategory category))
                    {
                        yield return category;
                    }
                }
            }

            // Also check categories that match via predicates
            foreach (var category in _categories.Values)
            {
                // Note: We can't check predicates without a CondOwner instance,
                // so this only returns categories with direct item ID matches
            }
        }

        /// <summary>
        /// Scan for unmapped items and add them to the misc category
        /// Should be called after all categories and items are registered
        /// </summary>
        public void ScanUnmappedItems()
        {
            if (DataHandler.dictCOs == null)
            {
                LaunchControlPlugin.Logger.LogWarning("DataHandler.dictCOs is not available. Cannot scan for unmapped items.");
                return;
            }

            const string miscCategoryId = "Miscellaneous.Unclassified";
            
            // Ensure misc category exists
            if (!_categories.TryGetValue(miscCategoryId, out ItemCategory miscCategory))
            {
                miscCategory = new ItemCategory(miscCategoryId, "Unclassified", "Items that could not be categorized");
                AddCategory(miscCategory, "Miscellaneous");
            }

            var unmappedItems = new List<string>();
            int totalItems = 0;

            foreach (var kvp in DataHandler.dictCOs)
            {
                var coDef = kvp.Key;
                var jsonCO = kvp.Value;

                // Only process items (not crew, ships, etc.)
                if (jsonCO.strType != null && jsonCO.strType.ToLower() == "item")
                {
                    totalItems++;

                    // Check if item is mapped to any category
                    bool isMapped = false;

                    // Check direct item ID mapping
                    if (_itemToCategories.ContainsKey(coDef))
                    {
                        isMapped = true;
                    }

                    // Check if any category matches via predicates
                    if (!isMapped)
                    {
                        // Create a temporary CondOwner to test predicates
                        // Since we can't easily create a CondOwner from JsonCondOwner,
                        // we'll check if the item matches any category's direct item IDs
                        // Check if item is in any items in any category
                        foreach (var category in _categories.Values)
                        {
                            foreach (var item in category.Items)
                            {
                                if (item.Id == coDef || (item.AlternativeIds != null && item.AlternativeIds.Contains(coDef)))
                                {
                                    isMapped = true;
                                    break;
                                }
                            }
                            if (isMapped) break;
                        }
                    }

                    if (!isMapped)
                    {
                        unmappedItems.Add(coDef);
                        miscCategory.AddItem(coDef);
                        
                        // Update tracking
                        if (!_itemToCategories.TryGetValue(coDef, out HashSet<string> categorySet))
                        {
                            categorySet = new HashSet<string>();
                            _itemToCategories[coDef] = categorySet;
                        }
                        categorySet.Add(miscCategoryId);
                    }
                }
            }

            if (unmappedItems.Count > 0)
            {
                LaunchControlPlugin.Logger.LogInfo($"Found {unmappedItems.Count} unmapped items out of {totalItems} total items. Added to '{miscCategoryId}' category.");
                
                // Log first 20 unmapped items at debug level
                foreach (var itemId in unmappedItems.Take(20))
                {
                    LaunchControlPlugin.Logger.LogDebug($"Unmapped item: {itemId}");
                }
                if (unmappedItems.Count > 20)
                {
                    LaunchControlPlugin.Logger.LogDebug($"... and {unmappedItems.Count - 20} more unmapped items");
                }
            }
            else
            {
                LaunchControlPlugin.Logger.LogInfo($"All {totalItems} items are mapped to categories.");
            }
        }

        /// <summary>
        /// Populate categories from their predicates by evaluating all items in DataHandler
        /// </summary>
        public void PopulateFromPredicates()
        {
            if (DataHandler.dictCOs == null || DataHandler.dictCOs.Count == 0)
            {
                LaunchControlPlugin.Logger.LogWarning("DataHandler.dictCOs is not available, cannot populate from predicates");
                return;
            }

            LaunchControlPlugin.Logger.LogInfo("Populating categories from predicates...");
            int totalMatches = 0;

            // Copy dictCOs to a list to avoid "out of sync" errors during iteration
            List<KeyValuePair<string, JsonCondOwner>> itemList = new List<KeyValuePair<string, JsonCondOwner>>();
            foreach (var pair in DataHandler.dictCOs)
            {
                itemList.Add(pair);
            }

            foreach (var categoryPair in _categories)
            {
                var category = categoryPair.Value;
                
                // Skip categories without predicates
                if (category.Predicates == null || category.Predicates.Count == 0)
                    continue;

                int matchesForCategory = 0;
                
                // Test each item in the game against this category's predicates
                foreach (var itemPair in itemList)
                {
                    string itemId = itemPair.Key;
                    JsonCondOwner jsonCO = itemPair.Value;
                    
                    // Execute each predicate to see if this item matches
                    bool matches = false;
                    foreach (var predicate in category.Predicates)
                    {
                        try
                        {
                            if (predicate(jsonCO))
                            {
                                matches = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LaunchControlPlugin.Logger.LogError($"Error evaluating predicate for category '{category.Id}' on item '{itemId}': {ex.Message}");
                        }
                    }
                    
                    if (matches)
                    {
                        // Check if this ID already exists in the category
                        bool idExists = false;
                        foreach (var existingItem in category.Items)
                        {
                            if (existingItem.Id == itemId)
                            {
                                idExists = true;
                                break;
                            }
                            
                            // Also check alternative IDs
                            if (existingItem.AlternativeIds != null)
                            {
                                foreach (var altId in existingItem.AlternativeIds)
                                {
                                    if (altId == itemId)
                                    {
                                        idExists = true;
                                        break;
                                    }
                                }
                                if (idExists) break;
                            }
                        }
                        
                        if (idExists)
                        {
                            // Already in category, skip
                            continue;
                        }
                        
                        // Check if an item with the same NAME already exists
                        string itemName = jsonCO.strName ?? jsonCO.strNameFriendly ?? itemId;
                        ItemHintId existingItemWithSameName = null;
                        
                        foreach (var existingItem in category.Items)
                        {
                            // Get the name of the existing item
                            string existingName = null;
                            if (DataHandler.dictCOs.TryGetValue(existingItem.Id, out JsonCondOwner existingCO))
                            {
                                existingName = existingCO.strName ?? existingCO.strNameFriendly ?? existingItem.Id;
                            }
                            
                            if (existingName != null && existingName == itemName)
                            {
                                existingItemWithSameName = existingItem;
                                break;
                            }
                        }
                        
                        if (existingItemWithSameName != null)
                        {
                            // Add as alternative ID to existing item
                            // Check if it's already in the alternatives
                            bool alreadyAlternative = false;
                            if (existingItemWithSameName.AlternativeIds != null)
                            {
                                for (int i = 0; i < existingItemWithSameName.AlternativeIds.Length; i++)
                                {
                                    if (existingItemWithSameName.AlternativeIds[i] == itemId)
                                    {
                                        alreadyAlternative = true;
                                        break;
                                    }
                                }
                            }
                            
                            if (!alreadyAlternative)
                            {
                                // Create new array with additional ID
                                int oldLength = existingItemWithSameName.AlternativeIds?.Length ?? 0;
                                string[] newAlternatives = new string[oldLength + 1];
                                
                                // Copy existing alternatives
                                if (existingItemWithSameName.AlternativeIds != null)
                                {
                                    for (int i = 0; i < oldLength; i++)
                                    {
                                        newAlternatives[i] = existingItemWithSameName.AlternativeIds[i];
                                    }
                                }
                                
                                // Add new ID
                                newAlternatives[oldLength] = itemId;
                                existingItemWithSameName.AlternativeIds = newAlternatives;
                                
                                matchesForCategory++;
                                totalMatches++;
                            }
                        }
                        else
                        {
                            // Add as new item to category
                            category.AddItem(itemId);
                            matchesForCategory++;
                            totalMatches++;
                        }
                    }
                }
                
                if (matchesForCategory > 0)
                {
                    LaunchControlPlugin.Logger.LogInfo($"  {category.Id}: Found {matchesForCategory} matching items");
                }
            }
            
            LaunchControlPlugin.Logger.LogInfo($"Populated {totalMatches} total items from predicates");
            
            // Now add overlays for items that are already in categories
            PopulateOverlays();
        }

        /// <summary>
        /// Populate categories with COOverlays based on their base items
        /// If a base item is in a category, all its overlays are also added
        /// </summary>
        private void PopulateOverlays()
        {
            if (DataHandler.dictCOOverlays == null || DataHandler.dictCOOverlays.Count == 0)
            {
                // Don't log - the warning is already logged in LaunchControlPlugin if overlays didn't load
                return;
            }

            LaunchControlPlugin.Logger.LogInfo($"Populating categories with {DataHandler.dictCOOverlays.Count} COOverlays...");
            int totalOverlays = 0;

            // Copy overlays to a list to avoid "out of sync" errors
            List<KeyValuePair<string, JsonCOOverlay>> overlayList = new List<KeyValuePair<string, JsonCOOverlay>>();
            foreach (var pair in DataHandler.dictCOOverlays)
            {
                overlayList.Add(pair);
            }

            foreach (var categoryPair in _categories)
            {
                var category = categoryPair.Value;
                int overlaysForCategory = 0;

                // For each overlay, check if its base item is in this category
                foreach (var overlayPair in overlayList)
                {
                    string overlayId = overlayPair.Key;
                    JsonCOOverlay overlay = overlayPair.Value;

                    // Skip if no base CO
                    if (string.IsNullOrEmpty(overlay.strCOBase))
                    {
                        continue;
                    }

                    // Check if the base CO is in this category's Items list
                    bool baseInCategory = false;
                    foreach (var item in category.Items)
                    {
                        if (item.Id == overlay.strCOBase)
                        {
                            baseInCategory = true;
                            break;
                        }
                        
                        // Also check alternative IDs
                        if (item.AlternativeIds != null)
                        {
                            foreach (var altId in item.AlternativeIds)
                            {
                                if (altId == overlay.strCOBase)
                                {
                                    baseInCategory = true;
                                    break;
                                }
                            }
                            if (baseInCategory) break;
                        }
                    }

                    if (baseInCategory)
                    {
                        // Add overlay to category if not already present
                        category.AddItem(overlayId);
                        overlaysForCategory++;
                        totalOverlays++;
                    }
                }

                if (overlaysForCategory > 0)
                {
                    LaunchControlPlugin.Logger.LogInfo($"  {category.Id}: Added {overlaysForCategory} overlays");
                }
            }

            LaunchControlPlugin.Logger.LogInfo($"Populated {totalOverlays} total overlays");
        }

        /// <summary>
        /// Clear all categories
        /// </summary>
        public void Clear()
        {
            _categories.Clear();
            _itemToCategories.Clear();
            LaunchControlPlugin.Logger.LogInfo("ItemCategoryManager cleared");
        }
    }
}

