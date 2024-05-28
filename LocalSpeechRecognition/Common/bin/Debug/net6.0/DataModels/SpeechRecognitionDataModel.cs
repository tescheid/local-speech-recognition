using System.Text.Json.Serialization;


namespace Common
{
    public class SpeechRecognitionDataModel
    {
        [JsonPropertyName("intent")]
        public string Intent { get; set; }

        [JsonPropertyName("slots")]
        public Dictionary<string, string> Slots { get; set; }
    }
}

