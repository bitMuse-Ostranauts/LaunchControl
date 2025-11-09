using HarmonyLib;
using System;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patch to intercept console commands and check if LaunchControl has registered handlers
    /// </summary>
    [HarmonyPatch(typeof(ConsoleResolver), "ResolveString")]
    public static class ConsoleResolverPatch
    {
        /// <summary>
        /// Prefix patch that runs before ConsoleResolver.ResolveString
        /// Returns false to skip original method if command was handled by LaunchControl
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(ref string strInput)
        {
            try
            {
                // Check if LaunchControl is initialized
                if (LaunchControl.Instance == null)
                {
                    return true; // Continue to original method
                }

                // Try to execute the command via LaunchControl
                bool handled = LaunchControl.Instance.Commands.ExecuteCommand(strInput);

                if (handled)
                {
                    // Command was handled by LaunchControl, skip original ConsoleResolver
                    return false;
                }

                // Command not handled by LaunchControl, continue to original method
                return true;
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Error in ConsoleResolverPatch: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
                
                // Continue to original method on error
                return true;
            }
        }
    }
}

