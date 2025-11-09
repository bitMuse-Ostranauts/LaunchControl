using HarmonyLib;
using Ostranauts.Bit.Interactions;
using System;
using System.Collections.Generic;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patches for Interaction class to support custom effects and data cleanup
    /// </summary>
    [HarmonyPatch(typeof(Interaction))]
    public class InteractionPatches
    {
        /// <summary>
        /// Prefix patch on Interaction.ApplyEffects to prepare/modify interactions before they execute
        /// </summary>
        [HarmonyPatch("ApplyEffects", typeof(List<string>), typeof(bool))]
        [HarmonyPrefix]
        public static void ApplyEffects_Prefix(Interaction __instance, List<string> aLog, bool isCancelIa)
        {
            try
            {
                // Check if LaunchControl is initialized
                if (LaunchControl.Instance == null || LaunchControl.Instance.Interactions == null)
                {
                    return;
                }

                // Get registered effects
                List<IEffect> effects = LaunchControl.Instance.Interactions.GetRegisteredEffects();
                if (effects == null || effects.Count == 0)
                {
                    return;
                }

                // Call Prepare on effects that need it
                foreach (IEffect effect in effects)
                {
                    try
                    {
                        if (effect.ShouldPrepare(__instance))
                        {
                            effect.Prepare(__instance);
                        }
                    }
                    catch (Exception ex)
                    {
                        LaunchControlPlugin.Logger.LogError($"Error preparing interaction effect {effect.GetType().Name}: {ex.Message}");
                        LaunchControlPlugin.Logger.LogError(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in InteractionPatches.ApplyEffects_Prefix: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Postfix patch on Interaction.ApplyEffects to execute custom registered effects
        /// </summary>
        [HarmonyPatch("ApplyEffects", typeof(List<string>), typeof(bool))]
        [HarmonyPostfix]
        public static void ApplyEffects_Postfix(Interaction __instance, List<string> aLog, bool isCancelIa)
        {
            try
            {
                // Check if LaunchControl is initialized
                if (LaunchControl.Instance == null || LaunchControl.Instance.Interactions == null)
                {
                    return;
                }

                // Get registered effects
                List<IEffect> effects = LaunchControl.Instance.Interactions.GetRegisteredEffects();
                if (effects == null || effects.Count == 0)
                {
                    return;
                }

                // Iterate through effects
                foreach (IEffect effect in effects)
                {
                    try
                    {
                        // Check if effect should execute
                        if (!effect.ShouldExecute(__instance))
                        {
                            continue;
                        }

                        // Execute the effect
                        EffectResult executeResult = effect.Execute(__instance);
                        
                        // If execute returns false for bShouldContinue, stop iteration
                        if (executeResult == null || !executeResult.bShouldContinue)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LaunchControlPlugin.Logger.LogError($"Error executing custom interaction effect {effect.GetType().Name}: {ex.Message}");
                        LaunchControlPlugin.Logger.LogError(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in InteractionPatches.ApplyEffects_Postfix: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Postfix patch on Interaction.Destroy to clean up interaction data
        /// </summary>
        [HarmonyPatch("Destroy")]
        [HarmonyPostfix]
        public static void Destroy_Postfix(Interaction __instance)
        {
            try
            {
                // Clean up interaction data using the id field
                if (__instance != null)
                {
                    InteractionDataProvider.Instance.ClearInteraction(__instance.id);
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in InteractionPatches.Destroy_Postfix: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }
    }
}

