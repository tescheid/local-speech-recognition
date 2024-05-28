using LocalSpeechRecognitionMaster;

namespace LocalSpeechRecognitionMaster
{
    internal class Program
    {
        static void Main()
        {
            MasterService service = new();
            service.Init();
        }
    }
}