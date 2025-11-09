using System;

namespace Ostranauts.Bit.Items.Categories
{
    /// <summary>
    /// Represents an item or category identifier with optional alternative IDs
    /// This includes support for COOverlays - when an overlay is applied to an item,
    /// the CondOwner's strCODef is set to the overlay name, so overlay IDs should be
    /// added to AlternativeIds for proper category matching.
    /// </summary>
    [Serializable]
    public class ItemHintId
    {
        public string Id;
        public string[] AlternativeIds;

        /// <summary>
        /// Default constructor for JSON deserialization
        /// </summary>
        public ItemHintId()
        {
            Id = string.Empty;
            AlternativeIds = new string[0];
        }

        public ItemHintId(string id, params string[] alternativeIds)
        {
            Id = id;
            AlternativeIds = alternativeIds ?? new string[0];
        }

        /// <summary>
        /// Create an ItemHintId with overlay IDs included as alternatives
        /// </summary>
        /// <param name="baseId">Base CO definition ID</param>
        /// <param name="overlayIds">COOverlay IDs that are variants of this item</param>
        /// <param name="otherAlternativeIds">Other alternative IDs (damaged, loose, etc.)</param>
        /// <returns>ItemHintId with overlays included</returns>
        public static ItemHintId WithOverlays(string baseId, string[] overlayIds, params string[] otherAlternativeIds)
        {
            if (overlayIds == null || overlayIds.Length == 0)
            {
                return new ItemHintId(baseId, otherAlternativeIds);
            }

            // Combine overlay IDs with other alternative IDs
            string[] allAlternatives = new string[overlayIds.Length + otherAlternativeIds.Length];
            for (int i = 0; i < overlayIds.Length; i++)
            {
                allAlternatives[i] = overlayIds[i];
            }
            for (int i = 0; i < otherAlternativeIds.Length; i++)
            {
                allAlternatives[overlayIds.Length + i] = otherAlternativeIds[i];
            }

            return new ItemHintId(baseId, allAlternatives);
        }

        public static implicit operator ItemHintId(string id)
        {
            return new ItemHintId(id);
        }

        public static implicit operator string(ItemHintId hintId)
        {
            return hintId?.Id;
        }

        public override string ToString()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemHintId other)
                return Id == other.Id;
            if (obj is string str)
                return Id == str;
            return false;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }
}
