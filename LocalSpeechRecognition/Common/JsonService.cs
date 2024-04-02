using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Common
{
    public class JsonService
    {
        private string filePath;
        private SpeechRecognitionDataModel data;
        private DateTime lastModified;
        private FileSystemWatcher watcher;
        private int maxRetries = 10;
        private int retryDelayms = 100;
        public event EventHandler<SpeechRecognitionDataModel> fileChangedEvent; //durch das <MyEventArgs> kann man sich das delegate spahren.

        public JsonService(string filePath)
        {
            this.filePath = filePath;
            try
            {
                data = LoadJsonFile();
                lastModified = File.GetLastWriteTime(filePath);
                watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
                watcher.Filter = Path.GetFileName(filePath);
                watcher.Changed += FileChanged;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
     

        public SpeechRecognitionDataModel LoadJsonFile()
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    string json = File.ReadAllText(filePath).Replace("\\","");

                    Console.WriteLine(json+"------------------------------------------");
                    return JsonSerializer.Deserialize<SpeechRecognitionDataModel>(json);
                }
                catch (IOException)
                {
                    Thread.Sleep(retryDelayms);
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
