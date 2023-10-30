using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;


namespace LocalSpeechRecognitionMaster
{
    public class MqttService
    {
        private MqttClient client;

        private string username = "paindMQTTUser";
        private string password = "rp2040";
        private string topic = "";

        public event EventHandler<MqttMsgPublishEventArgs> messageReceivedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public MqttService(string topic)
        {
            this.topic = topic;
            Connect();
        }

        public void Connect()
        {
             //client = new MqttClient("eee-02013.simple.eee.intern");
             client = new MqttClient("192.168.1.110");
            /* register to message received */
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            //authenticated connection
            string clientId = Guid.NewGuid().ToString();
            try
            {
               // password = GetPassword();
                client.Connect(clientId, username, password);
                Console.WriteLine("MQTT connected");
            }
            catch (Exception e)
            {
                Console.WriteLine("MQTT Connection error\n", e);
            }
            //EMPFANGEN
            /* subscribe to a topic */
            client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private static string GetPassword()
        {
            try
            {
                string pw= System.Environment.GetEnvironmentVariable("MQTT_PW");
                if (pw!=null)
                {
                    return pw;
                }
                else
                {
                    return "";
                }
           }
            catch(Exception e)
            {
                Console.WriteLine( e);
            }
            return "";
        }

        public void PublishMessage(string msg)
        {
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

