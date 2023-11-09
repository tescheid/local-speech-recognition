﻿using Common;
using LocalSpeechRecognitionMaster;
using LocalSpeechRecognitionMaster.DataModels;
using LocalSpeechRecognitionMaster.Services;
using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace LocalSpeechRecognitionMaster
{

    public class MasterService
    {
        private MqttService mqttServiceRx;
        private MqttService mqttServiceTx;
        private JsonService jsonService;
        private SoundService soundService;
        private PythonService pythonService;

        private int maxWaitOnResponseIterations = 50;

        private MqttMessage receivedActionRequest = new MqttMessage();
        private string answer = "";

        private string mqttBrokerUsername="";
        private string mqttBrokerPassword = "";


        public MasterService()
        {
            requestMqttBrokerPassword();
            Init();
        }

        private void requestMqttBrokerPassword()
        {
            //todo: uncomment
            /*
            Console.Write("Benutzername: ");
            mqttBrokerUsername = Console.ReadLine();
            Console.Write("Passwort: ");
            mqttBrokerPassword = Console.ReadLine();
            */
            mqttBrokerUsername = "paindMQTTUser";
            mqttBrokerPassword = "rp2040";
    }


        private void Init()
        {
            mqttServiceRx = new MqttService("LocalSpeechRecognitionTx",mqttBrokerUsername,mqttBrokerPassword); //receive from tx of gateway
            mqttServiceRx.messageReceivedEvent += BlindsActionRequested;

            mqttServiceTx = new MqttService("LocalSpeechRecognitionRx", mqttBrokerUsername, mqttBrokerPassword); //send to rx of gateway


            jsonService = new JsonService("./speechRecognitionOutput.json");
            jsonService.fileChangedEvent += AnswerReceived;

            soundService = new SoundService();
            soundService.audioPlayedEvent += ActionRequestSoundPlayed;

            pythonService = new PythonService("Python/test.py");
            pythonService.RunCmd();

            //SimulateFileChanged(); //todo auskommentieren
            StartSpeechRecognition();
        }

        private void StartSpeechRecognition()
        {
            //todo, start speech recognition
        }

        private void SimulateFileChanged()
        {
            while (true)
            {
                string content = "{\r\n\t\"text\": \"Yes\"\r\n}";
                File.WriteAllText(jsonService.getFilePath(), content);
                Thread.Sleep(1000);
            }

        }

        //Step 1: Action requested from user or gateway systemfor Blinds 
        private void BlindsActionRequested(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string receivedActionRequestJson = Encoding.UTF8.GetString(e.Message);
                receivedActionRequest = JsonSerializer.Deserialize<MqttMessage>(receivedActionRequestJson);
                Console.WriteLine(receivedActionRequest.Device+ " action requested: " + receivedActionRequest.Action);

                soundService.PlaySound(receivedActionRequest,true);
            }
            catch (Exception exception)
            {
                Console.WriteLine("This was no valid action request");
            }
        }
        //Step 2: User was Asked. Now waiting for response
        private void ActionRequestSoundPlayed(object sender, AudioEventArgs e)
        {
            Console.WriteLine("Action request Sound Played. Waiting for response...");
            int iterations = 0;
            while (true)
            {

                //Todo remove. just for simulation--------------------------------------
                if (iterations == 3)
                {
                    string content = "{\r\n\t\"text\": \"Yes\"\r\n}";
                    File.WriteAllText(jsonService.getFilePath(), content);
                }
                //Todo remove. just for simulation--------------------------------------

                if (answer.Length > 0)
                {

                    if (answer == Answers.Yes)
                    {
                        executeAction(receivedActionRequest.Device, receivedActionRequest.Action);
                    }
                    else
                    {
                        executeAction(receivedActionRequest.Device, Actions.None);
                    }
                    break;
                }

                if (iterations > maxWaitOnResponseIterations)
                {
                    executeAction(receivedActionRequest.Device, Actions.None);
                    break;
                }

                iterations++;
                Thread.Sleep(500);
            }
        }

        //Step 3. Response has been given.
        private void AnswerReceived(object sender, SpeechRecognitionDataModel data)
        {
            answer = data.text;
            Console.WriteLine("The User answered: " + answer);
        }

        //Step 4. Execute action based on user response
        private void executeAction(string device, string action)
        {
            string actionAsJson = "{\"Device\":" + device + "\",\"Action\":\"" + action + "\"}";
            Console.WriteLine("Execute Action: " + action);
            mqttServiceTx.PublishMessage(actionAsJson);
            answer = "";
            soundService.PlaySound(receivedActionRequest, false);

        }

    }
}
