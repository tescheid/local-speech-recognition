using LocalSpeechRecognitionMaster.Services;
using LocalSpeechRecognitionMaster.DataModels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using uPLibrary.Networking.M2Mqtt.Messages;
using NLog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;


namespace LocalSpeechRecognitionMaster
{
    /// <summary>
    /// Controlling class, Leader of LSR
    /// </summary>
    public class MasterService
    {  
        //Create Logger Instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //Create Instance of all Services
        private MqttService mqttServiceRx;
        private MqttService mqttServiceTx;
        private JsonService jsonService;
        private SoundService soundService;
        private PythonService pythonService;
        private AbsenceService absenceService;
        private DeviceManagementService deviceManagementService;
        private InitializationService initializationService;
        private ActionProcessingService actionProcessingService;
        
        //Configuration Paths and Settings
        public static IConfiguration Configuration { get; private set; }
        private readonly string _piperPath;
        private readonly string _modelPath;
        private readonly string _intentOutputPath;
        private readonly int _maxRetries;

        //Field for unanswered or unrecognized retries
        private int retryCount = 0;

        //Queue to store the requests
        readonly Queue<MqttMessage> actionRequests = new();

        //Sync. Object used to signal new MQTTrequest
        private readonly AutoResetEvent newActionEvent = new(false);

        // Lock for synchronizing access to the actionRequests queue
        private readonly object _actionRequestsLock = new();
          

        public MasterService()
        {
            Logger.Debug("Starting.");

            // Load Configuration from JSON file
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("LSR_Settings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();

            // Load paths and settings from configuration
            _modelPath = Configuration["Paths:ModelPath"];
            _piperPath = Configuration["Paths:PiperPath"];
            _intentOutputPath = Configuration["Paths:IntentOutputPath"];
            _maxRetries = int.Parse(Configuration["MasterConfig:maxRetries"]);
        }


        /// <summary>
        /// Initializes MasterService, instantiating all necessary services,
        /// setting up event handlers,  performing initial service configurations.
        /// </summary>
        public void Init()
        {
            Logger.Info("Initializing MasterService...");
            
            // Instantiate all services
            pythonService = new PythonService(Configuration);
            mqttServiceRx = new MqttService(Configuration["Topics:TopicTx"], Configuration);
            mqttServiceTx = new MqttService(Configuration["Topics:TopicRx"], Configuration);
            jsonService = new JsonService(_intentOutputPath);
            soundService = new SoundService(Configuration);
            absenceService = new AbsenceService(mqttServiceRx, Configuration);
            deviceManagementService = new DeviceManagementService(soundService);
            actionProcessingService = new ActionProcessingService(actionRequests, newActionEvent, _actionRequestsLock, pythonService, jsonService, soundService, mqttServiceTx, absenceService);

            // Set up event handlers
            mqttServiceRx.MessageReceivedEvent += MqttActionRequested;
            jsonService.FileChangedEvent += AnswerReceived;
            soundService.AudioPlayedEvent += QuestionSoundPlayed;
            Devices.OnNewActionAdded += deviceManagementService.GenerateSoundsForDeviceAction;

            // Initialize all services
            initializationService = new InitializationService(pythonService, mqttServiceRx, mqttServiceTx, jsonService, soundService, absenceService, actionProcessingService);
            initializationService.Initialize();

            Logger.Info("Application finally started.");
        }


        /// <summary>
        /// Handles incoming MQTT action requests. Deserializes the request message,
        /// enqueues the action request, and signals the new action event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MQTT message publish event arguments containing the action request message.</param>
        private void MqttActionRequested(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string receivedActionRequestJson = Encoding.UTF8.GetString(e.Message);
                MqttMessage receivedActionRequest = JsonSerializer.Deserialize<MqttMessage>(receivedActionRequestJson);
                lock (_actionRequestsLock)
                {
                    // Add the action request to the queue
                    actionRequests.Enqueue(receivedActionRequest);
                    Logger.Debug($"Enqueued action request: {receivedActionRequest.Device}, {receivedActionRequest.Action}");
                }
                Logger.Info($"{receivedActionRequest.Device} action requested: {receivedActionRequest.Action}");
                newActionEvent.Set(); // Signal that a new action has been enqueued
                Logger.Debug($"Event Set");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"This was no valid action request.");
            }
        }


