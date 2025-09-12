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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common;


public partial class BHelper
{
    private static readonly TaskFactory _taskFactory = new(
        CancellationToken.None, TaskCreationOptions.None,
        TaskContinuationOptions.None, TaskScheduler.Default);


    /// <summary>
    /// Builds correct file path for executable and app protocol.
    /// </summary>
    public static (string Executable, string Args) BuildExeArgs(string executable, string arguments, string currentFilePath = "")
    {
        var exe = executable.Trim();
        var isAppProtocol = exe.EndsWith(':');

        // exclude the double quotes if the executable is app protocol
        var filePath = isAppProtocol ? currentFilePath : $"\"{currentFilePath}\"";

        var args = arguments.Replace(Const.FILE_MACRO, filePath);

        return (Executable: exe, Args: args);
    }


    /// <summary>
    /// Run a command, supports auto-elevating process privilege
    /// if admin permission is required.
    /// </summary>
    public static async Task<IgExitCode> RunExeCmd(string exePath, string args, bool waitForExit = true, bool appendIgArgs = true, bool showError = false)
    {
        IgExitCode code;

        try
        {
            if (appendIgArgs)
            {
                args += $" {IgExeParams.HIDE_ADMIN_REQUIRED_ERROR_UI}";
            }

            code = (IgExitCode)await RunExeAsync(exePath, args, false, waitForExit, showError);


            // If that fails due to privs error, re-attempt with admin privs.
            if (code == IgExitCode.AdminRequired)
            {
                code = (IgExitCode)await RunExeAsync(
                    exePath,
                    args,
                    asAdmin: true,
                    waitForExit: waitForExit);
            }
        }
        catch
        {
            code = IgExitCode.Error;
        }

        return code;
    }


    /// <summary>
    /// Runs executable.
    /// </summary>
    public static async Task<int> RunExeAsync(string path, string args, bool asAdmin = false, bool waitForExit = false, bool showError = false)
    {
        var proc = new Process();

        // path is a protocal
        if (path.EndsWith(':'))
        {
            var url = $"{path}{args}";
            proc.StartInfo.FileName = url;
        }
        else
        {
            proc.StartInfo.FileName = path;
            proc.StartInfo.Arguments = args;
        }

        proc.StartInfo.Verb = asAdmin ? "runas" : "";
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.ErrorDialog = showError;

        try
        {
            proc.Start();

            if (waitForExit)
            {
                await proc.WaitForExitAsync();

                return proc.ExitCode;
            }

            return (int)IgExitCode.Done;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("system cannot find the file", StringComparison.OrdinalIgnoreCase))
            {
                return (int)IgExitCode.Error_FileNotFound;
            }

            return (int)IgExitCode.Error;
        }
    }


    /// <summary>
    /// Runs an async function synchronous.
    /// Source: <see href="https://github.com/aspnet/AspNetIdentity/blob/b7826741279450c58b230ece98bd04b4815beabf/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs" />
    /// </summary>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        var cultureUi = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.CurrentCulture;

        return _taskFactory.StartNew(() =>
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = cultureUi;
            return func();
        }).Unwrap().GetAwaiter().GetResult();
    }


    /// <summary>
    /// Runs an async function synchronous.
    /// Source: <see href="https://github.com/aspnet/AspNetIdentity/blob/b7826741279450c58b230ece98bd04b4815beabf/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs" />
    /// </summary>
    public static void RunSync(Func<Task> func)
    {
        var cultureUi = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.CurrentCulture;

        _taskFactory.StartNew(() =>
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = cultureUi;
            return func();
        }).Unwrap().GetAwaiter().GetResult();
    }

}
