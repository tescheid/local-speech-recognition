using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Gpio;
using System.Threading;
using Microsoft.VisualBasic;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;
using NLog;
using System.Timers;
using Microsoft.Extensions.Configuration;

namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for managing absence mode and handling GPIO button presses.
    /// </summary>
    public class AbsenceService
    {
        private GpioController controller;
        private readonly MqttService mqttService;

        // Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // GPIO pin numbers for buttons (from config)
        private readonly int _buttonPin;
        private readonly int _buttonPin_WM8960;
              
        // Timer and lock for debounce button presses
        private System.Timers.Timer debounceTimer;
        private readonly object debounceLock = new();

        // State of absence mode
        private bool _absenceMode = false;

        // Lock object for thread safety
        private readonly object _lock = new();

        // Dictionaries for converting ordinal and month names to numbers
        private static readonly Dictionary<string, int> OrdinalToNumber = new()
        {
            {"ersten", 1}, {"zweiten", 2}, {"dritten", 3}, {"vierten", 4}, {"fünften", 5}, {"sechsten", 6}, {"siebten", 7}, {"achten", 8},
            {"neunten", 9}, {"zehnten", 10}, {"elften", 11}, {"zwölften", 12},
            {"dreizehnten", 13}, {"vierzehnten", 14}, {"fünfzehnten", 15},
            {"sechzehnten", 16}, {"siebzehnten", 17}, {"achtzehnten", 18},
            {"neunzehnten", 19}, {"zwanzigsten", 20}, {"einundzwanzigsten", 21},
            {"zweiundzwanzigsten", 22}, {"dreiundzwanzigsten", 23}, {"vierundzwanzigsten", 24},
            {"fünfundzwanzigsten", 25}, {"sechsundzwanzigsten", 26}, {"siebenundzwanzigsten", 27},
            {"achtundzwanzigsten", 28}, {"neunundzwanzigsten", 29}, {"dreissigsten", 30},
            {"einunddreissigsten", 31}
        };

        private static readonly Dictionary<string, int> monthNames = new()
        {
             {"Januar", 1}, {"Februar", 2}, {"März", 3}, {"Mu00e4rz",3}, {"April", 4},
             {"Mai", 5}, {"Juni", 6}, {"Juli", 7}, {"August", 8},
             {"September", 9}, {"Oktober", 10}, {"November", 11}, {"Dezember", 12}
        };

        /// <summary>
        /// Constructor for the AbsenceService.
        /// Initializes the MQTT service and load GPIO button pins.
        /// </summary>
        /// <param name="mqttService">The MQTT service to use for publishing messages.</param>
        /// <param name="configuration">Configuration object for loading settings.</param>
        public AbsenceService(MqttService mqttService, IConfiguration configuration)
        {
            this.mqttService = mqttService;
            _buttonPin = int.Parse(configuration["ButtonPins:absencePin"]);
            _buttonPin_WM8960 = int.Parse(configuration["ButtonPins:absencePin"]);
        }

        /// <summary>
        /// Property to get or set the absence state.
        /// </summary>
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

        /// <summary>
        /// Initializes AbsenceService by setup of GPIO and debounve timer.
        /// </summary>
        public void Init()
        {
            Logger.Info("Initializing AbsenceService...");
            
            // Initialize GPIO controller
            controller = new GpioController();
            controller.OpenPin(_buttonPin, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Falling, OnButtonPressed);
            controller.OpenPin(_buttonPin_WM8960, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(_buttonPin_WM8960, PinEventTypes.Falling, OnButtonPressedMode);

            // Setup debounce
            debounceTimer = new System.Timers.Timer(700){AutoReset = false};
            debounceTimer.Elapsed += DebounceElapsed;
        }

        /// <summary>
        /// Callback for handling button press on absence button.
        /// Publishes an absence request via MQTT.
        /// </summary>
        private void OnButtonPressed(object sender, PinValueChangedEventArgs e)
        {
            if (e.ChangeType == PinEventTypes.Falling)
            {
                lock (debounceLock)
                {
                    if (!debounceTimer.Enabled)
                    {
                        Logger.Info("Button pressed");
                        mqttService.PublishMessage(MqttService.GenerateActionMessage("Absenz", "ein"));
                        debounceTimer.Start(); 
                    }
                }
            }
        }

        /// <summary>
        /// Callback for handling button presses on the absence pin.
        /// Publishes a mode request via MQTT.
        /// </summary>
        private void OnButtonPressedMode(object sender, PinValueChangedEventArgs e)
        {
            if (e.ChangeType == PinEventTypes.Falling)
            {
                lock (debounceLock)
                {
                    if (!debounceTimer.Enabled)
                    {
                        Logger.Info("Button pressed");
                        mqttService.PublishMessage(MqttService.GenerateActionMessage("Absenz", "AskMode"));
                        debounceTimer.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Callback for debounce timer elapsed.
        /// Stops the debounce timer and log.
        /// </summary>
        private void DebounceElapsed(object sender, ElapsedEventArgs e)
        {
            lock (debounceLock)
            {
                debounceTimer.Stop();
                Logger.Debug("Debounce timer elapsed.");
            }
        }

        /// <summary>
        /// Converts a month name to corresponding number.
        /// </summary>
        /// <param name="month">Name of the month.</param>
        /// <returns>Number of the month.</returns>
        public int GetMonthFromName(string month)
        {
            if (monthNames.TryGetValue(month, out int monthNumber))
            {
                Logger.Debug($"Month conversion: {month} - {monthNumber}");
                return monthNumber;
            }

            Logger.Error($"Invalid month name: {month}");
            throw new ArgumentException("Invalid month.");
        }

        /// <summary>
        /// Converts an ordinal number to corresponding integer.
        /// </summary>
        /// <param name="ordinal">The ordinal number as a string.</param>
        /// <returns>The integer value of ordinal number.</returns>
        public int ConvertOrdinalToNumber(string ordinal)
        {
            if (OrdinalToNumber.TryGetValue(ordinal.ToLower(), out int number))
            {
                Logger.Debug($"Ordinal to number conversion: {ordinal} - {number}");
                return number;
            }

            Logger.Error($"Invalid ordinal: {ordinal}");
            throw new ArgumentException($"Invalid ordinal: {ordinal}");
        }
    }
}
