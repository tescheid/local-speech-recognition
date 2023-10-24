using PAIND_Communication.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PAIND_Communication
{

    public class CommunicationService
    {
        private MqttService mqttHelper;
        private JsonService jsonHelper;
        private SoundService soundHelper;

        private int maxWaitOnResponseIterations = 50;

        private MqttMessage receivedActionRequest = new MqttMessage();
        private string answer = "";

        public CommunicationService()
        {
            mqttHelper = new MqttService("LocalSpeechRecognitionBlinds");
            mqttHelper.messageReceivedEvent += BlindsActionRequested;

            jsonHelper = new JsonService("./speechRecognitionOutput.json");
            jsonHelper.fileChangedEvent += AnswerReceived;

            soundHelper = new SoundService();
            soundHelper.audioPlayedEvent += ActionRequestSoundPlayed;

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
                File.WriteAllText(jsonHelper.getFilePath(), content);
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
                Console.WriteLine("Blinds action requested: " + receivedActionRequest.BlindsAction + " from: " + receivedActionRequest.Sender);

                if (receivedActionRequest.Sender != Senders.LocalSpeechRecognitionSystem)
                {
                    switch (receivedActionRequest.BlindsAction)
                    {
                        case BlindsActions.BlindsDown:
                            soundHelper.PlayRequestBlindDownSound();
                            break;
                        case BlindsActions.BlindsUp:
                            soundHelper.PlayRequestBlindsUpSound();
                            break;
                        default: Console.WriteLine("This is no valid action request"); break;
                    }
                }

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
                    File.WriteAllText(jsonHelper.getFilePath(), content);
                }
                //Todo remove. just for simulation--------------------------------------

                if (answer.Length > 0)
                {

                    switch (receivedActionRequest.BlindsAction)
                    {
                        case BlindsActions.BlindsDown:
                            if (answer == Answers.Yes)
                            {
                                executeAction(BlindsActions.BlindsDown);
                            }
                            else
                            {
                                executeAction(BlindsActions.BlindsNone);
                            }
                            break;
                        case BlindsActions.BlindsUp:
                            if (answer == Answers.Yes)
                            {
                                executeAction(BlindsActions.BlindsUp);
                            }
                            else
                            {
                                executeAction(BlindsActions.BlindsNone);
                            }
                            break;
                        default:
                            executeAction(BlindsActions.BlindsNone);
                            Console.WriteLine("This is no valid action request");
                            break;
                    }
                    break;
                }

                if (iterations > maxWaitOnResponseIterations)
                {
                    executeAction(BlindsActions.BlindsNone);
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
            Console.WriteLine("User answered: " + answer);
        }

        //Step 4. Execute action based on user response
        private void executeAction(string action)
        {
            string actionAsJson = "{ \"BlindsAction\":\"" + action + "\",\"Sender\":\"" + Senders.LocalSpeechRecognitionSystem + "\"}";
            Console.WriteLine("Execute Action: ", actionAsJson);
            mqttHelper.PublishMessage(actionAsJson);
        }

    }
}
