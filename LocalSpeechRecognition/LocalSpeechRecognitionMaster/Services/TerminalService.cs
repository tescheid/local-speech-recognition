﻿using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSpeechRecognitionMaster
{
    /// <summary>
    /// TerminalService used to execute terminal commands.
    /// </summary>
    public class TerminalService
    {
        // Logger instane
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Initialize new instance of TerminalService.
        /// </summary>
        public TerminalService() 
        {
            Logger.Info("TerminalService initialized.");
        }

        /// <summary>
        /// Execute command without returning output.
        /// </summary>
        /// <param name="cmd">Command to execute.</param>
        public static void RunCmd(string cmd)
        {
            Logger.Debug($"Running command: {cmd}");
            // Create process start info
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "/bin/bash",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-c \"{cmd}\""
            };

            using Process process = new() { StartInfo = processStartInfo };
            try
            {
                process.Start();
                Logger.Debug("Process started.");
    
                process.WaitForExit();
                Logger.Debug("Process exited.");

                string output = process.StandardOutput.ReadToEnd();
                Logger.Debug($"Command output: {output}");

                string errors = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errors))
                {
                    Logger.Warn($"Command errors: {errors}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while running command: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute terminal command and return output.
        /// </summary>
        /// <param name="cmd">Command to execute.</param>
        /// <returns>Output of command.</returns>
        public static string RunCmdAndReturnOutput(string cmd)
        {
            Logger.Debug($"Running command and returning output: {cmd}");

            ProcessStartInfo startInfo = new()
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmd}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = startInfo };
            try
            {
                process.Start();
                Logger.Debug("Process started.");

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                Logger.Debug("Process exited.");

                string errors = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errors))
                {
                    Logger.Warn($"Command errors: {errors}");
                }

                Logger.Info($"Command output: {output}");
                return output;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while running command: {ex.Message}");
                return string.Empty;
            }
        }

    }
}
