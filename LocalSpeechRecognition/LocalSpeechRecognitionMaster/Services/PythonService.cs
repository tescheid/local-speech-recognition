using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace LocalSpeechRecognitionMaster.Services
{
    public class PythonService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        readonly string pythonPath = "python";
        readonly string scriptPath = "";
        
        private TcpClient client;
        private NetworkStream stream;

        public PythonService(string scriptPath) {
            this.scriptPath = scriptPath;
        }

        public void Init()
        {
            Logger.Info("Initializing PythonService...");
            KillPortThread();
            Thread.Sleep(100);
            RunCmd();
            Thread.Sleep(100);
            InitializeConnection();
        }

        private static void KillPortThread()
        {
            TerminalService.RunCmd("sudo fuser -k 41000/tcp");
        }
        private void InitializeConnection()
        {
            int maxRetries = 5;
            int retryCount = 0;
            int retryDelay = 2000;
            while (retryCount < maxRetries)
            {
                try
                {
                    client = new TcpClient("localhost", 41000);
                    stream = client.GetStream();
                    Logger.Info("Connection with speechIntent established.");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error during connection init to speechIntent.");
                    KillPortThread();
                    Thread.Sleep(100);
                    RunCmd();
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Logger.Info($"Retrying connection in {retryDelay / 1000} seconds... ({retryCount}/{maxRetries})");
                        Thread.Sleep(retryDelay);
                    }
                    else
                    {
                        Logger.Error("Maximum retry attempts, check speechIntet!");
                    }
                }
            }
        }

        public void RunCmd()
        {
            Thread t = new(ExecuteFile){IsBackground = true};
            t.Start();
        }

        private void ExecuteFile()
        {
            
            ProcessStartInfo startInfo = new()
            {
                FileName = pythonPath,
                Arguments = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new() { StartInfo = startInfo };
            process.OutputDataReceived += (sender, args) => Logger.Info($"STI Output: {args.Data}");
            process.ErrorDataReceived += (sender, args) => Logger.Error($"STI Error: {args.Data}");

            process.Start();
            Logger.Info("STI Recognition started.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
        }

         public async Task SendCommand(string command)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int retryDelay = 2000;
            while (retryCount < maxRetries)
            {
                if (client == null || !client.Connected)
                {
                    Logger.Warn("Retry connection...");
                    InitializeConnection();
                }
                try
                {
                    byte[] commandBytes = Encoding.ASCII.GetBytes($"{command}\n");
                    await stream.WriteAsync(commandBytes);
                    Logger.Info($"Sent command to STI: {command}");

                    byte[] buffer = new byte[256];
                    int bytesRead = await stream.ReadAsync(buffer);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    Logger.Info($"Received response from STI: {response}");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error sending command.");
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Logger.Warn($"Retrying to send command in {retryDelay / 1000} seconds... ({retryCount}/{maxRetries})");
                        await Task.Delay(retryDelay);
                    }
                    else
                    {
                        Logger.Error("Maximum retry attempts reached. Failed to send command.");
                    }
                }
            }
        }

        public void CloseConnection()
        {
            try
            {
                stream?.Close();
                client?.Close();
                Logger.Info("Connection closed with Python server.");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error closing connection.");
            }
        }
    }
}
