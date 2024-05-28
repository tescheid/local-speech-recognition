using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using NLog;

namespace LocalSpeechRecognitionMaster.Services
{
    public class DeviceManagementService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly SoundService soundService;

        public DeviceManagementService(SoundService soundService)
        {
            this.soundService = soundService;
        }

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

            Devices.AddDevice("Taster", "gedrückt");

            Devices.AddDevice("Absenz", "ein");
            Devices.AddActionToDevice("Absenz", "aus");

            Logger.Info("Devices and actions initialized.");
        }

        public void GenerateSoundsForDeviceAction(string device, string action)
        {
            Logger.Info($"Generating sounds for device: {device}, action: {action}");
            soundService.GenerateSoundFilesForAction(device, action);
        }
    }
}

