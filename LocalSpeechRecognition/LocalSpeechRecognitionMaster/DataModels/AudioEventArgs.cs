namespace LocalSpeechRecognitionMaster
{
    public class AudioEventArgs:EventArgs
    {
        bool played = false;
        public AudioEventArgs(bool played) {
            this.played = played;
        } 
    }
}
