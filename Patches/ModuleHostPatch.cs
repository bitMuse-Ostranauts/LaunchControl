using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Ostranauts.Bit.MegaTooltip;
using Ostranauts.UI.MegaToolTip;
using Ostranauts.UI.MegaToolTip.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patch for ModuleHost.SetData to inject custom registered modules
    /// </summary>
    [HarmonyPatch(typeof(ModuleHost))]
    public class ModuleHostPatch
    {
        // Cached reflection fields
        private static FieldInfo _itemModulePrefabsField = null;
        private static FieldInfo _personModulePrefabsField = null;
        
        // Cache the original prefab arrays (only set once from Unity serialized data)
        private static GameObject[] _originalItemPrefabs = null;
        private static GameObject[] _originalPersonPrefabs = null;

        /// <summary>
        /// Prefix patch on ModuleHost.SetData to inject custom module templates into prefab arrays
        /// </summary>
        [HarmonyPatch("SetData", typeof(CondOwner))]
        [HarmonyPrefix]
        public static void SetData_Prefix(ModuleHost __instance, CondOwner co)
        {
            if (co == null)
            {
                return;
            }

            // Get LaunchControl instance
            if (LaunchControl.Instance == null || LaunchControl.Instance.MegaTooltipManager == null)
            {
                return;
            }

            try
            {
                // Determine which arrays to modify based on CondOwner type
                bool isItem = co.strType == "Item";
                List<ModuleRegistration> registrations = isItem 
                    ? LaunchControl.Instance.MegaTooltipManager.GetItemModules()
                    : LaunchControl.Instance.MegaTooltipManager.GetPersonModules();

                if (registrations.Count == 0)
                {
                    return; // No custom modules to inject
                }

                // Get and cache original prefab arrays
                if (_originalItemPrefabs == null || _originalPersonPrefabs == null)
                {
                    _originalItemPrefabs = GetItemPrefabs(__instance);
                    _originalPersonPrefabs = GetPersonPrefabs(__instance);
                    
                    if (_originalItemPrefabs == null || _originalPersonPrefabs == null)
                    {
                        LaunchControlPlugin.Logger.LogError("[ModuleHostPatch] Failed to get original prefab arrays");
                        return;
                    }
                }

                // Build modified array with custom modules injected
                GameObject[] originalArray = isItem ? _originalItemPrefabs : _originalPersonPrefabs;
                GameObject[] modifiedArray = BuildModifiedPrefabArray(originalArray, registrations, co);

                // Set the modified array back to the instance
                if (isItem)
                {
                    SetItemPrefabs(__instance, modifiedArray);
                }
                else
                {
                    SetPersonPrefabs(__instance, modifiedArray);
                }

                LaunchControlPlugin.Logger.LogInfo(
                    $"[ModuleHostPatch] Injected {registrations.Count} custom modules into {(isItem ? "item" : "person")} prefab array " +
                    $"(original: {originalArray.Length}, modified: {modifiedArray.Length})");
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"[ModuleHostPatch] Error in SetData_Prefix: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Build a modified prefab array with custom modules inserted at the correct positions
        /// </summary>
        private static GameObject[] BuildModifiedPrefabArray(
            GameObject[] originalPrefabs,
            List<ModuleRegistration> registrations,
            CondOwner co)
        {
            List<GameObject> resultList = new List<GameObject>(originalPrefabs);

            foreach (ModuleRegistration registration in registrations)
            {
                // Check visibility predicate
                if (!registration.ShouldShow(co))
                {
                    continue;
                }

                // Get the template for this module
                GameObject template = LaunchControl.Instance.MegaTooltipManager.GetOrCreateTemplate(registration);
                if (template == null)
                {
                    LaunchControlPlugin.Logger.LogWarning($"[ModuleHostPatch] Failed to get template for {registration.ModuleType.Name}");
                    continue;
                }

                // Calculate insertion index
                int insertionIndex = CalculateInsertionIndex(registration, resultList);

                // Insert the template into the array
                resultList.Insert(insertionIndex, template);

                LaunchControlPlugin.Logger.LogInfo(
                    $"[ModuleHostPatch] Inserted {registration.ModuleType.Name} template at index {insertionIndex}");
            }

            // Convert to array manually to be safe
            GameObject[] result = new GameObject[resultList.Count];
            for (int i = 0; i < resultList.Count; i++)
            {
                result[i] = resultList[i];
            }
            return result;
        }

        /// <summary>
        /// Calculate where to insert the module based on its positioning configuration
        /// </summary>
        private static int CalculateInsertionIndex(
            ModuleRegistration registration,
            List<GameObject> prefabs)
        {
            switch (registration.Positioning)
            {
                case ModuleRegistration.PositioningMode.InsertAfter:
                    return FindModuleIndex(prefabs, registration.TargetModuleName, after: true);

                case ModuleRegistration.PositioningMode.InsertBefore:
                    return FindModuleIndex(prefabs, registration.TargetModuleName, after: false);

                case ModuleRegistration.PositioningMode.Append:
                default:
                    return prefabs.Count; // Append to end
            }
        }

        /// <summary>
        /// Find the index to insert a module relative to a target module in prefab array
        /// </summary>
        private static int FindModuleIndex(
            List<GameObject> prefabs, 
            string targetModuleName, 
            bool after)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                IDataModule module = prefabs[i].GetComponent<IDataModule>();
                if (module == null) continue;

                string moduleName = module.GetType().Name;
                if (moduleName == targetModuleName)
                {
                    // Found the target module
                    int insertIndex = after ? i + 1 : i;
                    LaunchControlPlugin.Logger.LogInfo(
                        $"[ModuleHostPatch] Found '{targetModuleName}' at prefab index {i}, inserting at {insertIndex}");
                    return insertIndex;
                }
            }

            // Target not found, append to end
            LaunchControlPlugin.Logger.LogWarning(
                $"[ModuleHostPatch] Target module '{targetModuleName}' not found in prefabs, appending to end");
            return prefabs.Count;
        }

        /// <summary>
        /// Get the _itemModulePrefabs array via reflection
        /// </summary>
        private static GameObject[] GetItemPrefabs(ModuleHost instance)
        {
            if (object.ReferenceEquals(_itemModulePrefabsField, null))
            {
                _itemModulePrefabsField = typeof(ModuleHost).GetField(
                    "_itemModulePrefabs",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
            }

            if (object.ReferenceEquals(_itemModulePrefabsField, null))
            {
                LaunchControlPlugin.Logger.LogError("[ModuleHostPatch] Could not find _itemModulePrefabs field");
                return null;
            }

            return _itemModulePrefabsField.GetValue(instance) as GameObject[];
        }

        /// <summary>
        /// Get the _personModulePrefabs array via reflection
        /// </summary>
        private static GameObject[] GetPersonPrefabs(ModuleHost instance)
        {
            if (object.ReferenceEquals(_personModulePrefabsField, null))
            {
                _personModulePrefabsField = typeof(ModuleHost).GetField(
                    "_personModulePrefabs",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
            }

            if (object.ReferenceEquals(_personModulePrefabsField, null))
            {
                LaunchControlPlugin.Logger.LogError("[ModuleHostPatch] Could not find _personModulePrefabs field");
                return null;
            }

            return _personModulePrefabsField.GetValue(instance) as GameObject[];
        }

        /// <summary>
        /// Set the _itemModulePrefabs array via reflection
        /// </summary>
        private static void SetItemPrefabs(ModuleHost instance, GameObject[] prefabs)
        {
            if (!object.ReferenceEquals(_itemModulePrefabsField, null))
            {
                _itemModulePrefabsField.SetValue(instance, prefabs);
            }
        }

        /// <summary>
        /// Set the _personModulePrefabs array via reflection
        /// </summary>
        private static void SetPersonPrefabs(ModuleHost instance, GameObject[] prefabs)
        {
            if (!object.ReferenceEquals(_personModulePrefabsField, null))
            {
                _personModulePrefabsField.SetValue(instance, prefabs);
            }
        }
    }
}