        /// <summary>
        /// Handles calculation and confirmation of absence duration based on speech recognition data.
        /// If day and month slots provided, it calculates the absence end date, sends an MQTT message,
        /// confirms the absence, plays a success sound, resets the processing state.
        /// </summary>
        /// <param name="data">The speech recognition data model containing slots for day and month.</param>
        private async void HandleAbsenceDuration(SpeechRecognitionDataModel data)
        {
            if (data.Slots.TryGetValue("Day", out string day) &&
                data.Slots.TryGetValue("Month", out string month))
            {
                try
                {
                    DateTime currentDate = DateTime.Now;
                    int targetYear = currentDate.Year;

                    // Convert month and day
                    int targetMonth = absenceService.GetMonthFromName(month);
                    int targetDay = absenceService.ConvertOrdinalToNumber(day);
                    
                    // Raise year +1 if date already passed
                    if (new DateTime(targetYear, targetMonth, targetDay) < currentDate)
                    {
                        targetYear++;
                    }

                    // Create end date for absence
                    DateTime absenceEndDate = new(targetYear, targetMonth, targetDay);
                    Logger.Info($"Absence duration calculated: {absenceEndDate}");
                    
                    // send MQTT Message
                    string absenceMessage = MqttService.GenerateAbsenceMessage("Absenz", "ein", absenceEndDate);
                    mqttServiceTx.PublishMessage(absenceMessage);

                    // Confirm absence
                    absenceService.AbsenceMode = true;
                    Logger.Info("Absence end date sent to HomeAssistant.");

                    // Confirm succesful conversion
                    await soundService.PlaySound("success");
                    TerminalService.RunCmdAndReturnOutput($"echo 'Abwesenheit für den {day} {month} bestätigt' |" +
                        $" {_piperPath} -m {_modelPath}" +
                        " --output_file temp.wav && aplay temp.wav && rm temp.wav");

                    // Reset processing
                    actionProcessingService.Reset();
                    retryCount = 0;
                }
                catch (Exception ex)
                {
                    // Provide unsuccessful conversion
                    Logger.Error(ex, "Fehler beim Berechnen der Abwesenheitsdauer.");
                    TerminalService.RunCmdAndReturnOutput($"echo 'Bitte um erneute Angabe.' |" +
                        $" {_piperPath} -m {_modelPath}" +
                        " --output_file temp.wav && aplay temp.wav && rm temp.wav");
                    // Start over
                    actionProcessingService.Reset();
                    mqttServiceRx.PublishMessage(MqttService.GenerateActionMessage("Absenz", "ein"));
                }
            }
            else
            {
                Logger.Warn("Slot(s) missing for Duration.");
            }
        }


        /// <summary>
        /// Handles execution device action based on speech recognition data.
        /// If action slot provided and action exists for device,
        /// executes the action. If action does not exist, plays a "not found" sound, restarts speech recognition.
        /// </summary>
        /// <param name="data">The speech recognition data model containing slots for action and device.</param>
        private async void HandleDeviceAction(SpeechRecognitionDataModel data)
        {
            if (data.Slots.TryGetValue("Action", out string action))
            {
                if (Devices.ActionExistsForDevice(data.Intent, action))
                {
                    // Execute the action if it exists
                    actionProcessingService.ExecuteAction(data.Intent, action, true);
                    retryCount = 0;
                }
                else
                {
                    // Play Sound indicating Action not found, restart speechRecognition
                    Logger.Warn($"Action {action} for device {data.Intent} not found.");
                    await pythonService.SendCommand("action");
                    await soundService.PlaySound("action_not_found");
                    retryCount = 0;
                }
            }
            else
            {
                Logger.Warn("Slot(s) missing for action.");
            }
        }


