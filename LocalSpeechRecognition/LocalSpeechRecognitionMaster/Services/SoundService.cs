using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using LocalSpeechRecognitionMaster.DataModels;
using static System.Formats.Asn1.AsnWriter;
using System.Collections;
using Microsoft.Extensions.Configuration;

namespace LocalSpeechRecognitionMaster
{
    /// <summary>
    /// Service for managing and playing sound.
    /// </summary>
    public class SoundService
    {
        // Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Configuration paths and settings
        private readonly string _soundPath;
        private readonly string _modelPath;
        private readonly string _piperPath;

        //Dictionaries to store file paths and stereofiles
        private readonly ConcurrentDictionary<string, string> soundPaths = new();
        private readonly ConcurrentBag<string> stereoFiles = new();
                   
        // Event after playing audio
        public event EventHandler<AudioEventArgs> AudioPlayedEvent;

        public SoundService(IConfiguration configuration)
        {
            _soundPath = configuration["Paths:SoundPath"];
            _modelPath = configuration["Paths:ModelPath"];
            _piperPath = configuration["Paths:PiperPath"];
        }

        /// <summary>
        /// Initialize SoundService.
        /// Load existing sounds, generates basic sounds.
        /// </summary>
        public void Init()
        {
            Logger.Info("Initializing soundService...");
            LoadExistingSounds();

            // Generate basic sounds parallel
            Parallel.Invoke(
                () => GenerateBasicSound("Antwort nicht verstanden, bitte wiederholen!", "not_recognized"),
                () => GenerateBasicSound("Keine Antwort!", "no_answer"),
                () => GenerateBasicSound("Wann kehrst du zurück?", "absence_question"),
                () => GenerateBasicSound("Möchtest du eine Absenz angeben, oder einen Befehl ausführen?", "mode_question"),
                () => GenerateBasicSound("Willkommen zuhause, Absenz beendet", "absence_end"),
                () => GenerateBasicSound("Was kann ich für dich tun?", "action_question"),
                () => GenerateBasicSound("Aktion für dieses Gerät nicht vorhanden. Bitte wiederholen", "action_not_found"),
                () => CheckAndConvertSounds()
            );
        }

        /// <summary>
        /// Load existing sound from base directory.
        /// </summary>
        public void LoadExistingSounds()
        {
            try
            {
                if (Directory.Exists(_soundPath))
                {
                    // Load all files
                    var filePaths = Directory.EnumerateFiles(_soundPath, "*.wav", SearchOption.AllDirectories).ToList();

                    // Parallel check if existing in soundPaths and is stereo
                    Parallel.ForEach(filePaths, filePath =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (!soundPaths.ContainsKey(fileName))
                        {
                            Logger.Info($"Load existing Soundfile: {fileName}");
                            soundPaths.TryAdd(fileName, filePath);

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

        /// <summary>
        /// Generate sound file with given text and assign filename.
        /// </summary>
        /// <param name="textToSpeech">Text to convert.</param>
        /// <param name="fileName">Name of sound file.</param>
        public void GenerateBasicSound(string textToSpeech, string fileName)
        {
            string filePath = Path.Combine(_soundPath, $"{fileName}.wav");
            Logger.Info($"Generating sound file: {filePath}");

            // Generate sound file
            GenerateSound($"{textToSpeech}", filePath);
            Logger.Info($"Sound file '{filePath}' generated.");

            // Register and convert sound file to stereo
            RegisterAndConvert(fileName, filePath);
        }

        /// <summary>
        /// Generate confirmation, denial and question sound for device action.
        /// </summary>
        /// <param name="device">Device name.</param>
        /// <param name="action">Action name.</param>
        public void GenerateSoundFilesForAction(string device, string action)
        {
            try
            {
                Logger.Info($"Generate Sound for {device}, {action} started");
                string deviceSoundPath = Path.Combine(_soundPath, device);
                Directory.CreateDirectory(deviceSoundPath);
                
                //Setup Paths
                string questionPath = Path.Combine(deviceSoundPath, $"{action}_question.wav");
                string confirmedPath = Path.Combine(deviceSoundPath, $"{action}_confirmed.wav");
                string cancelledPath = Path.Combine(deviceSoundPath, $"{action}_cancelled.wav");

                // Generate sound files for the action
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

        /// <summary>
        /// Generate sound from given text.
        /// </summary>
        /// <param name="text">Text to convert.</param>
        /// <param name="filePath">path of sound file.</param>
        private void GenerateSound(string text, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    string command = $"echo '{text}' | {_piperPath} -m {_modelPath} --output_file {filePath}";
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

        /// <summary>
        /// Register file and convert to stereo.
        /// </summary>
        /// <param name="key">Key for sound file.</param>
        /// <param name="path">Path of sound file.</param>
        private void RegisterAndConvert(string key, string path)
        {
            if (!soundPaths.ContainsKey(key))
            {
                soundPaths[key] = path;
            }

            if (!stereoFiles.Contains(path))
            {
                if (IsMono(path))
                {
                    ConvertToStereo(path);
                }
                stereoFiles.Add(path);
            }
        }

        /// <summary>
        /// Convert file to stereo.
        /// </summary>
        /// <param name="filePath">Path of sound file.</param>
        private static void ConvertToStereo(string filePath)
        {
            try
            {   if (File.Exists(filePath))
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

        /// <summary>
        /// Check and convert all sound files to stereo.
        /// </summary>
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
                        stereoFiles.Add(soundPath);
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

        /// <summary>
        /// Checks if sound file is mono.
        /// </summary>
        /// <param name="filePath">Path of sound file.</param>
        /// <returns>True if file is mono,else false.</returns>
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

        /// <summary>
        /// Play sound for action key.
        /// </summary>
        /// <param name="actionKey">Key of sound file.</param>
        public async Task PlaySound(string actionKey)
        {
            try
            {
                if (soundPaths.ContainsKey(actionKey))
                {
                    
                    string filePath = soundPaths[actionKey];
                    await Task.Run(() => TerminalService.RunCmd($"aplay -D plughw:4,0 {filePath}"));
                    Logger.Info($"Played sound: {actionKey} from {filePath}");
                    
                    if (actionKey.EndsWith("_question") || actionKey.EndsWith("action_not_found"))
                    {
                        await Task.Delay(10);
                        AudioPlayedEvent?.Invoke(this, new AudioEventArgs(true));
                    }
                }
                else
                {
                    Logger.Warn("Sound file not found for action: " + actionKey);
                }
            }
            catch (KeyNotFoundException ex)
            {
                Logger.Error($"Key not found: {actionKey}, Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error playing sound for action {actionKey}: {ex.Message}");
            }
        }
    }
}
