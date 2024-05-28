using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.DataModels;
using NLog;
using Common;

namespace LocalSpeechRecognitionMaster.Services
{
    public class ActionProcessingService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Queue<MqttMessage> actionRequests;
        private readonly AutoResetEvent newActionEvent;
        private readonly object actionRequestsLock;
        private readonly PythonService pythonService;
        private readonly JsonService jsonService;
        private readonly SoundService soundService;
        private readonly MqttService mqttServiceTx;
        private readonly AbsenceService absenceService;
        private bool lastRequestFinished = true;
        private MqttMessage currentActionRequest;

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

        public void Init()
        {
            Thread t = new(ProcessActionRequests) { IsBackground = true };
            t.Start();
        }

        private void ProcessActionRequests()
        {
            while (true)
            {
                Logger.Info("Start waiting for Action request");
                newActionEvent.WaitOne();
                Logger.Debug("Received Trigger");

                if (actionRequests.Count > 0 && lastRequestFinished)
                {
                    Logger.Debug("Processing message");
                    lastRequestFinished = false;

                    lock (actionRequestsLock)
                    {
                        currentActionRequest = actionRequests.Dequeue();
                        Logger.Debug($"Dequeued action request: {currentActionRequest.Device}, {currentActionRequest.Action}");
                    }

                    if (currentActionRequest.Device == "Absenz" || (currentActionRequest.Device == "Absence" &&
                        (currentActionRequest.Action == "On" || currentActionRequest.Action == "Off" || currentActionRequest.Action == "AskMode")))
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

        public async void ProcessDeviceAndAction(string device, string action)
        {
            Logger.Info($"Processing device: {device}, action: {action}");
            if (device == "Action" && action == "On")
            {
                Logger.Debug("Sending command: action");
                await pythonService.SendCommand("action");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"action_question");
            }
            else if (action != "AskMode" && device != "Action")
            {
                Logger.Debug("Sending command: confirmation");
                await pythonService.SendCommand("confirmation");
                
                Logger.Info($"Add {device}");
                Devices.AddDevice(device, action);
                Logger.Info($"Add {action}");
                Devices.AddActionToDevice(device, action);

                jsonService.ClearJsonFile();
                await soundService.PlaySound($"{device}_{action}_question");
            }
        }

        public async void ProcessAbsenceRequest(string device, string action)
        {
            Logger.Debug($"Processing absence request: {device}, action: {action}");
            if (string.IsNullOrEmpty(device))
            {
                Logger.Error("Device is null or empty.");
                throw new ArgumentException($"'{nameof(device)}' cannot be null or empty.", nameof(device));
            }

            if (action == "AskMode" && !absenceService.AbsenceMode)
            {
                Logger.Debug("Sending command: mode");
                await pythonService.SendCommand("mode");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"mode_question");
            }
            else if (action == "ein")
            {
                Logger.Debug("Sending command: date");
                await pythonService.SendCommand("date");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"absence_question");
            }
            else if (action == "aus" || (action == "AskMode" && absenceService.AbsenceMode))
            {
                absenceService.AbsenceMode = false;
                await soundService.PlaySound($"absence_end");
                Reset();
            }
        }

        public async void ExecuteAction(string device, string action, bool confirmed, DateTime? EndDate = null)
        {
            Logger.Debug($"Executing action: {action} on device: {device}, confirmed: {confirmed}");
            string actionAsJson;

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

            if (device == "Absenz")
            {
                actionAsJson = MqttService.GenerateAbsenceMessage(device, action, EndDate.Value);
                Logger.Info($"Absence end date: {EndDate.Value}");
                mqttServiceTx.PublishMessage(actionAsJson);
            }
            else
            {

                actionAsJson = MqttService.GenerateActionMessage(device, action);
                if (confirmed != false)
                {
                    mqttServiceTx.PublishMessage(actionAsJson);
                }
            }

            Reset();
        }

        public void Reset()
        {
            lastRequestFinished = true;
            jsonService.ClearJsonFile();

            lock (actionRequestsLock)
            {
                if (actionRequests.Count > 0)
                {
                    Logger.Debug("There are pending action requests, setting the event.");
                    newActionEvent.Set();
                }
            }
        }
        public MqttMessage GetCurrentActionRequest()
        {
            return currentActionRequest;
        }
    }
}
