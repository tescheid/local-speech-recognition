using Common;
using NLog;

namespace LocalSpeechRecognitionMaster.Services
{
    public class InitializationService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly PythonService pythonService;
        private readonly MqttService mqttServiceRx;
        private readonly MqttService mqttServiceTx;
        private readonly JsonService jsonService;
        private readonly SoundService soundService;
        private readonly AbsenceService absenceService;
        private readonly DeviceManagementService deviceManagementService;
        private readonly ActionProcessingService actionProcessingService;

        public InitializationService(
            PythonService pythonService,
            MqttService mqttServiceRx,
            MqttService mqttServiceTx,
            JsonService jsonService,
            SoundService soundService,
            AbsenceService absenceService,
            DeviceManagementService deviceManagementService,
            ActionProcessingService actionProcessingService)
        {
            this.pythonService = pythonService;
            this.mqttServiceRx = mqttServiceRx;
            this.mqttServiceTx = mqttServiceTx;
            this.jsonService = jsonService;
            this.soundService = soundService;
            this.absenceService = absenceService;
            this.deviceManagementService = deviceManagementService;
            this.actionProcessingService = actionProcessingService;
        }

        public void Initialize()
        {
            Logger.Info("Initializing services...");

            pythonService.Init();
            Logger.Info("Python initialized");

            mqttServiceRx.Init();
            mqttServiceTx.Init();
            Logger.Info("MQTT initialized");

            jsonService.Init();
            Logger.Info("JSON initialized");

            soundService.Init();
            Logger.Info("Sound initialized");

            absenceService.Init();
            Logger.Info("Absence initialized");

            DeviceManagementService.Init();
            Logger.Info("Devices initialized");

            actionProcessingService.Init();
            Logger.Info($"Action Processing initialized");


            Logger.Info("All services initialized successfully.");
        }
        
    }
}