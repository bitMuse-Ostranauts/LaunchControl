using System;
using System.Collections.Generic;
using Ostranauts.Bit.Items;
using Ostranauts.Bit.Commands;
using Ostranauts.Bit.MegaTooltip;
using Ostranauts.Bit.PersistentData;
using Ostranauts.Bit.Patches;
using Ostranauts.Bit.Interactions;
using Ostranauts.Bit.Pledges;
using Ostranauts.Bit.Tasks;
using Ostranauts.Bit.PatchSystem;
using Ostranauts.UI.MegaToolTip.Interfaces;
using UnityEngine;

namespace Ostranauts.Bit
{
    /// <summary>
    /// LaunchControl singleton providing command registration and utility functions
    /// </summary>
    public class LaunchControl : MonoBehaviour
    {
        private static LaunchControl _instance;

        private MegaTooltipManager _megaTooltipManager;

        private CommandManager _commandManager;

        private ItemManager _itemManager;

        private PersistentDataManager _persistentDataManager;

        private InteractionManager _interactionManager;

        private PledgeManager _pledgeManager;

        private TaskDataProvider _taskDataProvider;

        private PatchSystem.PatchManager _patchManager;

        /// <summary>
        /// Event fired when the game has finished loading all ships and is ready for the player.
        /// This occurs after loading is complete but before the game unpauses.
        /// Mods can subscribe to this to perform initialization tasks on existing saves.
        /// </summary>
        public static event Action OnGameReady;
        
        /// <summary>
        /// Item manager instance
        /// </summary>
        public ItemManager Items
        {
            get { return _itemManager; }
        }

        /// <summary>
        /// Command manager instance
        /// </summary>
        public CommandManager Commands
        {
            get { return _commandManager; }
        }

        /// <summary>
        /// Singleton instance of LaunchControl
        /// </summary>
        public static LaunchControl Instance
        {
            get { return _instance; }
            private set { _instance = value; }
        }

        /// <summary>
        /// MegaTooltip manager instance
        /// </summary>
        public MegaTooltipManager MegaTooltipManager
        {
            get { return _megaTooltipManager; }
        }

        /// <summary>
        /// Persistent data manager instance
        /// </summary>
        public PersistentDataManager PersistentData
        {
            get { return _persistentDataManager; }
        }

        /// <summary>
        /// Interaction manager instance for registering custom effects and managing interaction data
        /// </summary>
        public InteractionManager Interactions
        {
            get { return _interactionManager; }
        }

        /// <summary>
        /// Pledge manager instance for registering custom pledge types
        /// </summary>
        public PledgeManager Pledges
        {
            get { return _pledgeManager; }
        }

        /// <summary>
        /// Task data provider instance for storing custom metadata on Task2 instances
        /// </summary>
        public TaskDataProvider Tasks
        {
            get { return _taskDataProvider; }
        }

