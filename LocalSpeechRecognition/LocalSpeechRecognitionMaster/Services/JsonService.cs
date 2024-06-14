using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using NLog;
using LocalSpeechRecognitionMaster.DataModels;


namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for handling JSON file operations related to speech recognition data.
    /// </summary>
    public class JsonService
    {
        //Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Path to speech recognition output file
        private readonly string _filePath;

        // Data model for speech recognition
        private SpeechRecognitionDataModel data;

        // Timestamp of last file modification
        private DateTime lastModified;

        // Watcher for changes of JSON file
        private FileSystemWatcher watcher;

        // Maximum retries loading JSON file and delay
        private readonly int maxRetries = 3;
        private readonly int retryDelayms = 500;
        
        // Event triggered when file changes
        public event EventHandler<SpeechRecognitionDataModel> FileChangedEvent; 
        
        // Timer for debouncing file change events
        private System.Timers.Timer debounceTimer;

        /// <summary>
        /// Constructor for JsonService.
        /// Initializes file path for JSON file.
        /// </summary>
        /// <param name="filePath">The path to the JSON file.</param>
        public JsonService(string filePath)
        {
            this._filePath = filePath;

        }

        /// <summary>
        /// Initializes JsonService.
        /// Setup filewatcher and load initial data.
        /// </summary>
        public void Init()
        {
            Logger.Info("Initializing JsonService...");
            try
            {   
                // Clear JSON initial
                ClearJsonFile(); 
                
                // Load initial data, empty, should repeat
                data = LoadJsonFile(); 

                // Get the last modified timestamp of the file
                lastModified = File.GetLastWriteTime(_filePath);

                // Set up the Watcher
                watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath))
                {
                    Filter = Path.GetFileName(_filePath)
                };
                watcher.Changed += FileChanged;
                Logger.Info("JsonService initialized and filewatcher started.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing JsonService.");
            }
        }

        /// <summary>
        /// Load JSON file, deserialize it into a SpeechRecognitionDataModel.
        /// Retries a few times if failed.
        /// </summary>
        /// <returns>Deserialized SpeechRecognitionDataModel.</returns>
        public SpeechRecognitionDataModel LoadJsonFile()
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // Read JSON and trim whitespace
                    string json = File.ReadAllText(_filePath).Trim();
                    if (string.IsNullOrWhiteSpace(json) || json == "{}")
                    {
                        Logger.Warn("Empty or invalid JSON content.");
                        continue;
                    }
                    // Deserialize JSON into the data model
                    SpeechRecognitionDataModel result = JsonSerializer.Deserialize<SpeechRecognitionDataModel>(json);
                    if (result != null)
                    {
                        Logger.Info($"Successfully loaded and parsed JSON: Intent = {result.Intent}, Slots = {JsonSerializer.Serialize(result.Slots)}");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error reading or deserializing the file.");
                    // Wait before retrying
                    Thread.Sleep(retryDelayms);
                }
            }
            return new SpeechRecognitionDataModel();
        }

        /// <summary>
        /// Handles file changed event.
        /// Reload JSON data and raise FileChangedEvent after debounce.
        /// </summary>
        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                Logger.Debug("File change detected, starting debounce timer.");
                if (debounceTimer == null)
                {
                    // Initialize debounce
                    debounceTimer = new System.Timers.Timer(200)
                    {
                        AutoReset = false
                    };
                    debounceTimer.Elapsed += DebounceElapsed;
                }
                // Stop to reset interval
                debounceTimer.Stop(); 

                DateTime currentModified = File.GetLastWriteTime(_filePath);
                if (currentModified != lastModified)
                {
                    // Update timestamp
                    lastModified = currentModified;

                    // Reload data from JSON
                    data = LoadJsonFile();

                    // Create output for logging
                    string dataOutput = $"Intent: {data.Intent}, " +
                    $"Slots: {JsonSerializer.Serialize(data.Slots, new JsonSerializerOptions { WriteIndented = true })}";
                    Logger.Info($"File has changed: {dataOutput}.");

                    // Trigger file changed event
                    FileChangedEvent?.Invoke(this, data);
                }
                // Restart the debounce timer
                debounceTimer.Start();
                Logger.Debug("Debounce start");
            }
        }

        /// <summary>
        /// Handles debounce elapsed .
        /// Stops debounce timer.
        /// </summary>
        private void DebounceElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            debounceTimer.Stop();
            Logger.Debug("Debounce stopped");
        }

        /// <summary>
        /// Clear JSON file with empty JSON.
        /// Disable filewatcher to prevent recursive events.
        /// </summary>
        public void ClearJsonFile()
        {
            try
            {
                // Disable watcher
                watcher.EnableRaisingEvents = false;

                // Write empty JSON 
                File.WriteAllText(_filePath, "{}");
                Logger.Info("JSON file cleared.");

                //Enable watcher
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error clearing JSON file.");
            }
        }
    }
}
