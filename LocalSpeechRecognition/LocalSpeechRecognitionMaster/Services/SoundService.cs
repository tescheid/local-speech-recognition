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
        public void PlaySound(MqttMessage msg, bool isQuestion, bool notRecognized)
        {
            if (notRecognized)
            {
                PlayAnswerNotRecognized();
            } 
            else if (msg.Device == Devices.Blinds && notRecognized==false)
            {
                switch (msg.Action)
                {
                    case Actions.UpDenied:

                        PlayBlindsAreUpDeniedSound();
                        break;
                    
                    case Actions.Up:
                        if (isQuestion)
                        {
                            PlayRequestBlindsUpSound();
                        }
                        else
                        {
                            PlayBlindsAreUpSound();
                        }

                        break;
                    
                    case Actions.DownDenied:

                        PlayBlindsAreDownDeniedSound();
                        break;

                    case Actions.Down:

                        if (isQuestion)
                        {
                            PlayRequestBlindsDownSound();
                        }
                        else
                        {
                            PlayBlindsAreDownSound();
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
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-hoch-question.wav");
            Thread.Sleep(1000);
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

        private void PlayRequestBlindsDownSound()
        {
            Console.WriteLine("Playing sound: Blinds down");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-runter-question.wav");
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

        private void PlayBlindsAreDownSound()
        {
            Console.WriteLine("Playing sound: Storen sind herunter gefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-runter-bestätigt.wav");
        }
        private void PlayBlindsAreDownDeniedSound()
        {
            Console.WriteLine("Playing sound: Storen sind herunter gefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-runter-abgebrochen.wav");
        }

        private void PlayBlindsAreUpSound()
        {
            Console.WriteLine("Playing sound: Storen sind hochgefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-hoch-bestätigt.wav");
        }
        private void PlayBlindsAreUpDeniedSound()
        {
            Console.WriteLine("Playing sound: Storen sind hochgefahren");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Beschattung-hoch-abgebrochen.wav");
        }

        private void PlayAnswerNotRecognized()
        {
            Console.WriteLine("Playing sound: Antwort nicht erkannt");
            terminalService.RunCmd("aplay -D hw:3,0 Sounds/AudioNew/Deutsch/Antwort-nicht-erkannt.wav");
            audioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
        }

    }
}
