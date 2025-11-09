using HarmonyLib;
using Ostranauts.Bit.Pledges;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patches for the PledgeFactory to support custom pledge types.
    /// Injects custom pledges registered via LaunchControl into the game's pledge system.
    /// </summary>
    [HarmonyPatch(typeof(PledgeFactory))]
    public static class PledgeFactoryPatch
    {
        private static bool _injectedIntoDictTypes = false;

        /// <summary>
        /// Prefix patch to inject custom pledge types into the game's dictTypes dictionary.
        /// This allows custom pledges to be created by the standard Factory method.
        /// </summary>
        [HarmonyPatch(nameof(PledgeFactory.Factory), new Type[] { typeof(CondOwner), typeof(JsonPledge), typeof(CondOwner) })]
        [HarmonyPrefix]
        public static void Factory_Prefix()
        {
            if (_injectedIntoDictTypes)
            {
                return;
            }

            try
            {
                // Access the static dictTypes field via reflection
                var dictTypesField = AccessTools.Field(typeof(PledgeFactory), "dictTypes");

                if (ReferenceEquals(dictTypesField, null))
                {
                    Debug.LogError("[PledgeFactoryPatch] Could not find dictTypes field in PledgeFactory");
                    return;
                }

                var dictTypes = dictTypesField.GetValue(null) as Dictionary<string, Type>;

                if (ReferenceEquals(dictTypes, null))
                {
                    Debug.LogError("[PledgeFactoryPatch] dictTypes is null or not a Dictionary<string, Type>");
                    return;
                }

                // Inject all registered custom pledge types
                if (LaunchControl.Instance == null || LaunchControl.Instance.Pledges == null)
                {
                    Debug.LogWarning("[PledgeFactoryPatch] LaunchControl.Instance.Pledges not yet initialized");
                    return;
                }

                var pledgeManager = LaunchControl.Instance.Pledges;
                int injectedCount = 0;

                foreach (string pledgeTypeName in pledgeManager.GetRegisteredPledgeTypes())
                {
                    // Create a temporary instance to get the type
                    Pledge2 tempPledge = pledgeManager.CreatePledge(pledgeTypeName);
                    if (tempPledge != null)
                    {
                        Type pledgeType = tempPledge.GetType();
                        
                        if (dictTypes.ContainsKey(pledgeTypeName))
                        {
                            Debug.LogWarning($"[PledgeFactoryPatch] Overwriting existing pledge type '{pledgeTypeName}' with {pledgeType.Name}");
                            dictTypes[pledgeTypeName] = pledgeType;
                        }
                        else
                        {
                            dictTypes.Add(pledgeTypeName, pledgeType);
                        }
                        
                        injectedCount++;
                    }
                }

                if (injectedCount > 0)
                {
                    Debug.Log($"[PledgeFactoryPatch] Successfully injected {injectedCount} custom pledge type(s) into PledgeFactory.dictTypes");
                }

                _injectedIntoDictTypes = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PledgeFactoryPatch] Error injecting pledge types: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Postfix patch to handle pledge creation for custom types.
        /// This is a fallback in case the prefix injection didn't work or was called after the first Factory call.
        /// </summary>
        [HarmonyPatch(nameof(PledgeFactory.Factory), new Type[] { typeof(CondOwner), typeof(JsonPledge), typeof(CondOwner) })]
        [HarmonyPostfix]
        public static void Factory_Postfix(ref Pledge2 __result, CondOwner coUs, JsonPledge jp, CondOwner coThem = null)
        {
            // If the base game already created a pledge, we don't need to do anything
            if (__result != null)
            {
                return;
            }

            // Check if this is one of our custom pledge types
            if (jp == null || string.IsNullOrEmpty(jp.strType))
            {
                return;
            }

            try
            {
                // Try to create a custom pledge
                if (LaunchControl.Instance == null || LaunchControl.Instance.Pledges == null)
                {
                    return;
                }

                var pledgeManager = LaunchControl.Instance.Pledges;
                Pledge2 customPledge = pledgeManager.CreatePledge(jp.strType);

                if (customPledge != null)
                {
                    // Initialize the pledge using the Init method (standard Pledge2 pattern)
                    if (customPledge.Init(coUs, jp, coThem))
                    {
                        __result = customPledge;
                        Debug.Log($"[PledgeFactoryPatch] Created custom pledge '{jp.strName}' of type '{jp.strType}' via postfix");
                    }
                    else
                    {
                        Debug.LogWarning($"[PledgeFactoryPatch] Failed to initialize custom pledge '{jp.strType}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PledgeFactoryPatch] Error creating custom pledge '{jp.strType}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reset the injection flag (useful for testing or mod reloading)
        /// </summary>
        internal static void ResetInjection()
        {
            _injectedIntoDictTypes = false;
        }
    }
}

