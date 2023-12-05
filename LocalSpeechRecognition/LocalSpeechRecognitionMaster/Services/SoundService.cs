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
        private TerminalService terminalService;

        public event EventHandler<AudioEventArgs> audioPlayedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public SoundService() {
        terminalService = new TerminalService();
        }

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
        
        //Sounds müssen im S16_LE (signed 16bit Little Endian) Format aufgenommen werden.
        private void PlayRequestBlindsUpSound()
        {
            Console.WriteLine("Playing sound: Blinds up");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/blindsUp.wav");
           // Thread.Sleep(3000);
            //Todo play sound "Sollen die Storen hochgefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

        private void PlayRequestBlindsDownSound()
        {
            Console.WriteLine("Playing sound: Blinds down");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/blindsDown.wav");
            //Todo play sound "Sollen die Storen heruntergefahren werden?"
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

        private void playBlindsAreDownSound()
        {
            Console.WriteLine("Playing sound: Storen sind herunter gefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/doingBlindsDown.wav");
            //Todo play sound "Storen sind herunter gefahren"
        }

        private void playBlindsAreUpSound()
        {
            Console.WriteLine("Playing sound: Storen sind hochgefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/doingBlindsUp.wav");
            //Todo play sound "Storen sind hochgefahren"
        }

    }
}
