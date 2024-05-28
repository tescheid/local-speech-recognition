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


namespace LocalSpeechRecognitionMaster
{
    public class MasterService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MqttService mqttServiceRx;
        private MqttService mqttServiceTx;
        private JsonService jsonService;
        private SoundService soundService;
        private PythonService pythonService;
        private AbsenceService absenceService;
        private DeviceManagementService deviceManagementService;
        private InitializationService initializationService;
        private ActionProcessingService actionProcessingService;

        private readonly int maxRetries = 3;
        private int retryCount = 0;

        private string mqttBrokerUsername = "";
        private string mqttBrokerPassword = "";

        readonly Queue<MqttMessage> actionRequests = new();
        private readonly AutoResetEvent newActionEvent = new(false);
        private readonly object _actionRequestsLock = new();

        // Mapping von deutschen Ordinalzahlen auf numerische Werte
        private static readonly Dictionary<string, int> OrdinalToNumber = new()
        {
            {"ersten", 1}, {"zweiten", 2}, {"dritten", 3}, {"vierten", 4},
            {"fünften", 5}, {"sechsten", 6}, {"siebten", 7}, {"achten", 8},
            {"neunten", 9}, {"zehnten", 10}, {"elften", 11}, {"zwölften", 12},
            {"dreizehnten", 13}, {"vierzehnten", 14}, {"fünfzehnten", 15},
            {"sechzehnten", 16}, {"siebzehnten", 17}, {"achtzehnten", 18},
            {"neunzehnten", 19}, {"zwanzigsten", 20}, {"einundzwanzigsten", 21},
            {"zweiundzwanzigsten", 22}, {"dreiundzwanzigsten", 23}, {"vierundzwanzigsten", 24},
            {"fünfundzwanzigsten", 25}, {"sechsundzwanzigsten", 26}, {"siebenundzwanzigsten", 27},
            {"achtundzwanzigsten", 28}, {"neunundzwanzigsten", 29}, {"dreißigsten", 30},
            {"einunddreissigsten", 31}
        };

