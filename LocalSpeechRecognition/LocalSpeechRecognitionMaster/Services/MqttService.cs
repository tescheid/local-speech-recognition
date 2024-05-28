using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using NLog;
using NLog.Fluent;

namespace LocalSpeechRecognitionMaster.Services
{
    public class MqttService
    {
        private MqttClient client;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string username = "BatMQTTUser";
        private readonly string password = "LsR_3123";
        private readonly string topic = "";

        public event EventHandler<MqttMsgPublishEventArgs> MessageReceivedEvent;

        public MqttService(string topic, string username,string password)
        {
            this.password= password;    
            this.username = username;
            this.topic = topic;
        }
        public void Init()
        {
            Logger.Info($"Initializing connection to MqttService for topic: {topic}...");
            Connect();
        }

        public void Connect()
        {
            try
            {
                client = new MqttClient("127.0.0.1");

                /* register to message received */
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                //authenticated connection
                string clientId = Guid.NewGuid().ToString();

                // password = GetPassword();
                client.Connect(clientId, username, password);
          
                /* subscribe to a topic */
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                Logger.Debug($"MQTT {topic} connected");
            }
            catch (Exception e)
            {
                Logger.Error($"MQTT Connection error\n:{e}");
            }
          
        }


        public static string GenerateActionMessage(string device, string action)
        {
            return "{\"Device\":\"" + device + "\",\"Action\":\"" + action + "\"}";
        }
        public static string GenerateAbsenceMessage(string device, string action,DateTime date)
        {
            return "{\"Device\": \""+device+"\"," +
                   "\"Action\":\"" + action + "\"," +
                   "\"EndDate\":\"" + date + "\"}";
        }

        public void PublishMessage(string msg)
        {
            Logger.Info(msg);
            client.Publish(topic, Encoding.UTF8.GetBytes(msg)); 
        }

        /* handle message received */
        void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message);
            MessageReceivedEvent?.Invoke(this,e);
        }

    }
}

