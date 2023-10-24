using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PAIND_Communication
{
    public class JsonService
    {
        private string filePath;
        private SpeechRecognitionDataModel data;
        private DateTime lastModified;
        private FileSystemWatcher watcher;
        private int maxRetries = 10;
        private int retryDelay_ms = 100;
        public event EventHandler<SpeechRecognitionDataModel> fileChangedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public JsonService(string filePath)
        {
            this.filePath = filePath;
            data = LoadJsonFile();
            Console.WriteLine(data.text);
            lastModified = File.GetLastWriteTime(filePath);

            watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
            watcher.Filter = Path.GetFileName(filePath);
            watcher.Changed += FileChanged;
            watcher.EnableRaisingEvents = true;

            //Timer checkTimer = new Timer(CheckFilePeriodically, null, 0, 5000); // Check every 5 seconds
        }


     

        private SpeechRecognitionDataModel LoadJsonFile()
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<SpeechRecognitionDataModel>(json);
                }
                catch (IOException)
                {
                    Thread.Sleep(retryDelay_ms);
                }
            }
            return new SpeechRecognitionDataModel();
        }



        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                DateTime currentModified = File.GetLastWriteTime(filePath);
                if (currentModified != lastModified)
                {
                    lastModified = currentModified;
                    data = LoadJsonFile();
                    Console.WriteLine("File has changed. Reloading data.");
                    fileChangedEvent?.Invoke(this, data);

                }
            }
        }


        public SpeechRecognitionDataModel GetData()
        {
            return data;
        }

        public string getFilePath()
        {
            return filePath;
        }
       

      


    }
}
