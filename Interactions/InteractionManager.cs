using System;
using System.Collections.Generic;
using Ostranauts.Bit.Tasks;

namespace Ostranauts.Bit.Interactions
{
    /// <summary>
    /// Manages custom interaction effects and provides access to interaction data storage
    /// </summary>
    public class InteractionManager
    {
        private List<IEffect> _registeredEffects;

        /// <summary>
        /// Creates a new InteractionManager instance
        /// </summary>
        public InteractionManager()
        {
            _registeredEffects = new List<IEffect>();
        }

        /// <summary>
        /// Registers a custom effect to be executed when interactions apply their effects
        /// Effects are executed in registration order
        /// </summary>
        /// <param name="effect">The effect to register</param>
        public void RegisterEffect(IEffect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));
            
            _registeredEffects.Add(effect);
            LaunchControlPlugin.Logger.LogInfo($"Registered custom interaction effect: {effect.GetType().Name}");
        }

        /// <summary>
        /// Gets all registered effects (internal use by patches)
        /// </summary>
        internal List<IEffect> GetRegisteredEffects()
        {
            return _registeredEffects;
        }

        // Convenience methods that delegate to InteractionDataProvider

        /// <summary>
        /// Sets custom data for an interaction
        /// </summary>
        /// <typeparam name="T">Type of the data to store</typeparam>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key to store the data under</param>
        /// <param name="value">The data to store</param>
        public void SetData<T>(Guid interactionId, string key, T value)
        {
            InteractionDataProvider.Instance.SetData(interactionId, key, value);
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
            return InteractionDataProvider.Instance.GetData<T>(interactionId, key);
        }

        /// <summary>
        /// Checks if an interaction has data stored under a specific key
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key to check for</param>
        /// <returns>True if data exists, false otherwise</returns>
        public bool HasData(Guid interactionId, string key)
        {
            return InteractionDataProvider.Instance.HasData(interactionId, key);
        }

        /// <summary>
        /// Removes specific data from an interaction
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        /// <param name="key">Key of the data to remove</param>
        public void RemoveData(Guid interactionId, string key)
        {
            InteractionDataProvider.Instance.RemoveData(interactionId, key);
        }

        /// <summary>
        /// Clears all data for an interaction
        /// </summary>
        /// <param name="interactionId">The Guid id of the interaction</param>
        public void ClearInteraction(Guid interactionId)
        {
            InteractionDataProvider.Instance.ClearInteraction(interactionId);
        }

        // Task2 convenience methods

        /// <summary>
        /// Gets the Task2 associated with an interaction (if any).
        /// Searches through active tasks to find one with a matching interaction.
        /// </summary>
        /// <param name="interaction">The interaction to find the task for</param>
        /// <returns>The associated Task2, or null if not found</returns>
        public Task2 GetTask(Interaction interaction)
        {
            if (ReferenceEquals(interaction, null))
            {
                return null;
            }

            if (ReferenceEquals(CrewSim.objInstance, null) || ReferenceEquals(CrewSim.objInstance.workManager, null))
            {
                return null;
            }

            WorkManager workManager = CrewSim.objInstance.workManager;

            // Access aTasksActive via reflection since it's private
            System.Reflection.FieldInfo activeTasksField = typeof(WorkManager).GetField("aTasksActive",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (ReferenceEquals(activeTasksField, null))
            {
                return null;
            }

            List<Task2> activeTasks = activeTasksField.GetValue(workManager) as List<Task2>;
            if (!ReferenceEquals(activeTasks, null))
            {
                for (int i = 0; i < activeTasks.Count; i++)
                {
                    Task2 task = activeTasks[i];
                    if (!ReferenceEquals(task, null))
                    {
                        Interaction taskInteraction = task.GetIA();
                        if (ReferenceEquals(taskInteraction, interaction))
                        {
                            return task;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Sets data on the Task2 associated with this interaction.
        /// If no task is found, this method does nothing.
        /// </summary>
        /// <typeparam name="T">Type of the data to store</typeparam>
        /// <param name="interaction">The interaction to get the task from</param>
        /// <param name="key">Key to store the data under</param>
        /// <param name="value">The data to store</param>
        public void SetTaskData<T>(Interaction interaction, string key, T value)
        {
            Task2 task = GetTask(interaction);
            if (!ReferenceEquals(task, null))
            {
                TaskDataProvider.Instance.SetData(task, key, value);
            }
        }

        /// <summary>
        /// Gets data from the Task2 associated with this interaction.
        /// </summary>
        /// <typeparam name="T">Type of the data to retrieve</typeparam>
        /// <param name="interaction">The interaction to get the task from</param>
        /// <param name="key">Key the data is stored under</param>
        /// <returns>The stored data, or default(T) if not found or no task associated</returns>
        public T GetTaskData<T>(Interaction interaction, string key)
        {
            Task2 task = GetTask(interaction);
            if (ReferenceEquals(task, null))
            {
                return default(T);
            }

            return TaskDataProvider.Instance.GetData<T>(task, key);
        }

        /// <summary>
        /// Checks if the Task2 associated with this interaction has data stored under a specific key.
        /// </summary>
        /// <param name="interaction">The interaction to get the task from</param>
        /// <param name="key">Key to check for</param>
        /// <returns>True if data exists, false otherwise or if no task associated</returns>
        public bool HasTaskData(Interaction interaction, string key)
        {
            Task2 task = GetTask(interaction);
            if (ReferenceEquals(task, null))
            {
                return false;
            }

            return TaskDataProvider.Instance.HasData(task, key);
        }
    }
}

