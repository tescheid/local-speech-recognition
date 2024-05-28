using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster.DataModels
{
    public static class Devices
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        
        private readonly static Dictionary<string, HashSet<string>> deviceActions = new();
        public static event Action<string, string> OnNewActionAdded;

        public static void AddDevice(string device, string action)
        {
            Logger.Debug($"AddDevice Start: {device}, {action}");
            if (!deviceActions.ContainsKey(device))
            {
                deviceActions[device] = new HashSet<string>();
                Actions.AddAction(action);
                Logger.Debug($"Device {device} added with initial action {action}.");
            }
            AddActionToDevice(device, action);
            Logger.Debug($"AddDevice End: {device}, {action}");
        }

        public static void AddActionToDevice(string device, string action)
        {
            Logger.Debug($"AddAction Start: {device}, {action}");
            if (deviceActions.ContainsKey(device) && !deviceActions[device].Contains(action))
            {
                deviceActions[device].Add(action);
                Logger.Debug($"Action {action} added to device {device}.");
                OnNewActionAdded?.Invoke(device, action);
            }
            Logger.Debug($"AddAction End: {device}, {action}");
        }

        public static bool DeviceExists(string device)
        {
            bool exists = deviceActions.ContainsKey(device);
            Logger.Debug($"DeviceExists check: {device} - {exists}");
            return exists;
        }

        public static bool ActionExistsForDevice(string device, string action)
        {
            bool exists = deviceActions.ContainsKey(device) && deviceActions[device].Contains(action);
            Logger.Debug($"ActionExistsForDevice check: {device}, {action} - {exists}");
            return exists;
        }
    }
}