        /// <summary>
        /// Handles modeselection based on speech recognition data.
        /// If intent is "Absence",processes the absence mode.
        /// If intent is "Action",processes the device action mode.
        /// </summary>
        /// <param name="data">The speech recognition data model containing the intent for mode selection.</param>
        private void HandleModeSelection(SpeechRecognitionDataModel data)
        {
            if (data.Intent == "Absence")
            {
                // Process absence mode selection
                actionProcessingService.ProcessAbsenceRequest("Absenz", "ein");
            }
            else if (data.Intent == "Action")
            {
                // Process device action mode selection
                actionProcessingService.ProcessDeviceAndAction("Action", "ein");
            }
        }


        /// <summary>
        /// Handles no answer received after a speech recognition prompt.
        /// If maximum retry count reached, cancelling action, notify user.
        /// Otherwise, ssks user to repeat 
        /// </summary>
        private async void HandleNoAnswer()
        {
            if (retryCount > _maxRetries)
            {
                // Cancel due to maximum retries reached
                Logger.Warn("No answer received.");
                await soundService.PlaySound("no_answer");
                // Execute action cancelled
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, false);
            }
            else
            {
                // Ask user to repeat
                Logger.Warn("Answer not recognized, please repeat");
                await soundService.PlaySound("not_recognized");

                if (actionProcessingService.GetCurrentActionRequest().Device == "Absenz")
                {
                    // Retry processing absence request
                    actionProcessingService.ProcessAbsenceRequest("Absenz", actionProcessingService.GetCurrentActionRequest().Action);
                }
                else {
                    // Retry processing device action
                    actionProcessingService.ProcessDeviceAndAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action);
                }
            }
        }


        /// <summary>
        /// Handles the confirmation of action based on speech recognition data.
        /// If intent "Ja", confirms and executes the action.
        /// If intent "Nein", denies and cancels the action.
        /// </summary>
        /// <param name="data">The speech recognition data model containing the intent for confirmation.</param>
        private void HandleConfirmation(SpeechRecognitionDataModel data)
        {
            if (data.Intent == "Ja")
            {
                // Execute action confirmed
                Logger.Info("Action confirmed");
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, true);
                retryCount = 0;
            }
            else if (data.Intent == "Nein")
            {
                // Execute action cancelled
                Logger.Info("Action denied");
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, false);
                retryCount = 0;
            }
        }


        /// <summary>
        /// Handles speech recognition data and routes intent to handler.
        /// Depending on intent, processes absence duration, device actions, mode selection,
        /// confirmation, asks to repeat if no answer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="data">The speech recognition data model containing the intent and slots.</param>
        private void AnswerReceived(object sender, SpeechRecognitionDataModel data)
        {
            Logger.Info($"Received intent: {data.Intent}");

            try
            {
                if(data.Intent == "noAnswer")
                {
                    // Ask again
                    HandleNoAnswer();
                }
                else if (data.Intent == "Dauer" && data.Slots != null)
                {
                    // Process absence answer
                    HandleAbsenceDuration(data);
                }
                else if (Devices.DeviceExists(data.Intent) && data.Slots != null)
                {
                    // Process action command
                    HandleDeviceAction(data);
                }
                else if (data.Intent == "Absence" || data.Intent == "Action")
                {
                    // Process mode selection answer
                    HandleModeSelection(data);
                }
                else if(data.Intent != "noAnswer")
                {
                    // Process confirmation answer
                    HandleConfirmation(data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing answer.");
            }
        }
        
        
        /// <summary>
        /// Handles the event when a question sound is played.
        /// Plays a confirmation sound, logs the event, and starts speech recognition.
        /// Increments the retry count.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The audio event arguments.</param>
        private async void QuestionSoundPlayed(object sender, AudioEventArgs e)
        {
            // Confirmation sound
            await soundService.PlaySound("90s-game-ui");
            Logger.Info("Sound played succesfully. Starting speech recognition");
            
            // Start speech recognition
            Logger.Debug("Sending command: start");
            await pythonService.SendCommand("start");
            retryCount++;
        }
    }
}
