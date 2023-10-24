using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster
{
    public class SoundService
    {
        public event EventHandler<AudioEventArgs> audioPlayedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public SoundService() { }

        
        public void PlayRequestBlindsUpSound()
        {
            Thread.Sleep(1000);
            //Todo play sound "Sollen die Storen hochgefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));

        }
        public void PlayRequestBlindDownSound()
        {
            Thread.Sleep(1000);
            //Todo play sound "Sollen die Storen hochgefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

    }
}
