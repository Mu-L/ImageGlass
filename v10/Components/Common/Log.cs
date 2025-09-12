/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Text;

namespace ImageGlass.Common;


public static class Log
{

    /// <summary>
    /// Outputs a message to the debug console when in debug mode.
    /// </summary>
    public static void Note(string message, string fnName = "", string className = "", string header = "")
    {
#if DEBUG
        var sb = new StringBuilder();
        sb.Append($"[LOG] {header}");

        // class name
        if (!string.IsNullOrWhiteSpace(className))
        {
            sb.Append($" {className}");
        }

        // function name
        if (!string.IsNullOrWhiteSpace(fnName))
        {
            sb.Append($" > {fnName}");
        }

        // message
        sb.Append($": {message}");

        Debug.WriteLine(sb);
#endif
    }


    /// <summary>
    /// Logs an info message.
    /// </summary>
    public static void Info(string message, string fnName = "", string className = "")
    {
        Note(message, fnName, className, "ℹ️ℹ️ℹ️");
    }


    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void Warn(string message, string fnName = "", string className = "")
    {
        Note(message, fnName, className, "⚠️⚠️⚠️");
    }


    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void Error(string message, string fnName = "", string className = "")
    {
        Note(message, fnName, className, "❌❌❌");
    }


    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void Error(Exception ex, string header = "", string fnName = "", string className = "")
    {
        Note($"{header}\n{ex.ToString()}", fnName, className, "⛔⛔⛔");
    }

}
