using HarmonyLib;
using Ostranauts.Bit.Tasks;
using System;

namespace Ostranauts.Bit.Patches
{
    /// <summary>
    /// Harmony patches for Task2 lifecycle management
    /// </summary>
    [HarmonyPatch(typeof(WorkManager))]
    public class TaskPatches
    {
        /// <summary>
        /// Cleanup task data when a task is removed from WorkManager
        /// </summary>
        [HarmonyPatch(nameof(WorkManager.RemoveTask), typeof(Task2))]
        [HarmonyPostfix]
        public static void RemoveTask_Postfix(Task2 task)
        {
            if (ReferenceEquals(task, null))
            {
                return;
            }

            try
            {
                TaskDataProvider.Instance.ClearTask(task);
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError(string.Format("[TaskPatches] Error clearing task data: {0}\n{1}", 
                    ex.Message, ex.StackTrace));
            }
        }
    }
}

