using System;
using System.Collections.Generic;

namespace Ostranauts.Bit.Tasks
{
    /// <summary>
    /// Provides persistent metadata storage for Task2 instances.
    /// Data is keyed by a stable hash of Task2 fields and survives save/load cycles.
    /// </summary>
    public class TaskDataProvider
    {
        private static TaskDataProvider _instance;

        // Runtime storage: key = Task2 hash, value = dictionary of custom data
        private Dictionary<string, Dictionary<string, object>> _taskData;

        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static TaskDataProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TaskDataProvider();
                }
                return _instance;
            }
        }

        private TaskDataProvider()
        {
            _taskData = new Dictionary<string, Dictionary<string, object>>();
        }

        /// <summary>
        /// Generates a stable hash key from Task2 fields.
        /// Uses strDuty, strInteraction, strTargetCOID, nTile, and strTileShip.
        /// </summary>
        private string GetTaskKey(Task2 task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // Create a stable string from task fields
            string compositeKey = string.Format("{0}:{1}:{2}:{3}:{4}",
                task.strDuty ?? "null",
                task.strInteraction ?? "null",
                task.strTargetCOID ?? "null",
                task.nTile,
                task.strTileShip ?? "null");

            // Return hash code as string for consistent key
            return compositeKey.GetHashCode().ToString();
        }

        /// <summary>
        /// Sets custom data for a Task2 instance
        /// </summary>
        /// <typeparam name="T">Type of data to store</typeparam>
        /// <param name="task">The Task2 instance</param>
        /// <param name="key">Key to store the data under</param>
        /// <param name="value">The data to store</param>
        public void SetData<T>(Task2 task, string key, T value)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            string taskKey = GetTaskKey(task);

            if (!_taskData.ContainsKey(taskKey))
            {
                _taskData[taskKey] = new Dictionary<string, object>();
            }

            _taskData[taskKey][key] = value;
        }

        /// <summary>
        /// Gets custom data for a Task2 instance
        /// </summary>
        /// <typeparam name="T">Type of data to retrieve</typeparam>
        /// <param name="task">The Task2 instance</param>
        /// <param name="key">Key the data is stored under</param>
        /// <returns>The stored data, or default(T) if not found</returns>
        public T GetData<T>(Task2 task, string key)
        {
            if (ReferenceEquals(task, null) || string.IsNullOrEmpty(key))
            {
                return default(T);
            }

            string taskKey = GetTaskKey(task);

            if (!_taskData.ContainsKey(taskKey))
            {
                return default(T);
            }

            if (!_taskData[taskKey].ContainsKey(key))
            {
                return default(T);
            }

            object value = _taskData[taskKey][key];
            
            if (ReferenceEquals(value, null))
            {
                return default(T);
            }

            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                // If cast fails, return default
                return default(T);
            }
        }

        /// <summary>
        /// Checks if a Task2 instance has data stored under a specific key
        /// </summary>
        /// <param name="task">The Task2 instance</param>
        /// <param name="key">Key to check for</param>
        /// <returns>True if data exists, false otherwise</returns>
        public bool HasData(Task2 task, string key)
        {
            if (ReferenceEquals(task, null) || string.IsNullOrEmpty(key))
            {
                return false;
            }

            string taskKey = GetTaskKey(task);

            return _taskData.ContainsKey(taskKey) && _taskData[taskKey].ContainsKey(key);
        }

        /// <summary>
        /// Removes specific data from a Task2 instance
        /// </summary>
        /// <param name="task">The Task2 instance</param>
        /// <param name="key">Key of the data to remove</param>
        public void RemoveData(Task2 task, string key)
        {
            if (ReferenceEquals(task, null) || string.IsNullOrEmpty(key))
            {
                return;
            }

            string taskKey = GetTaskKey(task);

            if (_taskData.ContainsKey(taskKey))
            {
                _taskData[taskKey].Remove(key);

                // If no data left for this task, remove the task entry
                if (_taskData[taskKey].Count == 0)
                {
                    _taskData.Remove(taskKey);
                }
            }
        }

        /// <summary>
        /// Clears all data for a Task2 instance
        /// </summary>
        /// <param name="task">The Task2 instance</param>
        public void ClearTask(Task2 task)
        {
            if (ReferenceEquals(task, null))
            {
                return;
            }

            string taskKey = GetTaskKey(task);
            _taskData.Remove(taskKey);
        }

        /// <summary>
        /// Removes data for tasks that no longer exist in the WorkManager.
        /// Should be called before saving to clean up orphaned data.
        /// </summary>
        public void CleanupOrphanedData()
        {
            if (ReferenceEquals(CrewSim.objInstance, null) || ReferenceEquals(CrewSim.objInstance.workManager, null))
            {
                return;
            }

            // Build a set of valid task keys from current tasks
            HashSet<string> validTaskKeys = new HashSet<string>();

            // Check all unclaimed tasks (these are the ones that get saved)
            WorkManager workManager = CrewSim.objInstance.workManager;
            
            // Access dictTasks2 via reflection since it's private
            System.Reflection.FieldInfo dictTasks2Field = typeof(WorkManager).GetField("dictTasks2", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (!ReferenceEquals(dictTasks2Field, null))
            {
                Dictionary<string, List<Task2>> dictTasks2 = dictTasks2Field.GetValue(workManager) as Dictionary<string, List<Task2>>;
                
                if (!ReferenceEquals(dictTasks2, null))
                {
                    foreach (KeyValuePair<string, List<Task2>> kvp in dictTasks2)
                    {
                        List<Task2> taskList = kvp.Value;
                        if (!ReferenceEquals(taskList, null))
                        {
                            for (int i = 0; i < taskList.Count; i++)
                            {
                                Task2 task = taskList[i];
                                if (!ReferenceEquals(task, null))
                                {
                                    try
                                    {
                                        string taskKey = GetTaskKey(task);
                                        validTaskKeys.Add(taskKey);
                                    }
                                    catch (Exception)
                                    {
                                        // Skip tasks that can't generate a valid key
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Also check active tasks via reflection
            System.Reflection.FieldInfo activeTasksField = typeof(WorkManager).GetField("aTasksActive",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (!ReferenceEquals(activeTasksField, null))
            {
                List<Task2> aTasksActive = activeTasksField.GetValue(workManager) as List<Task2>;
                
                if (!ReferenceEquals(aTasksActive, null))
                {
                    for (int i = 0; i < aTasksActive.Count; i++)
                    {
                        Task2 task = aTasksActive[i];
                        if (!ReferenceEquals(task, null))
                        {
                            try
                            {
                                string taskKey = GetTaskKey(task);
                                validTaskKeys.Add(taskKey);
                            }
                            catch (Exception)
                            {
                                // Skip tasks that can't generate a valid key
                            }
                        }
                    }
                }
            }

            // Remove data for tasks that no longer exist
            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, Dictionary<string, object>> kvp in _taskData)
            {
                if (!validTaskKeys.Contains(kvp.Key))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _taskData.Remove(keysToRemove[i]);
            }

            LaunchControlPlugin.Logger.LogInfo(string.Format("[TaskDataProvider] Cleaned up {0} orphaned task data entries", keysToRemove.Count));
        }

        /// <summary>
        /// Gets all task data for serialization (internal use by TaskDataHandler)
        /// </summary>
        internal Dictionary<string, Dictionary<string, object>> GetAllData()
        {
            return _taskData;
        }

        /// <summary>
        /// Sets all task data from deserialization (internal use by TaskDataHandler)
        /// </summary>
        internal void SetAllData(Dictionary<string, Dictionary<string, object>> data)
        {
            if (ReferenceEquals(data, null))
            {
                _taskData = new Dictionary<string, Dictionary<string, object>>();
            }
            else
            {
                _taskData = data;
            }
        }
    }
}

