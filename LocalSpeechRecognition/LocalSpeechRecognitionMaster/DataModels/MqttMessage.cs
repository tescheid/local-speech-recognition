namespace LocalSpeechRecognitionMaster.DataModels
{
    public class MqttMessage
    {
        public string Device { get; set; } = "";
        public string Action { get; set; } = "";
        public MqttMessage(string Device, string Action)
        {
            this.Device = Device; 
            this.Action = Action;
        }
    }
}
