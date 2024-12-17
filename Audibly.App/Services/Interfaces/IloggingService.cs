// Author: rstewa · https://github.com/rstewa
// Created: 4/13/2024
// Updated: 4/13/2024

using System;

namespace Audibly.App.Services.Interfaces;

public interface IloggingService
{
    void Log(string message);
    void LogError(Exception e, bool logToSentry = false);
}