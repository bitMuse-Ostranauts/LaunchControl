using System;
using System.Collections.Generic;

namespace Ostranauts.Bit.Interactions
{
    /// <summary>
    /// Manages custom data storage for Interaction instances using their Guid id
    /// Data is automatically cleaned up when interactions are destroyed
    /// </summary>
    public class InteractionDataProvider
    {
        private static InteractionDataProvider _instance;
        private Dictionary<Guid, Dictionary<string, object>> _interactionData;

        /// <summary>
        /// Singleton instance of the InteractionDataProvider
        /// </summary>
        public static InteractionDataProvider Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new InteractionDataProvider();
                return _instance;
            }
        }

        private InteractionDataProvider()
        {
            _interactionData = new Dictionary<Guid, Dictionary<string, object>>();
        }

        /// <summary>
        /// Sets custom data for an interaction
        /// </summary>
        /// <typeparam name="T">Type of the data to store</typeparam>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key to store the data under</param>
        /// <param name="value">The data to store</param>
        public void SetData<T>(Guid interactionId, string key, T value)
        {
            if (!_interactionData.ContainsKey(interactionId))
            {
                _interactionData[interactionId] = new Dictionary<string, object>();
            }

            _interactionData[interactionId][key] = value;
        }

        /// <summary>
        /// Gets custom data for an interaction
        /// </summary>
        /// <typeparam name="T">Type of the data to retrieve</typeparam>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key the data is stored under</param>
        /// <returns>The stored data, or default(T) if not found</returns>
        public T GetData<T>(Guid interactionId, string key)
        {
            if (_interactionData.ContainsKey(interactionId) && 
                _interactionData[interactionId].ContainsKey(key))
            {
                object value = _interactionData[interactionId][key];
                if (value is T)
                {
                    return (T)value;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Checks if an interaction has data stored under a specific key
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key to check for</param>
        /// <returns>True if data exists, false otherwise</returns>
        public bool HasData(Guid interactionId, string key)
        {
            return _interactionData.ContainsKey(interactionId) && 
                   _interactionData[interactionId].ContainsKey(key);
        }

        /// <summary>
        /// Removes specific data from an interaction
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key of the data to remove</param>
        public void RemoveData(Guid interactionId, string key)
        {
            if (_interactionData.ContainsKey(interactionId))
            {
                _interactionData[interactionId].Remove(key);
            }
        }

        /// <summary>
        /// Clears all data for an interaction
        /// Called automatically when an interaction is destroyed
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        public void ClearInteraction(Guid interactionId)
        {
            if (_interactionData.ContainsKey(interactionId))
            {
                _interactionData.Remove(interactionId);
            }
        }
    }
}

