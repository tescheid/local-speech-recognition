using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace LocalSpeechRecognitionMaster
{
    public class SoundService
    {
        public event EventHandler<AudioEventArgs> audioPlayedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public SoundService() { }

        //request action------------------------------------------------------------
        public void PlaySound(MqttMessage msg, bool isQuestion)
        {
            if (msg.Device==Devices.Blinds)
            {
                switch (msg.Action)
                {
                    case Actions.Up:
                        if (isQuestion)
                        {
                            PlayRequestBlindsUpSound();
                        }
                        else
                        {
                            playBlindsAreUpSound();
                        }
                        break;

                    case Actions.Down:

                        if (isQuestion)
                        {
                            PlayRequestBlindsDownSound();
                        }
                        else
                        {
                            playBlindsAreDownSound();
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        
        private void PlayRequestBlindsUpSound()
        {
            Console.WriteLine("Playing sound: Blinds up");
            Thread.Sleep(3000);
            //Todo play sound "Sollen die Storen hochgefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));

        }

        private void PlayRequestBlindsDownSound()
        {
            Console.WriteLine("Playing sound: Blinds down");
            Thread.Sleep(3000);
            //Todo play sound "Sollen die Storen hochgefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

        private void playBlindsAreDownSound()
        {
            Console.WriteLine("Playing sound: Storen sind herunter gefahren");
            Thread.Sleep(3000);
            //Todo play sound "Storen sind herunter gefahren"
        }

        private void playBlindsAreUpSound()
        {
            Console.WriteLine("Playing sound: Storen sind hochgefahren");
            Thread.Sleep(3000);
            //Todo play sound "Storen sind hochgefahren"
        }

    }
}
