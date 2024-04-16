// Author: rstewa · https://github.com/rstewa
// Created: 4/13/2024
// Updated: 4/13/2024

using System;
using System.IO;
using Audibly.App.Services.Interfaces;

namespace Audibly.App.Services;

public class LoggingService(string logFilePath) : IloggingService
{
    public void Log(string message)
    {
        var logMessage = $"{DateTime.Now}: {message}";
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }
    
    public void LogError(Exception e)
    {
        var logMessage = $"ERROR: {DateTime.Now}: {e.Message}";
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }
}