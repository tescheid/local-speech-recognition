using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalSpeechRecognitionMaster.DataModels;
using NLog;

namespace LocalSpeechRecognitionMaster.Services
{ 
    /// <summary>
    /// Service for processing action MQTT requests .
    /// </summary>
    public class ActionProcessingService
    {
        //Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //Queue to store the requests
        private readonly Queue<MqttMessage> actionRequests;

        //Sync. Object used to signal new MQTTrequest
        private readonly AutoResetEvent newActionEvent;

        // Lock for synchronizing access to the actionRequests queue
        private readonly object actionRequestsLock;

        // Required services
        private readonly PythonService pythonService;
        private readonly JsonService jsonService;
        private readonly SoundService soundService;
        private readonly MqttService mqttServiceTx;
        private readonly AbsenceService absenceService;
        
        // Flag to track if last request was finished
        private bool lastRequestFinished = true;

        // Current actionrequest in processing
        private MqttMessage currentActionRequest;


        /// <summary>
        /// Constructor for ActionProcessingService.
        /// Initializes dependencies, setup the action processing queue.
        /// </summary>
        /// <param name="actionRequests">Queue of action requests.</param>
        /// <param name="newActionEvent">Event to signal new action requests.</param>
        /// <param name="actionRequestsLock">Lock for synchronizing access to the actionRequests queue.</param>
        /// <param name="pythonService">Service for interacting with speech recognition.</param>
        /// <param name="jsonService">Service for handling JSON .</param>
        /// <param name="soundService">Service for playing sounds.</param>
        /// <param name="mqttServiceTx">Service for sending MQTT messages.</param>
        /// <param name="absenceService">Service for handling absencemode.</param>
        public ActionProcessingService(
            Queue<MqttMessage> actionRequests,
            AutoResetEvent newActionEvent,
            object actionRequestsLock,
            PythonService pythonService,
            JsonService jsonService,
            SoundService soundService,
            MqttService mqttServiceTx,
            AbsenceService absenceService)
        {
            this.actionRequests = actionRequests;
            this.newActionEvent = newActionEvent;
            this.actionRequestsLock = actionRequestsLock;
            this.pythonService = pythonService;
            this.jsonService = jsonService;
            this.soundService = soundService;
            this.mqttServiceTx = mqttServiceTx;
            this.absenceService = absenceService;
        }

        /// <summary>
        /// Initializes the ActionProcessingService, starting a background thread processing the action requests.
        /// </summary>
        public void Init()
        {
            Thread t = new(ProcessActionRequests) { IsBackground = true };
            t.Start();
        }

        /// <summary>
        /// Continuously processes action requests from the queue.
        /// </summary>
        private void ProcessActionRequests()
        {
            while (true)
            {
                Logger.Info("Start waiting for Action request");
                // Wait for a new action signal
                newActionEvent.WaitOne();
                Logger.Debug("Received Trigger");

                // Process action request if queue not empty and last request finished
                if (actionRequests.Count > 0 && lastRequestFinished)
                {
                    Logger.Debug("Processing message");
                    lastRequestFinished = false;

                    // Dequeue action request
                    lock (actionRequestsLock)
                    {
                        currentActionRequest = actionRequests.Dequeue();
                        Logger.Debug($"Dequeued action request: {currentActionRequest.Device}, {currentActionRequest.Action}");
                    }

                    // Process the action based on device
                    if (currentActionRequest.Device == "Absenz" && (currentActionRequest.Action == "ein" || currentActionRequest.Action == "aus" || currentActionRequest.Action == "AskMode"))
                    {
                        ProcessAbsenceRequest(currentActionRequest.Device, currentActionRequest.Action);
                    }
                    else
                    {
                        ProcessDeviceAndAction(currentActionRequest.Device, currentActionRequest.Action);
                    }
                }
            }
        }

