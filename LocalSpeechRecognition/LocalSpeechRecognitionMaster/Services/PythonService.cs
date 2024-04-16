using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster.Services
{
    public class PythonService
    {
        string pythonPath = "python";
        string scriptPath = "";
        private Process pythonProcess;

        public PythonService(string scriptPath) {
            this.scriptPath = scriptPath;
        }

        public void RunCmd()
        {
            Thread t = new Thread(ExecuteFile);
            t.IsBackground = true;
            t.Start();
        }

        private void ExecuteFile()
        {
            
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            pythonProcess = Process.Start(start);
            Console.WriteLine("Started Python script");
        }

        public void SendStartCommand()
        {
            try
            {
                using var client = new TcpClient("localhost", 12345);
                using var stream = client.GetStream();
                var command = Encoding.ASCII.GetBytes("start");
                stream.Write(command, 0, command.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending start command: {e.Message}");
            }
        }
    }
}
