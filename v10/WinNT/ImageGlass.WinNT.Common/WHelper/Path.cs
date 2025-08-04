using ImageGlass.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Windows.System;

namespace ImageGlass.WinNT.Common;

public static partial class WHelper
{
    /// <summary>
    /// Resolves a relative/protocol/link path to absolute path
    /// </summary>
    /// <param name="inputPath">A path</param>
    /// <returns></returns>
    public static string ResolvePath(string? inputPath)
    {
        if (string.IsNullOrEmpty(inputPath))
            return inputPath ?? "";

        var path = inputPath;
        const string protocol = Const.APP_PROTOCOL + ":";

        // If inputPath is URI Scheme
        if (path.StartsWith(protocol))
        {
            // Retrieve the real path
            path = Uri.UnescapeDataString(path).Remove(0, protocol.Length);
        }

        // Parse environment vars to absolute path
        path = Environment.ExpandEnvironmentVariables(path);

        if (string.Equals(Path.GetExtension(inputPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            path = FileShortcutApi.GetTargetPathFromShortcut(path);
        }

        return path;
    }


    /// <summary>
    /// Open URL in the default browser.
    /// </summary>
    public static async Task OpenUrlAsync(string? url, string campaign = "from_unknown")
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var ub = new UriBuilder(url);
            var queries = HttpUtility.ParseQueryString(ub.Query);
            queries["utm_source"] = "app_TODO"; // TODO: App.Version;
            queries["utm_medium"] = "app_click";
            queries["utm_campaign"] = campaign;

            ub.Query = queries.ToString();

            _ = await Launcher.LaunchUriAsync(ub.Uri);
        }
        catch { }
    }

    /// <summary>
    /// Opens file path in Explorer and selects it.
    /// </summary>
    public static void OpenFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            // TODO:
            //ExplorerApi.OpenFolderAndSelectItem(filePath);
        }
        catch
        {
            using var proc = Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }


    /// <summary>
    /// Opens the folder path in Explorer, creates the fodler path if not existed.
    /// </summary>
    public static void OpenFolderPath(string? dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath)) return;

        try
        {
            Directory.CreateDirectory(dirPath);
        }
        catch { }

        try
        {
            using var proc = Process.Start("explorer.exe", $"\"{dirPath}\"");
        }
        catch { }
    }

}
