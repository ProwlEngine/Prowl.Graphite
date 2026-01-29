// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Graphite;

/// <summary>
/// Logging severity levels.
/// </summary>
public enum LogSeverity
{
    Normal,
    Warning,
    Error
}

/// <summary>
/// Delegate for log message handlers.
/// </summary>
public delegate void LogHandler(string message, LogSeverity severity);

/// <summary>
/// Simple debug/logging class for Graphite.
/// </summary>
public static class Debug
{
    /// <summary>
    /// Event fired when a log message is emitted.
    /// Subscribe to this to integrate with your own logging system.
    /// </summary>
    public static event LogHandler? OnLog;

    public static void Log(string message)
    {
        WriteLog(message, LogSeverity.Normal, ConsoleColor.White);
    }

    public static void LogWarning(string message)
    {
        WriteLog(message, LogSeverity.Warning, ConsoleColor.Yellow);
    }

    public static void LogError(string message)
    {
        WriteLog(message, LogSeverity.Error, ConsoleColor.Red);
    }

    private static void WriteLog(string message, LogSeverity severity, ConsoleColor color)
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prevColor;

        OnLog?.Invoke(message, severity);
    }
}
