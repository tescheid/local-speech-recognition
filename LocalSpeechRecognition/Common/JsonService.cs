    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using static System.Collections.Specialized.BitVector32;
    using NLog;
    

    namespace Common
    {
        public class JsonService
        {
            private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

            private readonly string filePath;
            private SpeechRecognitionDataModel data;
            private DateTime lastModified;
            private FileSystemWatcher watcher;
            private readonly int maxRetries = 10;
            private readonly int retryDelayms = 100;
            public event EventHandler<SpeechRecognitionDataModel> FileChangedEvent; //durch das <MyEventArgs> kann man sich das delegate sparen.
            private System.Timers.Timer debounceTimer;
            public JsonService(string filePath)
            {
                this.filePath = filePath;
                
            }

            public void Init()
            {
                Logger.Info("Initializing JsonService...");
                try
                {
                    ClearJsonFile();

                    
                    data = LoadJsonFile();
                    lastModified = File.GetLastWriteTime(filePath);
                    watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
                    {
                        Filter = Path.GetFileName(filePath)
                    };
                    watcher.Changed += FileChanged;
                    Logger.Info("JsonService initialized and file watcher started.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error initializing JsonService.");
                }
            }


            public SpeechRecognitionDataModel LoadJsonFile()
            {
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath).Trim();
                        if (string.IsNullOrWhiteSpace(json) || json == "{}")
                        {
                            Logger.Warn("Empty or invalid JSON content.");
                            continue;
                        }

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
                        Thread.Sleep(retryDelayms);
                    }
                }
                return new SpeechRecognitionDataModel();
            }

            private void FileChanged(object sender, FileSystemEventArgs e)
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    Logger.Debug("File change detected, starting debounce timer.");
                    if (debounceTimer == null)
                    {
                        debounceTimer = new System.Timers.Timer(500)
                        {
                        AutoReset = false
                        };
                        debounceTimer.Elapsed += DebounceElapsed;
                    }
                    debounceTimer.Stop();
                    debounceTimer.Start(); 
                }
            }

            private void DebounceElapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                debounceTimer.Stop(); 
                DateTime currentModified = File.GetLastWriteTime(filePath);
                if (currentModified != lastModified)
                {
                    lastModified = currentModified;
                    data = LoadJsonFile();
                    string dataOutput = $"Intent: {data.Intent}, Slots: {JsonSerializer.Serialize(data.Slots, new JsonSerializerOptions { WriteIndented = true })}";
                    Logger.Info($"File has changed. Reloading {dataOutput}.");
                    FileChangedEvent?.Invoke(this, data);
                }
            }

            public void ClearJsonFile()
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    File.WriteAllText(filePath, "{}");
                    Logger.Info("JSON file cleared.");
                    watcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error clearing JSON file.");
                }
            }
        }
    }