        /// <summary>
        /// Patch manager instance for applying JSON patches to game data
        /// </summary>
        public PatchSystem.PatchManager PatchManager
        {
            get { return _patchManager; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                LaunchControlPlugin.Logger.LogWarning("Multiple LaunchControl instances detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            
            // Initialize MegaTooltipManager
            _megaTooltipManager = new MegaTooltipManager(gameObject);

            _itemManager = new ItemManager();

            _commandManager = new CommandManager();

            // Initialize InteractionManager
            _interactionManager = new InteractionManager();

            // Initialize PledgeManager
            _pledgeManager = new PledgeManager();
            _pledgeManager.SetLogger(LaunchControlPlugin.Logger);

            // Initialize PersistentDataManager
            try
            {
                _persistentDataManager = PersistentDataManager.Instance;
                _persistentDataManager.SetLogger(LaunchControlPlugin.Logger);
                
                // Initialize the persistent data load listener
                PersistentDataPatches.InitializeLoadListener();
                
                LaunchControlPlugin.Logger.LogInfo("PersistentData system initialized");
            }
            catch (System.Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to initialize PersistentData system: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }

            // Initialize TaskDataProvider and register TaskDataHandler
            try
            {
                _taskDataProvider = TaskDataProvider.Instance;
                
                TaskDataHandler taskHandler = new TaskDataHandler();
                taskHandler.Logger = LaunchControlPlugin.Logger;
                _persistentDataManager.RegisterHandler("LaunchControl.tasks", taskHandler);
                
                LaunchControlPlugin.Logger.LogInfo("Task data system initialized");
            }
            catch (System.Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to initialize Task data system: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }

            // Initialize PatchManager
            try
            {
                _patchManager = new PatchSystem.PatchManager(LaunchControlPlugin.Logger);
                LaunchControlPlugin.Logger.LogInfo("Patch system initialized");
            }
            catch (System.Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to initialize Patch system: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
            
            // Subscribe to game load complete event
            try
            {
                CrewSim.OnGameFinishedLoading.AddListener(OnCrewSimGameFinishedLoading);
                LaunchControlPlugin.Logger.LogInfo("Subscribed to game load complete event");
            }
            catch (System.Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to subscribe to game load event: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
            
            LaunchControlPlugin.Logger.LogInfo("LaunchControl instance initialized");
        }

        /// <summary>
        /// Called when CrewSim finishes loading the game
        /// </summary>
        private void OnCrewSimGameFinishedLoading()
        {
            LaunchControlPlugin.Logger.LogInfo("Game finished loading - firing OnGameReady event");
            OnGameReady?.Invoke();
        }

        /// <summary>
        /// Register a custom pledge type that can be used in the game's AI system.
        /// The pledge type must inherit from Pledge2 and have a parameterless constructor.
        /// </summary>
        /// <param name="pledgeTypeName">The name used in JSON pledge definitions (strType field)</param>
        /// <param name="pledgeClass">The C# class type that implements the pledge (must inherit from Pledge2)</param>
        /// <returns>True if registered successfully, false otherwise</returns>
        public static bool RegisterPledgeType(string pledgeTypeName, Type pledgeClass)
        {
            if (_instance == null)
            {
                Debug.LogError("LaunchControl.RegisterPledgeType called before LaunchControl was initialized!");
                return false;
            }

            return _instance._pledgeManager.RegisterPledgeType(pledgeTypeName, pledgeClass);
        }

        /// <summary>
        /// Register a custom module for item tooltips
        /// </summary>
        /// <param name="moduleType">Module type (must implement IDataModule and derive from MonoBehaviour)</param>
        /// <param name="setupUI">Callback to programmatically create UI components</param>
        /// <returns>ModuleRegistration for fluent configuration</returns>
        public static ModuleRegistration RegisterItemModule(Type moduleType, UISetupCallback setupUI)
        {
            if (_instance == null)
            {
                Debug.LogError("LaunchControl.RegisterItemModule called before LaunchControl was initialized!");
                return null;
            }

            return _instance._megaTooltipManager.RegisterItemModule(moduleType, setupUI);
        }

        /// <summary>
        /// Register a custom module for person/crew tooltips
        /// </summary>
        /// <param name="moduleType">Module type (must implement IDataModule and derive from MonoBehaviour)</param>
        /// <param name="setupUI">Callback to programmatically create UI components</param>
        /// <returns>ModuleRegistration for fluent configuration</returns>
        public static ModuleRegistration RegisterPersonModule(Type moduleType, UISetupCallback setupUI)
        {
            if (_instance == null)
            {
                Debug.LogError("LaunchControl.RegisterPersonModule called before LaunchControl was initialized!");
                return null;
            }

            return _instance._megaTooltipManager.RegisterPersonModule(moduleType, setupUI);
        }

        private void OnDestroy()
        {
            // Unsubscribe from game load complete event
            if (CrewSim.OnGameFinishedLoading != null)
            {
                CrewSim.OnGameFinishedLoading.RemoveListener(OnCrewSimGameFinishedLoading);
            }
            
            // Cleanup the persistent data load listener
            PersistentDataPatches.CleanupLoadListener();
        }
    }
}
