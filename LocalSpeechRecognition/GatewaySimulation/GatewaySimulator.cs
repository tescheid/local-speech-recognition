using Common;
using Common.DataModels;
using LocalSpeechRecognitionMaster;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace GatewaySimulation
{
    public class GatewaySimulator
    {
        string mqttBrokerUsername = "paindMQTTUser";
        string mqttBrokerPassword = "rp2040";

        MqttService mqttServiceRx;
        MqttService mqttServiceTx;
        GpioController gpc = new GpioController();
        Random rand = new Random();
        byte ReceivedResponseLedPin = 16;

        public GatewaySimulator()
        {
            Init();
            Simulate();
        }

        private void Init()
        {
            mqttServiceRx = new MqttService(MqttTopics.TopicRx, mqttBrokerUsername, mqttBrokerPassword);
            mqttServiceRx.messageReceivedEvent += ReceivedMqttMessage;
            mqttServiceTx = new MqttService(MqttTopics.TopicTx, mqttBrokerUsername, mqttBrokerPassword);
            gpc.OpenPin(ReceivedResponseLedPin, PinMode.Output);
        }

        public void Simulate()
        {
            int randomSleepTime = rand.Next(10000, 20000);
            mqttServiceTx.PublishMessage(mqttServiceTx.GenerateActionMessage(Devices.Blinds, getRandomAction()));
            Thread.Sleep(randomSleepTime);
            Simulate();
        }

        private string getRandomAction()
        {
            int randomInd = rand.Next(2);
            switch (randomInd)
            {
                case 0: return Actions.Up;
                case 1: return Actions.Down;
                default: return Actions.None;
            }
        }

        public void ReceivedMqttMessage(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine("Received: "+Encoding.UTF8.GetString(e.Message));
            for (byte i = 0; i < 6; i++)
            {
                gpc.Write(ReceivedResponseLedPin, i % 2 == 0 ? 1 : 0);
                Thread.Sleep(500);
            }
        }
    }
}