        public MasterService()
        {
            Logger.Debug("Starting.");
            RequestMqttBrokerPassword();
        }
        private void RequestMqttBrokerPassword()
        {
            mqttBrokerUsername = "BatMQTTUser";
            mqttBrokerPassword = "LsR_3123";
        }
        public void Init()
        {
            Logger.Info("Initializing MasterService...");

            pythonService = new PythonService("/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/speechIntent.py");
            mqttServiceRx = new MqttService(MqttTopics.TopicTx, mqttBrokerUsername, mqttBrokerPassword);
            mqttServiceTx = new MqttService(MqttTopics.TopicRx, mqttBrokerUsername, mqttBrokerPassword);
            jsonService = new JsonService("./Python/speechRecognitionOutput.json");
            soundService = new SoundService();
            absenceService = new AbsenceService(mqttServiceRx);
            deviceManagementService = new DeviceManagementService(soundService);
            actionProcessingService = new ActionProcessingService(actionRequests, newActionEvent, _actionRequestsLock, pythonService, jsonService, soundService, mqttServiceTx, absenceService);

            mqttServiceRx.MessageReceivedEvent += MqttActionRequested;
            jsonService.FileChangedEvent += AnswerReceived;
            soundService.AudioPlayedEvent += QuestionSoundPlayed;
            Devices.OnNewActionAdded += deviceManagementService.GenerateSoundsForDeviceAction;


            initializationService = new InitializationService(pythonService, mqttServiceRx, mqttServiceTx, jsonService, soundService, absenceService, deviceManagementService, actionProcessingService);
            initializationService.Initialize();

            Logger.Info("Application finally started.");
        }
        private void MqttActionRequested(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string receivedActionRequestJson = Encoding.UTF8.GetString(e.Message);
                MqttMessage receivedActionRequest = JsonSerializer.Deserialize<MqttMessage>(receivedActionRequestJson);
                lock (_actionRequestsLock)
                {
                    actionRequests.Enqueue(receivedActionRequest);
                    Logger.Debug($"Enqueued action request: {receivedActionRequest.Device}, {receivedActionRequest.Action}");
                }
                Logger.Info($"{receivedActionRequest.Device} action requested: {receivedActionRequest.Action}");
                newActionEvent.Set();
                Logger.Debug($"Event Set");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"This was no valid action request.");
            }
        }
        private void HandleAbsenceDuration(SpeechRecognitionDataModel data)
        {
            if (data.Slots.TryGetValue("Day", out string day) &&
                data.Slots.TryGetValue("Month", out string month))
            {
                try
                {
                    DateTime currentDate = DateTime.Now;
                    DateTime absenceEndDate = new(currentDate.Year, GetMonthFromName(month), ConvertOrdinalToNumber(day));
                    Logger.Info($"Absence duration calculated: {absenceEndDate}");

                    string absenceMessage = MqttService.GenerateAbsenceMessage("Absenz", "ein", absenceEndDate);
                    mqttServiceTx.PublishMessage(absenceMessage);
                    absenceService.AbsenceMode = true;
                    Logger.Info("Absence end date sent to Home Assistant.");
                    TerminalService.RunCmdAndReturnOutput($"echo 'Abwesenheit für den {day} {month} bestätigt' |" +
                        " /home/auxilium/piper/piper/piper -m /home/auxilium/piper/thorsten_voice/de_DE-thorsten-medium.onnx" +
                        " --output_file temp.wav && aplay temp.wav && rm temp.wav");
                    actionProcessingService.Reset();
                    retryCount = 0;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Fehler beim Berechnen der Abwesenheitsdauer.");
                    TerminalService.RunCmdAndReturnOutput($"echo 'Bitte um erneute Angabe.' |" +
                        " /home/auxilium/piper/piper/piper -m /home/auxilium/piper/thorsten_voice/de_DE-thorsten-medium.onnx" +
                        " --output_file temp.wav && aplay temp.wav && rm temp.wav");
                    actionProcessingService.Reset();
                    mqttServiceRx.PublishMessage(MqttService.GenerateActionMessage("Absenz", "ein"));
                }
            }
            else
            {
                Logger.Warn("Slot(s) fehlen für die Abwesenheitsdauer.");
            }
        }
        private async void HandleDeviceAction(SpeechRecognitionDataModel data)
        {
            if (data.Slots.TryGetValue("Action", out string action))
            {
                if (Devices.ActionExistsForDevice(data.Intent, action))
                {
                    actionProcessingService.ExecuteAction(data.Intent, action, true);
                    retryCount = 0;
                }
                else
                {
                    Logger.Warn($"Aktion {action} für das Gerät {data.Intent} nicht gefunden.");
                    await pythonService.SendCommand("action");
                    soundService.PlaySound("action_not_found").Wait();
                }
            }
            else
            {
                Logger.Warn("Slot(s) fehlen für die Aktion.");
            }
        }
        private void HandleModeSelection(SpeechRecognitionDataModel data)
        {
            if (data.Intent == "Absence")
            {
                actionProcessingService.ProcessAbsenceRequest("Absenz", "ein");
            }
            else if (data.Intent == "Action")
            {
                actionProcessingService.ProcessDeviceAndAction("Action", "On");
            }
        }
        private async void HandleConfirmation(SpeechRecognitionDataModel data)
        {
            if (data.Intent == "Ja")
            {
                Logger.Info("Antwort: bestätigt");
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, true);
                retryCount = 0;
            }
            else if (data.Intent == "Nein")
            {
                Logger.Info("Antwort: abgelehnt");
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, false);
                retryCount = 0;
            }
            else if (retryCount > maxRetries)
            {
                Logger.Warn("Keine Antwort erkannt.");
                await soundService.PlaySound("no_answer");
                actionProcessingService.ExecuteAction(actionProcessingService.GetCurrentActionRequest().Device, actionProcessingService.GetCurrentActionRequest().Action, false);
            }
            else
            {
                Logger.Warn("Antwort nicht erkannt, bitte wiederholen.");
                await pythonService.SendCommand("confirmation");
                await soundService.PlaySound("not_recognized");
                jsonService.ClearJsonFile();
                await soundService.PlaySound($"{actionProcessingService.GetCurrentActionRequest().Device}_{actionProcessingService.GetCurrentActionRequest().Action}_question");
            }
        }

        private void AnswerReceived(object sender, SpeechRecognitionDataModel data)
        {
            Logger.Info($"Received intent: {data.Intent}");

            try
            {
                if (data.Intent == "Dauer" && data.Slots != null)
                {
                    HandleAbsenceDuration(data);
                }
                else if (Devices.DeviceExists(data.Intent) && data.Slots != null)
                {
                    HandleDeviceAction(data);
                }
                else if (data.Intent == "Absence" || data.Intent == "Action")
                {
                    HandleModeSelection(data);
                }
                else
                {
                    HandleConfirmation(data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing answer.");
            }
        }
        private async void QuestionSoundPlayed(object sender, AudioEventArgs e)
        {
            await soundService.PlaySound("90s-game-ui");
            Logger.Info("Sound played. Starting Recognition");
            Logger.Debug("Sending command: start");
            await pythonService.SendCommand("start");

            retryCount++;
        }
        private static int GetMonthFromName(string month)
        {
            var monthNames = new Dictionary<string, int>
            {
                {"Januar", 1}, {"Februar", 2}, {"März", 3}, {"Mu00e4rz",3}, {"April", 4},
                {"Mai", 5}, {"Juni", 6}, {"Juli", 7}, {"August", 8},
                {"September", 9}, {"Oktober", 10}, {"November", 11}, {"Dezember", 12}
            };

            if (monthNames.TryGetValue(month, out int monthNumber))
            {
                Logger.Debug($"Month conversion: {month} - {monthNumber}");
                return monthNumber;
            }

            Logger.Error($"Invalid month name: {month}");
            throw new ArgumentException("Ungültiger Monatsname.");
        }
        private static int ConvertOrdinalToNumber(string ordinal)
        {
            if (OrdinalToNumber.TryGetValue(ordinal.ToLower(), out int number))
            {
                Logger.Debug($"Ordinal to number conversion: {ordinal} - {number}");
                return number;
            }

            Logger.Error($"Unknown ordinal: {ordinal}");
            throw new ArgumentException($"Unbekannte Ordinalzahl: {ordinal}");
        }
    }
}
