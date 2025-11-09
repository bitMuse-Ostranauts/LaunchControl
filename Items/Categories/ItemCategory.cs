using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Bit.Items.Categories
{
    /// <summary>
    /// Delegate for item category predicate matching (used only at startup for population)
    /// </summary>
    public delegate bool ItemCategoryPredicate(JsonCondOwner condOwner);

    /// <summary>
    /// Represents a hierarchical item category that can contain items and subcategories
    /// </summary>
    public class ItemCategory
    {
        /// <summary>
        /// Unique identifier for this category
        /// </summary>
        public ItemHintId Id { get; private set; }

        /// <summary>
        /// Display name for this category
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description of what items belong to this category
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Parent category (null if root category)
        /// </summary>
        public ItemCategory Parent { get; private set; }

        /// <summary>
        /// Child categories
        /// </summary>
        public List<ItemCategory> Subcategories { get; private set; }

        /// <summary>
        /// List of item hint IDs in this category
        /// </summary>
        public List<ItemHintId> Items { get; private set; }

        /// <summary>
        /// Predicate rules for procedurally matching items to this category
        /// </summary>
        public List<ItemCategoryPredicate> Predicates { get; private set; }

        /// <summary>
        /// Create a new category
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description</param>
        public ItemCategory(string id, string displayName, string description = "")
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Category ID cannot be null or empty", nameof(id));
            }

            Id = new ItemHintId(id);
            DisplayName = displayName ?? id;
            Description = description ?? "";
            Subcategories = new List<ItemCategory>();
            Items = new List<ItemHintId>();
            Predicates = new List<ItemCategoryPredicate>();
        }

        /// <summary>
        /// Add an item hint ID to this category
        /// </summary>
        /// <param name="itemHintId">Item hint ID to add</param>
        public void AddItem(ItemHintId itemHintId)
        {
            if (itemHintId != null && !string.IsNullOrEmpty(itemHintId.Id))
            {
                // Check if already exists (avoiding LINQ for Unity compatibility)
                bool alreadyExists = false;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i].Id == itemHintId.Id)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                
                if (!alreadyExists)
                {
                    Items.Add(itemHintId);
                }
            }
        }
        
        /// <summary>
        /// Add an item by ID (implicit conversion to ItemHintId)
        /// </summary>
        /// <param name="itemId">Item ID to add</param>
        public void AddItem(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                AddItem(new ItemHintId(itemId));
            }
        }

        /// <summary>
        /// Add an item with alternative IDs
        /// </summary>
        /// <param name="itemId">Primary item ID</param>
        /// <param name="alternativeIds">Alternative item IDs (variants, damaged, loose, etc.)</param>
        public void AddItem(string itemId, params string[] alternativeIds)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                AddItem(new ItemHintId(itemId, alternativeIds));
            }
        }

        /// <summary>
        /// Remove an item by ID
        /// </summary>
        /// <param name="itemId">Item ID to remove</param>
        public bool RemoveItem(string itemId)
        {
            // Manual removal (avoiding LINQ for Unity compatibility)
            bool removed = false;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Id == itemId)
                {
                    Items.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }

        /// <summary>
        /// Add a subcategory to this category
        /// </summary>
        /// <param name="subcategory">Subcategory to add</param>
        public void AddSubcategory(ItemCategory subcategory)
        {
            if (subcategory == null)
            {
                throw new ArgumentNullException(nameof(subcategory));
            }

            if (subcategory.Parent != null)
            {
                throw new InvalidOperationException($"Category '{subcategory.Id}' already has a parent");
            }

            if (Subcategories.Contains(subcategory))
            {
                return; // Already added
            }

            Subcategories.Add(subcategory);
            subcategory.Parent = this;
        }

        /// <summary>
        /// Remove a subcategory from this category
        /// </summary>
        /// <param name="subcategory">Subcategory to remove</param>
        public bool RemoveSubcategory(ItemCategory subcategory)
        {
            if (subcategory == null)
            {
                return false;
            }

            bool removed = Subcategories.Remove(subcategory);
            if (removed)
            {
                subcategory.Parent = null;
            }

            return removed;
        }

        /// <summary>
        /// Add a predicate rule for matching items
        /// </summary>
        /// <param name="predicate">Function that returns true if CondOwner matches this category</param>
        public void AddPredicate(ItemCategoryPredicate predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            Predicates.Add(predicate);
        }

        /// <summary>
        /// Check if a CondOwner matches this category (runtime check)
        /// This checks direct item IDs and alternative IDs against the Items list (populated at startup)
        /// </summary>
        /// <param name="co">CondOwner to check</param>
        /// <returns>True if the CondOwner matches this category</returns>
        public bool Matches(CondOwner co)
        {
            if (co == null)
            {
                return false;
            }

            // Check direct item ID match (including alternative IDs)
            if (!string.IsNullOrEmpty(co.strCODef))
            {
                foreach (var item in Items)
                {
                    if (item.Id == co.strCODef)
                    {
                        return true;
                    }
                    
                    // Check alternative IDs
                    if (item.AlternativeIds != null)
                    {
                        foreach (var altId in item.AlternativeIds)
                        {
                            if (altId == co.strCODef)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(co.strItemDef))
            {
                foreach (var item in Items)
                {
                    if (item.Id == co.strItemDef)
                    {
                        return true;
                    }
                    
                    // Check alternative IDs
                    if (item.AlternativeIds != null)
                    {
                        foreach (var altId in item.AlternativeIds)
                        {
                            if (altId == co.strItemDef)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // Note: Predicates are NOT checked here - they only run at startup to populate Items list
            
            // Recursively check parent categories
            if (Parent != null)
            {
                return Parent.Matches(co);
            }

            return false;
        }

        /// <summary>
        /// Get all descendant categories (children, grandchildren, etc.)
        /// </summary>
        public IEnumerable<ItemCategory> GetAllDescendants()
        {
            foreach (var subcategory in Subcategories)
            {
                yield return subcategory;
                foreach (var descendant in subcategory.GetAllDescendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Get the full path from root to this category (e.g., "Food.PreparedMeals.StreetFood")
        /// </summary>
        public string GetFullPath()
        {
            var path = new List<string>();
            var current = this;

            while (current != null)
            {
                path.Insert(0, current.Id);
                current = current.Parent;
            }

            return string.Join(".", path);
        }

        /// <summary>
        /// Get all item IDs in this category and all subcategories (flattened)
        /// </summary>
        public HashSet<string> GetAllItemIds()
        {
            var allIds = new HashSet<string>();
            
            // Add all IDs from items (including alternative IDs)
            foreach (var item in Items)
            {
                allIds.Add(item.Id);
                
                if (item.AlternativeIds != null)
                {
                    foreach (var altId in item.AlternativeIds)
                    {
                        allIds.Add(altId);
                    }
                }
            }
            
            // Add all IDs from subcategories
            foreach (var subcategory in Subcategories)
            {
                foreach (var itemId in subcategory.GetAllItemIds())
                {
                    allIds.Add(itemId);
                }
            }
            
            return allIds;
        }

        /// <summary>
        /// Check if this category or any descendant matches the CondOwner
        /// </summary>
        public bool MatchesRecursive(CondOwner co)
        {
            // Check self
            if (Matches(co))
            {
                return true;
            }

            // Check all descendants
            foreach (var descendant in GetAllDescendants())
            {
                if (descendant.Matches(co))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

