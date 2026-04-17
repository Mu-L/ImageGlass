/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
namespace ImageGlass.SDK;

/// <summary>
/// Proxy to the ImageGlass host API. Methods send requests over the named pipe
/// and return deserialized responses. Used by plugins via <see cref="PluginBase.HostApi"/>.
/// </summary>
public interface IPluginHostProxy
{
    // Pixel operations

    /// <summary>
    /// Reads a single pixel color at source coordinates. Fast (pipe only).
    /// </summary>
    Task<PluginColor> ReadPixelAsync(int x, int y);

    /// <summary>
    /// Gets full pixel buffer via memory-mapped file. Caller must dispose the buffer.
    /// </summary>
    Task<PixelBuffer?> GetPixelBufferAsync(bool selectionOnly = false);

    /// <summary>
    /// Releases a previously acquired pixel buffer back to the host.
    /// </summary>
    Task ReleasePixelBufferAsync(PixelBuffer buffer);

    // Photo info

    /// <summary>
    /// Gets metadata of the current photo.
    /// </summary>
    Task<PluginPhotoMetadata?> GetPhotoMetadataAsync();

    /// <summary>
    /// Gets the list of all photos in the current collection with their index.
    /// </summary>
    Task<PluginPhotoList> GetPhotoListAsync();

    // Viewer

    /// <summary>
    /// Gets the source image dimensions.
    /// </summary>
    Task<(int Width, int Height)> GetSourceSizeAsync();

    /// <summary>
    /// Gets the current selection rectangle, or null if none.
    /// </summary>
    Task<PluginRect?> GetSelectionAsync();

    /// <summary>
    /// Sets the selection rectangle, or null to clear.
    /// </summary>
    Task SetSelectionAsync(PluginRect? rect);

    /// <summary>
    /// Enables or disables selection mode on the viewer.
    /// </summary>
    Task EnableSelectionAsync(bool enable);

    // App API

    /// <summary>
    /// Runs a named ImageGlass API method.
    /// </summary>
    Task RunApiAsync(string apiName, string? argument = null);

    // Theming

    /// <summary>
    /// Gets the current theme information.
    /// </summary>
    Task<ThemeInfo> GetThemeInfoAsync();

    // Event subscriptions

    /// <summary>
    /// Sets which real-time events (pointer, selection, frame) the plugin wants to receive.
    /// </summary>
    Task SubscribeEventsAsync(PluginEventSubscriptions subscriptions);
}
