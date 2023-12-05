using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster
{
    public class TerminalService
    {
        public TerminalService() {
        }    

       public void RunCmd(string cmd)
        {
            //string command = "sudo aplay -D hw:3,0 test.wav";

            // Create process start info
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-c \"{cmd}\""
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                // Start the process
                process.Start();

                // Wait for the process to exit
                process.WaitForExit();

                // Output any errors
                string errors = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errors))
                {
                    Console.WriteLine($"Error: {errors}");
                }
                else
                {
                    Console.WriteLine("Command executed successfully");
                }
            }
        }
    }
}
