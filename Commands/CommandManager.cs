using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Bit.Commands
{
    public class CommandManager
    {
        private Dictionary<string, Action<string>> _commands;

        public CommandManager()
        {
            _commands = new Dictionary<string, Action<string>>();
        }

        /// <summary>
        /// Register a console command with a callback (instance method)
        /// </summary>
        /// <param name="commandName">The command name (e.g., "dumpui")</param>
        /// <param name="callback">Callback that receives the full command input string</param>
        public void RegisterCommand(string commandName, Action<string> callback)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                LaunchControlPlugin.Logger.LogError("Cannot register command with null or empty name");
                return;
            }

            if (callback == null)
            {
                LaunchControlPlugin.Logger.LogError($"Cannot register command '{commandName}' with null callback");
                return;
            }

            string lowerCommandName = commandName.ToLower();

            if (_commands.ContainsKey(lowerCommandName))
            {
                LaunchControlPlugin.Logger.LogWarning($"Command '{commandName}' is already registered. Overwriting.");
                _commands[lowerCommandName] = callback;
            }
            else
            {
                _commands.Add(lowerCommandName, callback);
                LaunchControlPlugin.Logger.LogInfo($"Registered command: {commandName}");
            }
        }

        /// <summary>
        /// Execute a command if it's registered
        /// </summary>
        /// <param name="input">The full command input string</param>
        /// <returns>True if command was found and executed, false otherwise</returns>
        public bool ExecuteCommand(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            string trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
            {
                return false;
            }

            // Extract command name (first word)
            string commandName;
            int spaceIndex = trimmedInput.IndexOf(' ');
            if (spaceIndex > 0)
            {
                commandName = trimmedInput.Substring(0, spaceIndex).ToLower();
            }
            else
            {
                commandName = trimmedInput.ToLower();
            }

            // Check if command is registered
            if (_commands.ContainsKey(commandName))
            {
                try
                {
                    Action<string> callback = _commands[commandName];
                    callback(trimmedInput);
                    return true;
                }
                catch (Exception ex)
                {
                    LaunchControlPlugin.Logger.LogError($"Error executing command '{commandName}': {ex.Message}");
                    LaunchControlPlugin.Logger.LogError(ex.StackTrace);
                    DevConsole.Output($"<color=red>Error executing command '{commandName}': {ex.Message}</color>");
                    return true; // Return true because command was found, even though it errored
                }
            }

            return false;
        }

        /// <summary>
        /// Static convenience method for registering commands
        /// </summary>
        public static void RegisterCommandStatic(string commandName, Action<string> callback)
        {
            if (LaunchControl.Instance == null)
            {
                Debug.LogError("LaunchControl.RegisterCommand called before LaunchControl was initialized!");
                return;
            }

            LaunchControl.Instance.Commands.RegisterCommand(commandName, callback);
        }

        /// <summary>
        /// Get list of all registered command names
        /// </summary>
        public List<string> GetRegisteredCommands()
        {
            List<string> commandList = new List<string>();
            foreach (string key in _commands.Keys)
            {
                commandList.Add(key);
            }
            return commandList;
        }
    }
}
