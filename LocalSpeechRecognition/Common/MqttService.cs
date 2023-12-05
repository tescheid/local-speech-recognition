using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;


namespace Common
{
    public class MqttService
    {
        private MqttClient client;

        private string username = "paindMQTTUser";
        private string password = "rp2040";
        private string topic = "";

        public event EventHandler<MqttMsgPublishEventArgs> messageReceivedEvent;

        public MqttService(string topic, string username,string password)
        {
            this.password= password;    
            this.username = username;
            this.topic = topic;
            Connect();
        }

        public void Connect()
        {
           
           
            try
            {
                //client = new MqttClient("eee-02013.simple.eee.intern");
                client = new MqttClient("192.168.1.110");
                /* register to message received */
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                //authenticated connection
                string clientId = Guid.NewGuid().ToString();
                // password = GetPassword();
                client.Connect(clientId, username, password);
                Console.WriteLine("MQTT connected");
                //EMPFANGEN
                /* subscribe to a topic */
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            catch (Exception e)
            {
                Console.WriteLine("MQTT Connection error\n", e);
            }
          
        }


        public string GenerateActionMessage(string device, string action)
        {
            return "{\"Device\":\"" + device + "\",\"Action\":\"" + action + "\"}";
        }


        public void PublishMessage(string msg)
        {
            Console.WriteLine(msg);
            //string msg = "This is a message from Client"; //Console.ReadLine();
            client.Publish(topic, Encoding.UTF8.GetBytes(msg)); //Topic=Aprog
        }

        /* handle message received */
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message);
            messageReceivedEvent?.Invoke(this,e);
        }

    }
}

