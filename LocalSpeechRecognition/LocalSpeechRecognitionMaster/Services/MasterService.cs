using Common;
using Common.DataModels;
using LocalSpeechRecognitionMaster;
using LocalSpeechRecognitionMaster.Services;
using System.Collections;
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

        private string answer = "";

        private string mqttBrokerUsername = "";
        private string mqttBrokerPassword = "";

        Queue<MqttMessage> actionRequests = new Queue<MqttMessage>();
        MqttMessage currentActionRequest;
        bool lastRequestFinished = true;
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
            mqttServiceRx = new MqttService(MqttTopics.TopicTx, mqttBrokerUsername, mqttBrokerPassword); //receive from tx of gateway
            mqttServiceRx.messageReceivedEvent += ActionRequested;

            mqttServiceTx = new MqttService(MqttTopics.TopicRx, mqttBrokerUsername, mqttBrokerPassword); //send to rx of gateway


            jsonService = new JsonService("./speechRecognitionOutput.json");
            jsonService.fileChangedEvent += AnswerReceived;

            soundService = new SoundService();
            soundService.audioPlayedEvent += ActionRequestSoundPlayed;

            pythonService = new PythonService("Python/speechRecognition.py");

            StartSpeechRecognition();

            Thread t = new Thread(ProcessActionRequests);
            t.IsBackground = true;
            t.Start();

        }

        private void StartSpeechRecognition()
        {
            //pythonService.RunCmd();
        }


        //Step 0: Action requested from user or gateway system  
        private void ActionRequested(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string receivedActionRequestJson = Encoding.UTF8.GetString(e.Message);
                MqttMessage receivedActionRequest = JsonSerializer.Deserialize<MqttMessage>(receivedActionRequestJson);
                Console.WriteLine(receivedActionRequest.Device + " action requested: " + receivedActionRequest.Action);
                actionRequests.Enqueue(receivedActionRequest);
            }
            catch (Exception exception)
            {
                Console.WriteLine("This was no valid action request");
            }
        }
        //Step 1: Process Action request Queue
        private void ProcessActionRequests()
        {
            while (true)
            {
                if (actionRequests.Count > 0 && lastRequestFinished)
                {
                    Console.WriteLine("now");
                    lastRequestFinished = false;
                    currentActionRequest = actionRequests.Dequeue();
                    soundService.PlaySound(currentActionRequest, true);
                }
                else
                {
                    Thread.Sleep(1000);
                }

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
                    string content = "{\"text\": \"Ja\"}";
                    File.WriteAllText(jsonService.getFilePath(), content);
                }
                //Todo remove. just for simulation--------------------------------------

                if (answer.Length > 0)
                {
                    Console.WriteLine("Antwort"+answer);
                    if (answer == Answers.Yes)
                    {
                        executeAction(currentActionRequest.Device, currentActionRequest.Action);
                    }
                    else
                    {
                        executeAction(currentActionRequest.Device, Actions.None);
                    }
                    break;
                }

                if (iterations > maxWaitOnResponseIterations)
                {
                    executeAction(currentActionRequest.Device, Actions.None);
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
            string actionAsJson = mqttServiceRx.GenerateActionMessage(device, action);
            Console.WriteLine("Execute Action: " + action);
            mqttServiceTx.PublishMessage(actionAsJson);
            answer = "";
            soundService.PlaySound(new MqttMessage(device,action), false);
            lastRequestFinished = true;
        }

    }
}
