using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Ostranauts.Bit.Pledges
{
    /// <summary>
    /// Manager for registering and creating custom pledge types.
    /// This allows mods to add new AI behaviors/pledges without modifying game files.
    /// </summary>
    public class PledgeManager
    {
        private readonly Dictionary<string, Type> _customPledgeTypes;
        private ManualLogSource _logger;

        public PledgeManager()
        {
            _customPledgeTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Set the logger for this manager
        /// </summary>
        public void SetLogger(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register a custom pledge type with the system.
        /// The pledge type must inherit from Pledge2 and have a parameterless constructor.
        /// </summary>
        /// <param name="pledgeTypeName">The name used in JSON pledge definitions (strType field)</param>
        /// <param name="pledgeClass">The C# class type that implements the pledge</param>
        /// <returns>True if registered successfully, false otherwise</returns>
        public bool RegisterPledgeType(string pledgeTypeName, Type pledgeClass)
        {
            if (string.IsNullOrEmpty(pledgeTypeName))
            {
                LogError("Cannot register pledge with null or empty type name");
                return false;
            }

            if (pledgeClass == null)
            {
                LogError($"Cannot register pledge '{pledgeTypeName}' with null class type");
                return false;
            }

            // Verify the class inherits from Pledge2
            if (!typeof(Pledge2).IsAssignableFrom(pledgeClass))
            {
                LogError($"Cannot register pledge '{pledgeTypeName}': Class {pledgeClass.Name} does not inherit from Pledge2");
                return false;
            }

            // Verify the class has a parameterless constructor
            if (pledgeClass.GetConstructor(Type.EmptyTypes) == null)
            {
                LogError($"Cannot register pledge '{pledgeTypeName}': Class {pledgeClass.Name} does not have a parameterless constructor");
                return false;
            }

            // Register or update the pledge type
            if (_customPledgeTypes.ContainsKey(pledgeTypeName))
            {
                LogWarning($"Pledge type '{pledgeTypeName}' already registered, overwriting with {pledgeClass.Name}");
                _customPledgeTypes[pledgeTypeName] = pledgeClass;
            }
            else
            {
                _customPledgeTypes.Add(pledgeTypeName, pledgeClass);
                LogInfo($"Registered custom pledge type '{pledgeTypeName}' -> {pledgeClass.Name}");
            }

            return true;
        }

        /// <summary>
        /// Attempt to create a custom pledge instance from a pledge type name.
        /// Returns null if the pledge type is not registered.
        /// </summary>
        /// <param name="pledgeTypeName">The pledge type name to create</param>
        /// <returns>A new Pledge2 instance or null if not found</returns>
        public Pledge2 CreatePledge(string pledgeTypeName)
        {
            if (string.IsNullOrEmpty(pledgeTypeName))
            {
                return null;
            }

            if (!_customPledgeTypes.TryGetValue(pledgeTypeName, out Type pledgeType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(pledgeType) as Pledge2;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create pledge instance for type '{pledgeTypeName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a pledge type is registered
        /// </summary>
        public bool IsRegistered(string pledgeTypeName)
        {
            return !string.IsNullOrEmpty(pledgeTypeName) && 
                   _customPledgeTypes.ContainsKey(pledgeTypeName);
        }

        /// <summary>
        /// Get all registered pledge type names
        /// </summary>
        public IEnumerable<string> GetRegisteredPledgeTypes()
        {
            return _customPledgeTypes.Keys;
        }

        /// <summary>
        /// Get the count of registered pledge types
        /// </summary>
        public int RegisteredCount => _customPledgeTypes.Count;

        /// <summary>
        /// Clear all registered pledge types (useful for testing)
        /// </summary>
        internal void Clear()
        {
            _customPledgeTypes.Clear();
        }

        #region Logging helpers

        private void LogInfo(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo($"[PledgeManager] {message}");
            }
            else
            {
                Debug.Log($"[PledgeManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning($"[PledgeManager] {message}");
            }
            else
            {
                Debug.LogWarning($"[PledgeManager] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError($"[PledgeManager] {message}");
            }
            else
            {
                Debug.LogError($"[PledgeManager] {message}");
            }
        }

        #endregion
    }
}

