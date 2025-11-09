using System;
using System.Reflection;
using UnityEngine;

namespace Ostranauts.Bit
{
    /// <summary>
    /// Helper class for outputting messages to the in-game console
    /// </summary>
    public static class DevConsole
    {
        private static FieldInfo _myLogField;
        private static PropertyInfo _instanceProperty;
        private static bool _initialized = false;

        /// <summary>
        /// Output a message to the in-game console (ConsoleToGUI)
        /// </summary>
        /// <param name="message">The message to output</param>
        public static void Output(string message)
        {
            try
            {
                if (!_initialized)
                {
                    Initialize();
                }

                if (object.ReferenceEquals(_instanceProperty, null) || object.ReferenceEquals(_myLogField, null))
                {
                    // Fallback to Unity console
                    Debug.Log("[LaunchControl Console] " + message);
                    return;
                }

                // Get ConsoleToGUI.instance
                object consoleInstance = _instanceProperty.GetValue(null, null);
                if (consoleInstance == null)
                {
                    Debug.Log("[LaunchControl Console] " + message);
                    return;
                }

                // Get current log string
                string currentLog = _myLogField.GetValue(consoleInstance) as string;
                if (currentLog == null)
                {
                    currentLog = "";
                }

                // Append new message
                string newLog = currentLog + "\n" + message;

                // Set updated log
                _myLogField.SetValue(consoleInstance, newLog);

                // Also log to BepInEx for debugging
                LaunchControlPlugin.Logger.LogInfo("[Console] " + message);
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to output to console: {ex.Message}");
                Debug.Log("[LaunchControl Console] " + message);
            }
        }

        private static void Initialize()
        {
            try
            {
                Type consoleToGUIType = Type.GetType("ConsoleToGUI, Assembly-CSharp");
                if (object.ReferenceEquals(consoleToGUIType, null))
                {
                    LaunchControlPlugin.Logger.LogWarning("Could not find ConsoleToGUI type");
                    _initialized = true;
                    return;
                }

                // Get the static instance property
                _instanceProperty = consoleToGUIType.GetProperty("instance", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (object.ReferenceEquals(_instanceProperty, null))
                {
                    LaunchControlPlugin.Logger.LogWarning("Could not find ConsoleToGUI.instance property");
                    _initialized = true;
                    return;
                }

                // Get the myLog field
                _myLogField = consoleToGUIType.GetField("myLog", 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                if (object.ReferenceEquals(_myLogField, null))
                {
                    LaunchControlPlugin.Logger.LogWarning("Could not find ConsoleToGUI.myLog field");
                    _initialized = true;
                    return;
                }

                LaunchControlPlugin.Logger.LogInfo("DevConsole initialized successfully");
                _initialized = true;
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"Failed to initialize DevConsole: {ex.Message}");
                _initialized = true;
            }
        }
    }
}

