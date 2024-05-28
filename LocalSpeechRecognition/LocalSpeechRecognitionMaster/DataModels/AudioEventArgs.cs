namespace LocalSpeechRecognitionMaster.DataModels
{
    public class AudioEventArgs:EventArgs
    {
        private readonly bool played = false;
        public AudioEventArgs(bool played) {
            this.played = played;
        } 
    }
}
