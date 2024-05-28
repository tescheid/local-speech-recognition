using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Gpio;
using System.Threading;
using Microsoft.VisualBasic;
using Common;
using Common.DataModels;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;
using NLog;
using System.Timers;

namespace LocalSpeechRecognitionMaster.Services
{  
    public class AbsenceService
    {
        private GpioController controller;
        private readonly int buttonPin = 17; 

        private readonly MqttService mqttService;
       

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private System.Timers.Timer debounceTimer;
        private readonly object debounceLock = new();
        private bool _absenceMode = false;
        private readonly object _lock = new();
        public bool AbsenceMode
        {
            get
            {
                lock (_lock)
                {
                    return _absenceMode;
                }
            }
            set
            {
                lock (_lock)
                {
                    _absenceMode = value;
                    Logger.Info($"Absence mode set to: {_absenceMode}");
                }
            }
        }
        public AbsenceService(MqttService mqttService)
        {
            this.mqttService = mqttService;
        }

        public void Init()
        {
            Logger.Info("Initializing AbsenceService..."); 
            
            controller = new GpioController();
            controller.OpenPin(buttonPin, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(buttonPin, PinEventTypes.Falling, OnButtonPressed);

            debounceTimer = new System.Timers.Timer(200){AutoReset = false}; // 200ms debounce time
            debounceTimer.Elapsed += DebounceElapsed;
        }

        private void OnButtonPressed(object sender, PinValueChangedEventArgs e)
        {
            if (e.ChangeType == PinEventTypes.Falling)
            {
                lock (debounceLock)
                {
                    if (!debounceTimer.Enabled)
                    {
                        Logger.Info("Taster gedrückt -> Frage nach dem Modus...");
                        mqttService.PublishMessage(MqttService.GenerateActionMessage("Absence", "AskMode"));
                        debounceTimer.Start(); // Start debounce timer
                    }
                }
            }
        }

        private void DebounceElapsed(object sender, ElapsedEventArgs e)
        {
            lock (debounceLock)
            {
                debounceTimer.Stop();
                Logger.Debug("Debounce timer elapsed.");
            }
        }

    }
}
