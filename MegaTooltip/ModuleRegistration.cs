using System;
using UnityEngine;

namespace Ostranauts.Bit.MegaTooltip
{
    /// <summary>
    /// Delegate for visibility predicate that determines if a module should be shown
    /// </summary>
    public delegate bool VisibilityPredicate(CondOwner co);

    /// <summary>
    /// Delegate for UI setup callback
    /// </summary>
    public delegate void UISetupCallback(GameObject go);

    /// <summary>
    /// Builder/factory class for configuring a MegaToolTip module registration.
    /// Provides fluent interface for setting up visibility conditions and positioning.
    /// </summary>
    public class ModuleRegistration
    {
        /// <summary>
        /// The type of the module (must implement IDataModule)
        /// </summary>
        public Type ModuleType { get; private set; }

        /// <summary>
        /// Callback to setup the UI for this module when creating the template
        /// </summary>
        public UISetupCallback SetupUI { get; private set; }

        /// <summary>
        /// Predicate to determine if this module should be shown for a given CondOwner
        /// </summary>
        public VisibilityPredicate Predicate { get; private set; }

        /// <summary>
        /// Positioning mode for this module
        /// </summary>
        public PositioningMode Positioning { get; private set; }

        /// <summary>
        /// Target module name for positioning (used with InsertAfter/InsertBefore)
        /// </summary>
        public string TargetModuleName { get; private set; }

        /// <summary>
        /// Positioning mode options
        /// </summary>
        public enum PositioningMode
        {
            /// <summary>
            /// Append to the end of the module list
            /// </summary>
            Append,

            /// <summary>
            /// Insert after a specific module
            /// </summary>
            InsertAfter,

            /// <summary>
            /// Insert before a specific module
            /// </summary>
            InsertBefore
        }

        /// <summary>
        /// Create a new module registration
        /// </summary>
        public ModuleRegistration(Type moduleType, UISetupCallback setupUI)
        {
            // Use ReferenceEquals to avoid Type.op_Equality in older .NET Framework
            if (object.ReferenceEquals(moduleType, null))
            {
                throw new ArgumentNullException(nameof(moduleType));
            }

            if (object.ReferenceEquals(setupUI, null))
            {
                throw new ArgumentNullException(nameof(setupUI));
            }

            ModuleType = moduleType;
            SetupUI = setupUI;
            Predicate = null; // Default: always show (will be checked as null)
            Positioning = PositioningMode.Append;
            TargetModuleName = string.Empty;
        }

        /// <summary>
        /// Set a condition for when this module should be displayed
        /// </summary>
        /// <param name="predicate">Function that returns true if module should be shown</param>
        /// <returns>This registration for method chaining</returns>
        public ModuleRegistration OnlyShowIf(VisibilityPredicate predicate)
        {
            Predicate = predicate;
            return this;
        }

        /// <summary>
        /// Insert this module after a specific module type
        /// </summary>
        /// <param name="moduleName">Name of the module type to insert after (e.g., "ValueModule")</param>
        /// <returns>This registration for method chaining</returns>
        public ModuleRegistration InsertAfter(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentException("Module name cannot be null or empty", nameof(moduleName));
            }

            Positioning = PositioningMode.InsertAfter;
            TargetModuleName = moduleName;
            return this;
        }

        /// <summary>
        /// Insert this module before a specific module type
        /// </summary>
        /// <param name="moduleName">Name of the module type to insert before (e.g., "ValueModule")</param>
        /// <returns>This registration for method chaining</returns>
        public ModuleRegistration InsertBefore(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentException("Module name cannot be null or empty", nameof(moduleName));
            }

            Positioning = PositioningMode.InsertBefore;
            TargetModuleName = moduleName;
            return this;
        }

        /// <summary>
        /// Check if this module should be shown for the given CondOwner
        /// </summary>
        public bool ShouldShow(CondOwner co)
        {
            // If no predicate set, always show
            if (Predicate == null)
            {
                return true;
            }

            try
            {
                return Predicate(co);
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in visibility predicate for {ModuleType.Name}: {ex.Message}");
                return false;
            }
        }
    }
}

