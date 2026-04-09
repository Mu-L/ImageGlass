/*
ImageGlass - A lightweight, versatile image viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using System.Text.Json;

namespace ImageGlass.SDK;

/// <summary>
/// Implements <see cref="IPluginHostProxy"/> by forwarding each call
/// as a typed request/response pair over the named pipe.
/// </summary>
internal sealed class PluginHostProxy(PluginClient client) : IPluginHostProxy
{
    // Pixel operations

    public async Task<PluginColor> ReadPixelAsync(int x, int y)
    {
        var resp = await client.SendRequestAsync(MessageTypes.READ_PIXEL,
            new ReadPixelRequest { X = x, Y = y }).ConfigureAwait(false);
        var data = DeserializePayload<ReadPixelResponse>(resp);
        return data is null ? default : new PluginColor(data.R, data.G, data.B, data.A);
    }

    public async Task<PixelBuffer?> GetPixelBufferAsync(bool selectionOnly = false)
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_PIXEL_BUFFER,
            new GetPixelBufferRequest { SelectionOnly = selectionOnly }).ConfigureAwait(false);
        var data = DeserializePayload<GetPixelBufferResponse>(resp);
        if (data is null || string.IsNullOrEmpty(data.MmfPath)) return null;
        return new PixelBuffer(data.MmfPath, data.Width, data.Height, data.Stride, data.ColorType);
    }

    public async Task ReleasePixelBufferAsync(PixelBuffer buffer)
    {
        buffer.Dispose();
        await client.SendRequestAsync(MessageTypes.RELEASE_PIXEL_BUFFER,
            new ReleasePixelBufferRequest { MmfPath = buffer.FilePath }).ConfigureAwait(false);
    }

    // Photo info

    public async Task<PluginPhotoMetadata?> GetPhotoMetadataAsync()
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_PHOTO_METADATA).ConfigureAwait(false);
        return DeserializePayload<PluginPhotoMetadata>(resp);
    }

    public async Task<PluginPhotoList> GetPhotoListAsync()
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_PHOTO_LIST).ConfigureAwait(false);
        return DeserializePayload<PluginPhotoList>(resp) ?? new PluginPhotoList();
    }

    // Viewer

    public async Task<(int Width, int Height)> GetSourceSizeAsync()
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_SOURCE_SIZE).ConfigureAwait(false);
        var data = DeserializePayload<SourceSizeResponse>(resp);
        return data is null ? (0, 0) : (data.Width, data.Height);
    }

    public async Task<PluginRect?> GetSelectionAsync()
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_SELECTION).ConfigureAwait(false);
        var data = DeserializePayload<SetSelectionRequest>(resp);
        if (data?.X is null) return null;
        return new PluginRect(data.X.Value, data.Y ?? 0, data.Width ?? 0, data.Height ?? 0);
    }

    public async Task SetSelectionAsync(PluginRect? rect)
    {
        var payload = rect.HasValue
            ? new SetSelectionRequest { X = rect.Value.X, Y = rect.Value.Y, Width = rect.Value.Width, Height = rect.Value.Height }
            : new SetSelectionRequest();
        await client.SendRequestAsync(MessageTypes.SET_SELECTION, payload).ConfigureAwait(false);
    }

    public async Task EnableSelectionAsync(bool enable)
    {
        await client.SendRequestAsync(MessageTypes.ENABLE_SELECTION,
            new EnableSelectionRequest { Enable = enable }).ConfigureAwait(false);
    }

    // App API

    public async Task RunApiAsync(string apiName, string? argument = null)
    {
        await client.SendRequestAsync(MessageTypes.RUN_API,
            new RunApiRequest { ApiName = apiName, Argument = argument }).ConfigureAwait(false);
    }

    // Theming

    public async Task<ThemeInfo> GetThemeInfoAsync()
    {
        var resp = await client.SendRequestAsync(MessageTypes.GET_THEME_INFO).ConfigureAwait(false);
        return DeserializePayload<ThemeInfo>(resp) ?? new ThemeInfo();
    }

    // Event subscriptions

    public async Task SubscribeEventsAsync(PluginEventSubscriptions subscriptions)
    {
        await client.SendRequestAsync(MessageTypes.SUBSCRIBE_EVENTS, subscriptions).ConfigureAwait(false);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "Serializer uses source-generated PluginJsonContext options")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializer uses source-generated PluginJsonContext options")]
    private static T? DeserializePayload<T>(PluginMessage msg) where T : class
    {
        if (msg.Payload is null) return null;
        return msg.Payload.Value.Deserialize<T>(PluginJsonContext.Default.Options);
    }
}
