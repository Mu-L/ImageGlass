/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
namespace ImageGlass.SDK;

/// <summary>
/// Base class for non-hosted external plugins.
/// Handles named-pipe connection, message dispatch, and lifecycle.
/// Plugin authors subclass this and override <c>OnXxx</c> methods.
/// </summary>
public abstract class PluginBase : IDisposable
{
    private PluginClient? _client;

    /// <summary>
    /// Unique plugin identifier. Must match manifest <c>id</c>.
    /// </summary>
    public abstract string PluginId { get; }

    /// <summary>
    /// The plugin client providing host API access.
    /// </summary>
    private protected PluginClient Client => _client
        ?? throw new InvalidOperationException("Plugin not initialized.");

    /// <summary>
    /// Host API proxy — viewer, photo info, dialogs, etc.
    /// </summary>
    public IPluginHostProxy HostApi => Client.HostApi;

    /// <summary>
    /// Data directory for this plugin (set by host during INIT).
    /// Plugins can store local caches or state here.
    /// </summary>
    public string DataDirectory { get; internal set; } = string.Empty;

    /// <summary>
    /// Current theme information (updated by host events).
    /// </summary>
    public ThemeInfo? CurrentTheme { get; internal set; }


    #region Lifecycle hooks

    /// <summary>
    /// Called after connection established and settings loaded.
    /// </summary>
    protected internal virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when the host requests plugin execution (non-hosted).
    /// </summary>
    protected internal virtual Task OnExecuteAsync(CancellationToken ct) => Task.CompletedTask;

    #endregion


    #region Event hooks

    /// <summary>
    /// Called when the current photo changes or is unloaded.
    /// </summary>
    protected internal virtual void OnPhotoChanged(PhotoChangedEventArgs e) { }

    /// <summary>
    /// Called when theme changes.
    /// </summary>
    protected internal virtual void OnThemeChanged(ThemeInfo theme) { }

    /// <summary>
    /// Called when language changes.
    /// </summary>
    protected internal virtual void OnLanguageChanged(LanguageChangedEventArgs e) { }

    /// <summary>
    /// Called when cursor moves over viewer (only if subscribed).
    /// </summary>
    protected internal virtual void OnPointerMoved(PointerEventArgs e) { }

    /// <summary>
    /// Called when click on viewer (only if subscribed).
    /// </summary>
    protected internal virtual void OnPointerPressed(PointerEventArgs e) { }

    /// <summary>
    /// Called when selection changes (only if subscribed).
    /// </summary>
    protected internal virtual void OnSelectionChanged(SelectionEventArgs? e) { }

    /// <summary>
    /// Called when animation frame changes (only if subscribed).
    /// </summary>
    protected internal virtual void OnFrameChanged(int frameIndex) { }

    #endregion


    #region Entry point

    /// <summary>
    /// Main entry point. Call from <c>Program.Main(args)</c>.
    /// Connects to host, runs message loop until shutdown.
    /// </summary>
    public async Task RunAsync(string[] args)
    {
        var pipeName = ParsePipeNameFromArgs(args);
        if (string.IsNullOrEmpty(pipeName))
        {
            throw new ArgumentException(
                "Missing --pipe argument. This plugin must be launched by ImageGlass.");
        }

        _client = new PluginClient(pipeName, this);
        await _client.ConnectAndRunAsync().ConfigureAwait(false);
    }

    private static string? ParsePipeNameFromArgs(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--pipe", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    #endregion


    public virtual void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
