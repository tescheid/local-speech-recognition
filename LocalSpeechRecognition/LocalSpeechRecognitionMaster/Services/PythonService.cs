using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster.Services
{
    public class PythonService
    {
        string pythonPath = "python";
        string scriptPath = ""; 

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
            Console.WriteLine("asdhflajskfh");
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = pythonPath;
            start.Arguments = scriptPath;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
            }
        }

        /*
        static void RunCmd()
        {
            // Path to the Python interpreter (python.exe) on your system.
            string pythonPath = "C:\\Python39\\python.exe"; // Replace with the actual path.

            // Path to your Python script.
            string scriptPath = "C:\\Users\\domin\\Documents\\Dev\\PAIND\\local-speech-recognition\\LocalSpeechRecognition\\LocalSpeechRecognitionMaster\\Python\\test.py"; // Replace with the actual path.

            // Command to run the Python script.
            string command = $"\"{pythonPath}\" \"{scriptPath}\"";

            // Create a ProcessStartInfo object to configure the process.
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"/C {command}" // /C tells cmd.exe to run the command and then terminate.
            };

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;

                // Start the process.
                process.Start();

                // Read the output and errors (if needed).
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Wait for the process to finish.
                process.WaitForExit();

                // Display the output and errors.
                Console.WriteLine("Output:");
                Console.WriteLine(output);
                Console.WriteLine("Errors:");
                Console.WriteLine(error);
            }
        }

        */
    }
}
