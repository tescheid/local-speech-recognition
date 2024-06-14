using NLog;

namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for initializing all other services in the application.
    /// </summary>
    public class InitializationService
    {
        //Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Required service
        private readonly PythonService pythonService;
        private readonly MqttService mqttServiceRx;
        private readonly MqttService mqttServiceTx;
        private readonly JsonService jsonService;
        private readonly SoundService soundService;
        private readonly AbsenceService absenceService;
        private readonly ActionProcessingService actionProcessingService;

        /// <summary>
        /// Constructor for the InitializationService.
        /// Initializes the dependencies for the service.
        /// </summary>
        /// <param name="pythonService">Service for interacting with speech recognition.</param>
        /// <param name="mqttServiceRx">Service for receiving MQTT messages.</param>
        /// <param name="mqttServiceTx">Service for transmitting MQTT messages.</param>
        /// <param name="jsonService">Service for handling JSON.</param>
        /// <param name="soundService">Service for playing sounds.</param>
        /// <param name="absenceService">Service for handling absencemode.</param>
        /// <param name="actionProcessingService">Service for processing action requests.</param>
        public InitializationService(
            PythonService pythonService,
            MqttService mqttServiceRx,
            MqttService mqttServiceTx,
            JsonService jsonService,
            SoundService soundService,
            AbsenceService absenceService,
            ActionProcessingService actionProcessingService)
        {
            this.pythonService = pythonService;
            this.mqttServiceRx = mqttServiceRx;
            this.mqttServiceTx = mqttServiceTx;
            this.jsonService = jsonService;
            this.soundService = soundService;
            this.absenceService = absenceService;
            this.actionProcessingService = actionProcessingService;
        }

        /// <summary>
        /// Initializes all services.
        /// </summary>
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