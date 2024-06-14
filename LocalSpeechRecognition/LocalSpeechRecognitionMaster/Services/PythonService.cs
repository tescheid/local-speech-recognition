using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;

namespace LocalSpeechRecognitionMaster.Services
{
    /// <summary>
    /// Service for handling external python speech recognition.
    /// </summary>
    public class PythonService
    {
        // Logger instance
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Configuration Paths and settings
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly int _port;

        // TCP Communcation
        private TcpClient client;
        private NetworkStream stream;

        // Thread for Python script
        private Thread PythonScript;

        /// <summary>
        /// Constructor PythonService.
        /// Initializes service with configuration.
        /// </summary>
        public PythonService(IConfiguration configuration) {
            _scriptPath = configuration["Paths:IntentPath"];
            _pythonPath = configuration["Paths:PythonPath"];
            _port = int.Parse(configuration["TCP:Port"]);
        }

        /// <summary>
        /// Initialize PythonService.
        /// </summary>
        public void Init()
        {
            Logger.Info("Initializing PythonService...");
            StartPython();

            // Wait for the Python script to start
            Thread.Sleep(300); 

            InitializeConnection();
        }

        /// <summary>
        /// Kills any process using this port.
        /// </summary>
        private static void KillPortThread(int port)
        {
            TerminalService.RunCmd($"sudo fuser -k {port}/tcp");
        }

        /// <summary>
        /// Initializes TCP connection to Python server.
        /// </summary>
        private void InitializeConnection()
        {
            int maxRetries = 5;
            int retryCount = 0;
            int retryDelay = 2000;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Try to connect to Python
                    client = new TcpClient("localhost", _port);
                    stream = client.GetStream();
                    Logger.Info("Connection with speechIntent established.");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error during connection init to speechIntent.");
                    
                    // Restart the Python script
                    StartPython();
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Logger.Info($"Retrying connection in {retryDelay / 1000} seconds... ({retryCount}/{maxRetries})");
                        Thread.Sleep(retryDelay); // Wait before retry
                    }
                    else
                    {
                        Logger.Error("Maximum retry attempts, check speechIntet!");
                    }
                }
            }
        }

        /// <summary>
        /// Start Python script in background.
        /// </summary>
        private void StartPython()
        {
            if (PythonScript != null && PythonScript.IsAlive)
            {
                KillPortThread(_port); 
                Thread.Sleep(100);
            }
            PythonScript = new Thread(ExecuteFile) { IsBackground = true };
            PythonScript.Start();
        }

        /// <summary>
        /// Execute Python file.
        /// </summary>
        private void ExecuteFile()
        {
            
            ProcessStartInfo startInfo = new()
            {
                FileName = _pythonPath,// Path to Python
                Arguments = _scriptPath,// Path to Python script
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
        }

        /// <summary>
        /// Send command to Python server.
        /// </summary>
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

                    // Reinitialize if not connected
                    InitializeConnection(); 
                }
                try
                {
                    // Convert command to bytes and send it
                    byte[] commandBytes = Encoding.ASCII.GetBytes($"{command}\n");
                    await stream.WriteAsync(commandBytes, 0, commandBytes.Length);
                    Logger.Info($"Sent command to STI: {command}");

                    // Read response
                    byte[] buffer = new byte[256];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
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
                        await Task.Delay(retryDelay); // Wait before retry
                    }
                    else
                    {
                        Logger.Error("Maximum retry attempts reached. Failed to send command.");
                    }
                }
            }
        }
    }
}
