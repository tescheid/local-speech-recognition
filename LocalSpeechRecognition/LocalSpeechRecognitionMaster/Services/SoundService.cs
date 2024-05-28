using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using LocalSpeechRecognitionMaster.DataModels;

namespace LocalSpeechRecognitionMaster
{
    public class SoundService
    {
        private readonly string soundPath = "/home/auxilium/netcore/LocalSpeechRecognitionMaster/Sounds/";
        private readonly ConcurrentDictionary<string, string> soundPaths = new();
        private readonly ConcurrentBag<string> stereoFiles = new();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        

        public event EventHandler<AudioEventArgs> AudioPlayedEvent;

        public SoundService()
        {
            
        }
        public void Init()
        {
            Logger.Info("Initializing soundService...");
            Parallel.Invoke(
                () => GenerateBasicSound("Antwort nicht verstanden, bitte wiederholen!", "not_recognized"),
                () => GenerateBasicSound("Keine Antwort!", "no_answer"),
                () => GenerateBasicSound("Wann kehrst du zurück?", "absence_question"),
                () => GenerateBasicSound("Möchtest du eine Absenz angeben, oder einen Befehl ausführen?", "mode_question"),
                () => GenerateBasicSound("Willkommen zuhause, Absenz beendet", "absence_end"),
                () => GenerateBasicSound("Was kann ich für dich tun?", "action_question"),
                () => GenerateBasicSound("Aktion für dieses Gerät nicht vorhanden. Bitte wiederholen", "action_not_found")
            );

            Parallel.Invoke(
                () => LoadExistingSounds(),
                () => CheckAndConvertSounds()
            );
        }

        public void LoadExistingSounds()
        {
            try
            {
                if (Directory.Exists(soundPath))
                {
                    var filePaths = Directory.EnumerateFiles(soundPath, "*.wav", SearchOption.AllDirectories).ToList();
                    Parallel.ForEach(filePaths, filePath =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (!soundPaths.ContainsKey(fileName))
                        {
                            Logger.Info($"Load existing Soundfile: {fileName}");

                            // Speichere die Zuordnung von Namen zu Pfaden in `soundPaths`
                            soundPaths.TryAdd(fileName, filePath);

                            // Prüfe, ob die Datei bereits Stereo ist
                            if (!IsMono(filePath))
                            {
                                stereoFiles.Add(filePath);
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error loading existing sounds: {e.Message}");
            }
        }

        public void GenerateSoundFilesForAction(string device, string action)
        {
            try
            {
                Logger.Info($"Generate Sound for {device}, {action} started");
                string deviceSoundPath = Path.Combine(soundPath, device);
                Directory.CreateDirectory(deviceSoundPath);
                
                string questionPath = Path.Combine(deviceSoundPath, $"{action}_question.wav");
                string confirmedPath = Path.Combine(deviceSoundPath, $"{action}_confirmed.wav");
                string cancelledPath = Path.Combine(deviceSoundPath, $"{action}_cancelled.wav");

                GenerateSound($"Möchten Sie das Kommando {device} {action} ausführen?", questionPath);
                GenerateSound($"{device} {action} bestätigt.", confirmedPath);
                GenerateSound($"{device} {action} abgebrochen.", cancelledPath);

                RegisterAndConvert($"{device}_{action}_question", questionPath);
                RegisterAndConvert($"{device}_{action}_confirmed", confirmedPath);
                RegisterAndConvert($"{device}_{action}_cancelled", cancelledPath);
                Logger.Info($"Generate Sound for {device}, {action} ended");
            }
            catch (Exception e)
            {
                Logger.Error($"Error generating sound files for action: {e.Message}");
            }
        }
        public void GenerateBasicSound(string textToSpeech, string fileName)
        {
            string filePath = Path.Combine(soundPath, $"{fileName}.wav");
            Logger.Info($"Generating sound file: {filePath}");
            GenerateSound($"{textToSpeech}", filePath);
            Logger.Info($"Sound file '{filePath}' generated.");
            RegisterAndConvert(fileName, filePath);
        }


        private static void GenerateSound(string text, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    string command = $"echo '{text}' | /home/auxilium/piper/piper/piper -m /home/auxilium/piper/thorsten_voice/de_DE-thorsten-medium.onnx --output_file {filePath}";
                    TerminalService.RunCmd(command);
                    Logger.Info($"Generated sound for {filePath} with text: {text}");
                }
                else
                {
                    Logger.Info($"Sound file {filePath} already exists.");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error generating sound: {e.Message}");
            }
        }

        private void RegisterAndConvert(string key, string path)
        {
            // Speichere den Pfad in `soundPaths`
            if (!soundPaths.ContainsKey(key))
            {
                soundPaths[key] = path;
            }

            // Konvertiere nur, wenn die Datei noch nicht Stereo ist
            if (!stereoFiles.Contains(path))
            {
                if (IsMono(path))
                {
                    ConvertToStereo(path);
                }
                stereoFiles.Add(path); // Nach erfolgreicher Konvertierung als Stereo markieren
            }
        }

        
        private void CheckAndConvertSounds()
        {
            try
            {
                Logger.Info($"Check and Convert start");

                Parallel.ForEach(soundPaths.Values, soundPath =>
                {
                    if (!stereoFiles.Contains(soundPath) && IsMono(soundPath))
                    {
                        ConvertToStereo(soundPath);
                        stereoFiles.Add(soundPath); // Nach erfolgreicher Konvertierung hinzufügen
                    }
                    else
                    {
                        Logger.Info($"Skipping conversion for {soundPath}, already stereo.");
                    }
                });
                Logger.Info($"Check and Convert end");
            }
            catch (Exception e)
            {
                Logger.Error($"Error checking and converting sounds: {e.Message}");
            }
        }

        private static void ConvertToStereo(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string tempFilePath = Path.ChangeExtension(filePath, ".temp.wav");
                    string command = $"ffmpeg -i {filePath} -ac 2 {tempFilePath}";

                    TerminalService.RunCmd(command);

                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(filePath);
                        File.Move(tempFilePath, filePath);
                        Logger.Debug($"Converted {filePath} to stereo.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error converting to stereo: {e.Message}");
            }
        }

        private static bool IsMono(string filePath)
        {
            try
            {
                string command = $"ffprobe -v error -select_streams a:0 -show_entries stream=channels -of csv=p=0 {filePath}";
                string output = TerminalService.RunCmdAndReturnOutput(command);
                return output.Trim() == "1";
            }
            catch (Exception e)
            {
                Logger.Error($"Error checking if file is mono: {e.Message}");
                return false;
            }
        }

        public Task PlaySound(string actionKey)
        {
            try
            {
                if (soundPaths.ContainsKey(actionKey))
                {
                    
                    string filePath = soundPaths[actionKey];
                    TerminalService.RunCmd($"aplay -D plughw:4,0 {filePath}");
                    Logger.Info($"Played sound: {actionKey} from {filePath}");
                    
                    if (actionKey.EndsWith("_question") || actionKey == "not_recognized" || actionKey == "action_not_found")
                    {
                        Thread.Sleep(20);
                        AudioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
                    }
                }
                else
                {
                    Logger.Warn("Sound file not found for action: " + actionKey);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error playing sound: {e.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
