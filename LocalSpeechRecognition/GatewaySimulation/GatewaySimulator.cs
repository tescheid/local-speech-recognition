using Common;
using Common.DataModels;
using System.Device.Gpio;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace GatewaySimulation
{
    public class GatewaySimulator
    {
        readonly string mqttBrokerUsername = "BatMQTTUser";
        readonly string mqttBrokerPassword = "LsR_3123";

        MqttService mqttServiceRx;
        MqttService mqttServiceTx;
        readonly GpioController gpc = new();
        readonly Random rand = new();
        readonly byte ReceivedResponseLedPin = 16;

        public GatewaySimulator()
        {
            Init();
            Simulate();
        }

        private void Init()
        {
            mqttServiceRx = new MqttService(MqttTopics.TopicRx, mqttBrokerUsername, mqttBrokerPassword);
            mqttServiceRx.MessageReceivedEvent += ReceivedMqttMessage;
            mqttServiceTx = new MqttService(MqttTopics.TopicTx, mqttBrokerUsername, mqttBrokerPassword);
            gpc.OpenPin(ReceivedResponseLedPin, PinMode.Output);
        }

        public void Simulate()
        {
            int randomSleepTime = rand.Next(10000, 20000);
            mqttServiceTx.PublishMessage(MqttService.GenerateActionMessage("Licht", "aus"));
            Thread.Sleep(randomSleepTime);
            Simulate();
        }

        public void ReceivedMqttMessage(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine("Received: " + Encoding.UTF8.GetString(e.Message));
            for (byte i = 0; i < 6; i++)
            {
                gpc.Write(ReceivedResponseLedPin, i % 2 == 0 ? 1 : 0);
                Thread.Sleep(500);
            }
        }
    }
}
