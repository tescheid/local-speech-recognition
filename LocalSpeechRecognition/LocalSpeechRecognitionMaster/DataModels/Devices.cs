using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace LocalSpeechRecognitionMaster.DataModels
{
    /// <summary>
    /// Static class for managing devices and actions.
    /// </summary>
    public static class Devices
    {
        //Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        // Dictionary to store devices and actions
        private readonly static Dictionary<string, HashSet<string>> deviceActions = new();

        // Event triggered when a new action is added, used for Generating Sounds
        public static event Action<string, string> OnNewActionAdded;

        /// <summary>
        /// Add new device initial action.
        /// If device already existing, adds the action to device.
        /// </summary>
        /// <param name="device">Name of device.</param>
        /// <param name="action">Action for device.</param>
        public static void AddDevice(string device, string action)
        {
            Logger.Debug($"AddDevice Start: {device}, {action}");

            // Add device with a new action set if not existing
            if (!deviceActions.ContainsKey(device))
            {
                deviceActions[device] = new HashSet<string>();

                // Add action in Actions list (Actionslist not used anymore)
                Actions.AddAction(action);
                Logger.Debug($"Device {device} added with initial action {action}.");
            }
            // Add action to device action set
            AddActionToDevice(device, action);
            Logger.Debug($"AddDevice End: {device}, {action}");
        }

        /// <summary>
        /// Add action to existing device.
        /// Triggers OnNewActionAdded for sound generating.
        /// </summary>
        /// <param name="device">Name of device.</param>
        /// <param name="action">Action to add.</param>
        public static void AddActionToDevice(string device, string action)
        {
            Logger.Debug($"AddAction Start: {device}, {action}");
            if (deviceActions.ContainsKey(device) && !deviceActions[device].Contains(action))
            {
                // Add action to device action set if not existing
                deviceActions[device].Add(action);
                Logger.Debug($"Action {action} added to device {device}.");

                // Trigger the new action event to generate the sounds
                OnNewActionAdded?.Invoke(device, action);
            }
            Logger.Debug($"AddAction End: {device}, {action}");
        }

        /// <summary>
        /// Checks if device existing.
        /// </summary>
        /// <param name="device">Name of device to check.</param>
        /// <returns>True if device existing, else false.</returns>
        public static bool DeviceExists(string device)
        {
            bool exists = deviceActions.ContainsKey(device);
            Logger.Debug($"DeviceExists check: {device} - {exists}");
            return exists;
        }

        /// <summary>
        /// Checks if action existing for device.
        /// </summary>
        /// <param name="device">Name of device.</param>
        /// <param name="action">Action to check for the device.</param>
        /// <returns>True if action existing, else false.</returns>
        public static bool ActionExistsForDevice(string device, string action)
        {
            bool exists = deviceActions.ContainsKey(device) && deviceActions[device].Contains(action);
            Logger.Debug($"ActionExistsForDevice check: {device}, {action} - {exists}");
            return exists;
        }
    }
}
