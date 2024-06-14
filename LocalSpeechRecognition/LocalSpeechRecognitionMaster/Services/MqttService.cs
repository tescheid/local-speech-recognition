using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using NLog;
using NLog.Fluent;
using Microsoft.Extensions.Configuration;

namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for handling MQTT communication.
    /// </summary>
    public class MqttService
    {
        // MQTT instance
        private MqttClient client;

        // Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // MQTT settings
        private readonly string _username;
        private readonly string _password;
        private readonly string _topic;

        // Event triggerd for incoming message
        public event EventHandler<MqttMsgPublishEventArgs> MessageReceivedEvent;

        /// <summary>
        /// Constructor for  MqttService.
        /// Initializes MQTT service with  topic, username, and password.
        /// </summary>
        /// <param name="topic">Topic to subscribe or publish.</param>
        /// <param name="username">MQTT username.</param>
        /// <param name="password">MQTT password.</param>
        public MqttService(string topic, IConfiguration configuration)
        {
            _password= configuration["Credentials:mqttBrokerPassword"];    
            _username = configuration["Credentials:mqttBrokerPassword"];
            _topic = topic;
        }

        /// <summary>
        /// Initializes MQTT connection.
        /// </summary>
        public void Init()
        {
            Logger.Info($"Initializing connection to MqttService for topic: {_topic}...");
            Connect();
        }

        /// <summary>
        /// Connects to MQTT broker subscribe to topic.
        /// </summary>
        public void Connect()
        {
            try
            {
                // Connect to local! MQTT Broker
                client = new MqttClient("127.0.0.1");

                // Register message received event
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                // Generate a client ID
                string clientId = Guid.NewGuid().ToString();

                // Connect to broker with authentication
                client.Connect(clientId, _username, _password);

                // Subscribe to topic
                client.Subscribe(new string[] {_topic}, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                Logger.Debug($"MQTT {_topic} connected");
            }
            catch (Exception e)
            {
                Logger.Error($"MQTT Connection error\n:{e}");
            }
          
        }

        /// <summary>
        /// Generate JSON string for action message.
        /// </summary>
        /// <param name="device">Device for action.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns>action message.</returns>
        public static string GenerateActionMessage(string device, string action)
        {
            return "{\"Device\":\"" + device + "\",\"Action\":\"" + action + "\"}";
        }

        /// <summary>
        /// Generate JSON string for absence message.
        /// </summary>
        /// <param name="device">Always "Absenz".</param>
        /// <param name="action">Absence action.</param>
        /// <param name="date">Enddate of  absence.</param>
        /// <returns>absence message.</returns>
        public static string GenerateAbsenceMessage(string device, string action,DateTime date)
        {
            return "{\"Device\": \""+device+"\"," +
                   "\"Action\":\"" + action + "\"," +
                   "\"EndDate\":\"" + date + "\"}";
        }

        /// <summary>
        /// Publish message to Broker.
        /// </summary>
        /// <param name="msg">Message to publish.</param>
        public void PublishMessage(string msg)
        {
            Logger.Info(msg);
            client.Publish(_topic, Encoding.UTF8.GetBytes(msg)); 
        }

        /// <summary>
        /// Message received eventhandler.
        /// </summary>
        void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message);
            MessageReceivedEvent?.Invoke(this,e);
        }

    }
}

