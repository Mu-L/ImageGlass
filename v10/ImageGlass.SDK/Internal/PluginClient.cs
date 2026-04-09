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
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;

namespace ImageGlass.SDK;

/// <summary>
/// Manages the named-pipe connection to the ImageGlass host.
/// Sends requests, receives events, and routes messages to plugin callbacks.
/// </summary>
internal sealed class PluginClient : IDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly Lock _writeLock = new();
    private int _nextRequestId;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<PluginMessage>> _pending = new();
    private readonly PluginBase _plugin;
    private readonly CancellationTokenSource _cts = new();

    public IPluginHostProxy HostApi { get; }


    internal PluginClient(string pipeName, PluginBase plugin)
    {
        _plugin = plugin;
        _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        HostApi = new PluginHostProxy(this);
    }



    /// <summary>
    /// Connect to host pipe and run the message loop until shutdown.
    /// </summary>
    public async Task ConnectAndRunAsync()
    {
        await _pipe.ConnectAsync(5000, _cts.Token).ConfigureAwait(false);
        _reader = new StreamReader(_pipe, leaveOpen: true);
        _writer = new StreamWriter(_pipe, leaveOpen: true) { AutoFlush = true };

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(_cts.Token).ConfigureAwait(false);
                if (line is null) break; // pipe closed

                PluginMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize(line, PluginJsonContext.Default.PluginMessage);
                }
                catch
                {
                    continue; // skip malformed messages
                }
                if (msg is null) continue;

                if (msg.RequestId.HasValue && _pending.TryRemove(msg.RequestId.Value, out var tcs))
                {
                    tcs.SetResult(msg);
                }
                else
                {
                    // Fire-and-forget: don't block the read loop so that
                    // event handlers (e.g. OnExecuteAsync) can call HostApi
                    // methods and receive responses without deadlocking.
                    _ = DispatchEventAsync(msg);
                }
            }
        }
        catch (OperationCanceledException) { }
    }


    /// <summary>
    /// Send a request and wait for the correlated response.
    /// </summary>
    internal async Task<PluginMessage> SendRequestAsync(string type, object? payload = null)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);
        var tcs = new TaskCompletionSource<PluginMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var jsonPayload = payload is null
            ? null
            : (JsonElement?)JsonSerializer.SerializeToElement(payload, payload.GetType(), PluginJsonContext.Default);

        var msg = new PluginMessage { Type = type, RequestId = requestId, Payload = jsonPayload };
        var json = JsonSerializer.Serialize(msg, PluginJsonContext.Default.PluginMessage);

        lock (_writeLock)
        {
            _writer!.WriteLine(json);
        }

        return await tcs.Task.ConfigureAwait(false);
    }


    /// <summary>
    /// Send a one-way event (no response expected).
    /// </summary>
    internal void SendEvent(string type, object? payload = null)
    {
        var jsonPayload = payload is null
            ? null
            : (JsonElement?)JsonSerializer.SerializeToElement(payload, payload.GetType(), PluginJsonContext.Default);

        var msg = new PluginMessage { Type = type, Payload = jsonPayload };
        var json = JsonSerializer.Serialize(msg, PluginJsonContext.Default.PluginMessage);

        lock (_writeLock)
        {
            _writer!.WriteLine(json);
        }
    }


    private async Task DispatchEventAsync(PluginMessage msg)
    {
        try
        {
            switch (msg.Type)
            {
                case MessageTypes.INIT:
                    var init = Deserialize<PluginInitPayload>(msg.Payload);
                    if (init is not null)
                    {
                        _plugin.DataDirectory = init.DataDirectory;
                        _plugin.CurrentTheme = init.ThemeInfo;
                    }
                    await _plugin.OnInitializedAsync().ConfigureAwait(false);
                    break;

                case MessageTypes.PHOTO_CHANGED:
                    var photo = Deserialize<PhotoChangedEventArgs>(msg.Payload);
                    _plugin.OnPhotoChanged(photo ?? new PhotoChangedEventArgs());
                    break;

                case MessageTypes.THEME_CHANGED:
                    var theme = Deserialize<ThemeInfo>(msg.Payload);
                    if (theme is not null) _plugin.CurrentTheme = theme;
                    _plugin.OnThemeChanged(theme ?? new ThemeInfo());
                    break;

                case MessageTypes.LANGUAGE_CHANGED:
                    var lang = Deserialize<LanguageChangedEventArgs>(msg.Payload);
                    _plugin.OnLanguageChanged(lang ?? new LanguageChangedEventArgs());
                    break;

                case MessageTypes.POINTER_MOVED:
                    var ptrMoved = Deserialize<PointerEventArgs>(msg.Payload);
                    if (ptrMoved is not null) _plugin.OnPointerMoved(ptrMoved);
                    break;

                case MessageTypes.POINTER_PRESSED:
                    var ptrPressed = Deserialize<PointerEventArgs>(msg.Payload);
                    if (ptrPressed is not null) _plugin.OnPointerPressed(ptrPressed);
                    break;

                case MessageTypes.SELECTION_CHANGED:
                    var sel = Deserialize<SelectionEventArgs>(msg.Payload);
                    _plugin.OnSelectionChanged(sel);
                    break;

                case MessageTypes.FRAME_CHANGED:
                    var frame = Deserialize<FrameChangedPayload>(msg.Payload);
                    _plugin.OnFrameChanged(frame?.FrameIndex ?? 0);
                    break;

                case MessageTypes.EXECUTE:
                    await _plugin.OnExecuteAsync(_cts.Token).ConfigureAwait(false);
                    break;

                case MessageTypes.SHUTDOWN:
                    _cts.Cancel();
                    break;
            }
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"[{_plugin.PluginId}] {msg.Type} failed: {ex}");
            }
            catch { }
        }
    }


    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "Serializer uses source-generated PluginJsonContext options")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializer uses source-generated PluginJsonContext options")]
    private static T? Deserialize<T>(JsonElement? element) where T : class
    {
        if (element is null) return null;
        return element.Value.Deserialize<T>(PluginJsonContext.Default.Options);
    }


    public void Dispose()
    {
        _cts.Cancel();
        foreach (var kvp in _pending)
        {
            kvp.Value.TrySetCanceled();
        }
        _pending.Clear();
        try { _writer?.Dispose(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _pipe.Dispose(); } catch { }
        _cts.Dispose();
    }

}
