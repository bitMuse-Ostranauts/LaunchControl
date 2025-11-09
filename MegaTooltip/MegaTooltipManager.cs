using System;
using System.Collections.Generic;
using Ostranauts.UI.MegaToolTip.Interfaces;
using UnityEngine;

namespace Ostranauts.Bit.MegaTooltip
{
    /// <summary>
    /// Manages registration and template caching for custom MegaToolTip modules
    /// </summary>
    public class MegaTooltipManager
    {
        private readonly List<ModuleRegistration> _itemModuleRegistrations;
        private readonly List<ModuleRegistration> _personModuleRegistrations;
        private readonly Dictionary<string, GameObject> _templateCache;
        private readonly GameObject _templateContainer;

        /// <summary>
        /// Create a new MegaTooltipManager
        /// </summary>
        /// <param name="parentObject">Parent GameObject to store templates under</param>
        public MegaTooltipManager(GameObject parentObject)
        {
            if (parentObject == null)
            {
                throw new ArgumentNullException(nameof(parentObject));
            }

            _itemModuleRegistrations = new List<ModuleRegistration>();
            _personModuleRegistrations = new List<ModuleRegistration>();
            _templateCache = new Dictionary<string, GameObject>();

            // Create a hidden container for module templates
            _templateContainer = new GameObject("MegaTooltipTemplates");
            _templateContainer.transform.SetParent(parentObject.transform, false);
            // Keep container active so templates can be used as prefabs
            // (Hidden off-screen via positioning instead)
            _templateContainer.SetActive(true);
            
            LaunchControlPlugin.Logger.LogInfo("MegaTooltipManager initialized");
        }

        /// <summary>
        /// Register a custom module for item tooltips
        /// </summary>
        /// <param name="moduleType">Module type (must implement IDataModule and derive from MonoBehaviour)</param>
        /// <param name="setupUI">Callback to programmatically create UI components</param>
        /// <returns>ModuleRegistration for fluent configuration</returns>
        public ModuleRegistration RegisterItemModule(Type moduleType, UISetupCallback setupUI)
        {
            ModuleRegistration registration = new ModuleRegistration(moduleType, setupUI);
            _itemModuleRegistrations.Add(registration);
            
            LaunchControlPlugin.Logger.LogInfo("Registered item module: " + moduleType.Name);
            
            return registration;
        }

        /// <summary>
        /// Register a custom module for person/crew tooltips
        /// </summary>
        /// <param name="moduleType">Module type (must implement IDataModule and derive from MonoBehaviour)</param>
        /// <param name="setupUI">Callback to programmatically create UI components</param>
        /// <returns>ModuleRegistration for fluent configuration</returns>
        public ModuleRegistration RegisterPersonModule(Type moduleType, UISetupCallback setupUI)
        {
            ModuleRegistration registration = new ModuleRegistration(moduleType, setupUI);
            _personModuleRegistrations.Add(registration);
            
            LaunchControlPlugin.Logger.LogInfo("Registered person module: " + moduleType.Name);
            
            return registration;
        }

        /// <summary>
        /// Get all registered item module registrations
        /// </summary>
        public List<ModuleRegistration> GetItemModules()
        {
            return _itemModuleRegistrations;
        }

        /// <summary>
        /// Get all registered person module registrations
        /// </summary>
        public List<ModuleRegistration> GetPersonModules()
        {
            return _personModuleRegistrations;
        }

        /// <summary>
        /// Get or create a template GameObject for the given module type
        /// </summary>
        /// <param name="registration">Module registration containing type and setup callback</param>
        /// <returns>Template GameObject (inactive, cached for cloning)</returns>
        public GameObject GetOrCreateTemplate(ModuleRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            // Use type name as key to avoid Type equality issues in older .NET Framework
            string typeKey = registration.ModuleType.FullName;

            // Check cache first
            GameObject cachedTemplate;
            if (_templateCache.TryGetValue(typeKey, out cachedTemplate))
            {
                return cachedTemplate;
            }

            // Create new template
            GameObject template = CreateTemplate(registration);
            _templateCache[typeKey] = template;
            
            return template;
        }

        /// <summary>
        /// Create a template GameObject from a registration
        /// </summary>
        private GameObject CreateTemplate(ModuleRegistration registration)
        {
            try
            {
                // Create GameObject with module type name
                GameObject template = new GameObject(registration.ModuleType.Name);
                template.transform.SetParent(_templateContainer.transform, false);
                
                // Add the module component
                Component component = template.AddComponent(registration.ModuleType);
                
                // Verify it implements IDataModule
                if (!(component is IDataModule))
                {
                    UnityEngine.Object.Destroy(template);
                    throw new InvalidOperationException(
                        $"Module type {registration.ModuleType.Name} does not implement IDataModule");
                }

                // Call setup callback to create UI
                try
                {
                    registration.SetupUI(template);
                }
                catch (Exception ex)
                {
                    LaunchControlPlugin.Logger.LogError(
                        $"Error during UI setup for {registration.ModuleType.Name}: {ex.Message}");
                    LaunchControlPlugin.Logger.LogError(ex.StackTrace);
                    UnityEngine.Object.Destroy(template);
                    throw;
                }

                // Keep template ACTIVE for use as a prefab
                // (Unity instantiates inactive clones from inactive prefabs)
                template.SetActive(true);
                
                LaunchControlPlugin.Logger.LogInfo($"Created template for module: {registration.ModuleType.Name}");
                
                return template;
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to create template for {registration.ModuleType.Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create an instance of a module from its template
        /// </summary>
        /// <param name="registration">Module registration</param>
        /// <param name="parent">Parent transform for the instance</param>
        /// <returns>New module instance GameObject (active)</returns>
        public GameObject InstantiateModule(ModuleRegistration registration, Transform parent)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            // Get or create template
            GameObject template = GetOrCreateTemplate(registration);

            // Clone the template
            GameObject instance = UnityEngine.Object.Instantiate(template, parent);
            instance.name = registration.ModuleType.Name; // Remove "(Clone)" suffix
            instance.SetActive(true); // Activate the instance
            
            return instance;
        }

        /// <summary>
        /// Clear all registrations and templates (useful for cleanup/testing)
        /// </summary>
        public void Clear()
        {
            _itemModuleRegistrations.Clear();
            _personModuleRegistrations.Clear();
            
            // Destroy all cached templates
            foreach (GameObject template in _templateCache.Values)
            {
                if (template != null)
                {
                    UnityEngine.Object.Destroy(template);
                }
            }
            _templateCache.Clear();
            
            LaunchControlPlugin.Logger.LogInfo("MegaTooltipManager cleared");
        }
    }
}