        /// <summary>
        /// Processes an action request.
        /// </summary>
        /// <param name="device">The device to execute the action on.</param>
        /// <param name="action">The action to execute.</param>
        public async void ProcessDeviceAndAction(string device, string action)
        {
            Logger.Info($"Processing device: {device}, action: {action}");
            if (device == "Action" && action == "ein")
            {
                Logger.Debug("Sending command: action");
                // Change speech recognition context
                await pythonService.SendCommand("action");

                // Clear the JSON file after sending the command
                jsonService.ClearJsonFile();

                // Play the action question sound
                await soundService.PlaySound($"action_question");
            }
            else if (action != "AskMode" && device != "Action")
            {
                Logger.Debug("Sending command: confirmation");

                // Change speech recognition context
                await pythonService.SendCommand("confirmation");

                // Add the device and action to the devices list
                Logger.Info($"Add {device}");
                Devices.AddDevice(device, action);
                Logger.Info($"Add {action}");
                Devices.AddActionToDevice(device, action);
                
                // Clear the JSON file after sending the command
                jsonService.ClearJsonFile();

                // Play the device action question sound
                await soundService.PlaySound($"{device}_{action}_question");
            }
        }

        /// <summary>
        /// Processes an absence request.
        /// </summary>
        /// <param name="device">Always "Absenz".</param>
        /// <param name="action">The action either "ein", "aus" or "AskMode").</param>
        public async void ProcessAbsenceRequest(string device, string action)
        {
            Logger.Debug($"Processing absence request: {device}, action: {action}");

            if (device == "Absenz " && action == "AskMode" && !absenceService.AbsenceMode)
            {
                Logger.Debug("Sending command: mode");
                await pythonService.SendCommand("mode");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"mode_question");
            }
            else if (device == "Absenz " && action == "ein" && !absenceService.AbsenceMode)
            {
                Logger.Debug("Sending command: date");
                await pythonService.SendCommand("date");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"absence_question");
            }
            else if (device == "Absenz " && action == "aus" || (action == "AskMode" && absenceService.AbsenceMode))
            {
                absenceService.AbsenceMode = false;
                await soundService.PlaySound($"absence_end");
                ExecuteAction("Absenz", "aus", true, DateTime.Now);
                Reset();
            }
        }

        /// <summary>
        /// Executes action, optionally publishing MQTT message.
        /// </summary>
        /// <param name="device">The device to execute the action on.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="confirmed">Action was confirmed.</param>
        /// <param name="EndDate">Optional end date.</param>
        public async void ExecuteAction(string device, string action, bool confirmed, DateTime? EndDate = null)
        {
            Logger.Debug($"Executing action: {action} on device: {device}, confirmed: {confirmed}");
            string actionAsJson;

            // Play confirmation or cancel sound based on confirmation
            if (confirmed)
            {
                await soundService.PlaySound($"{device}_{action}_confirmed");
                Logger.Info($"Action confirmed: {device} - {action}");
            }
            else
            {
                await soundService.PlaySound($"{device}_{action}_cancelled");
                Logger.Info($"Action cancelled: {device} - {action}");

            }
            // Publish absence message if device "Absenz" and end date provided
            if (device == "Absenz" && EndDate != null)
            {
                actionAsJson = MqttService.GenerateAbsenceMessage(device, action, EndDate.Value);
                Logger.Info($"Absence end date: {EndDate.Value}");
                mqttServiceTx.PublishMessage(actionAsJson);
            }
            else
            {
                // Publish action message if confirmed
                actionAsJson = MqttService.GenerateActionMessage(device, action);
                if (confirmed != false)
                {
                    mqttServiceTx.PublishMessage(actionAsJson);
                }
            }

            Reset();
        }

        /// <summary>
        /// Resets ActionProcessingService, triggers processing of pending requests.
        /// </summary>
        public void Reset()
        {
            // Mark the last request as finished
            lastRequestFinished = true;

            // Clear the JSON file
            jsonService.ClearJsonFile();

            lock (actionRequestsLock)
            {
                // Check for pending requests and signal event
                if (actionRequests.Count > 0)
                {
                    Logger.Debug("There are pending action requests, setting the event.");
                    newActionEvent.Set();
                }
            }
        }

        /// <summary>
        /// Gets current action request.
        /// </summary>
        /// <returns>Current MqttMessage action request.</returns>
        public MqttMessage GetCurrentActionRequest()
        {
            return currentActionRequest;
        }
    }
}
