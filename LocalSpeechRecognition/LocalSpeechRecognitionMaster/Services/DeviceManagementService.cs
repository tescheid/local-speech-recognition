using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using LocalSpeechRecognitionMaster.DataModels;

namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for managing devices and their actions.
    /// </summary>
    public class DeviceManagementService
    {
        //Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //Required services
        private readonly SoundService soundService;

        /// <summary>
        /// Constructor for the DeviceManagementService.
        /// Initializes sound service.
        /// </summary>
        /// <param name="soundService">Service for generating sound files.</param>
        public DeviceManagementService(SoundService soundService)
        {
            this.soundService = soundService;
        }

        /// <summary>
        /// Initializes default devices and actions.
        /// </summary>
        public static void Init()
        {
            Logger.Info("Initializing devices and actions...");

            string light = "Licht";
            Devices.AddDevice(light, "ein");
            Devices.AddActionToDevice(light, "aus");
            Devices.AddActionToDevice(light, "erhöhe");
            Devices.AddActionToDevice(light, "dimme");

            string thermostat = "Heizung";
            Devices.AddDevice(thermostat, "ein");
            Devices.AddActionToDevice(thermostat, "aus");
            Devices.AddActionToDevice(thermostat, "erhöhe");
            Devices.AddActionToDevice(thermostat, "senke");

            string speaker = "Lautsprecher";
            Devices.AddDevice(speaker, "starte");
            Devices.AddActionToDevice(speaker, "pausiere");
            Devices.AddActionToDevice(speaker, "erhöhe");
            Devices.AddActionToDevice(speaker, "senke");

            string tv = "Fernseher";
            Devices.AddDevice(tv, "ein");
            Devices.AddActionToDevice(tv, "aus");
            Devices.AddActionToDevice(tv, "erhöhe");
            Devices.AddActionToDevice(tv, "senke");

            string alarm = "Alarm";
            Devices.AddDevice(alarm, "ein");
            Devices.AddActionToDevice(alarm, "aus");

            string blinds = "Beschattung";
            Devices.AddDevice(blinds, "hoch");
            Devices.AddActionToDevice(blinds, "runter");

            Devices.AddDevice("Absenz", "ein");
            Devices.AddActionToDevice("Absenz", "aus");

            Logger.Info("Devices and actions initialized.");
        }

        /// <summary>
        /// Generates sound files for device action.
        /// </summary>
        /// <param name="device">Device to generate sound files for.</param>
        /// <param name="action">Action to generate sound files for.</param>
        public void GenerateSoundsForDeviceAction(string device, string action)
        {
            Logger.Info($"Generating sounds for device: {device}, action: {action}");
            soundService.GenerateSoundFilesForAction(device, action);
        }
    }
}

