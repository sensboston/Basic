using Basic.Core;
using Microsoft.JSInterop;

namespace Basic.Web;

/// <summary>
/// Display output implementation for Blazor WebAssembly.
/// Renders FrameBuffer pixels to HTML5 canvas via JavaScript interop.
/// </summary>
public sealed class BlazorDisplayOutput : IDisplayOutput
{
    private readonly IJSRuntime jsRuntime;
    private readonly string canvasId;
    private int width;
    private int height;
    private bool disposed;
    private readonly List<string> keyQueue = new();
    private readonly object keyLock = new();
    private Action<bool>? graphicsModeCallback;

    public bool IsValid => !disposed;

    public bool KeyAvailable
    {
        get
        {
            lock (keyLock)
            {
                return keyQueue.Count > 0;
            }
        }
    }

    public BlazorDisplayOutput(IJSRuntime jsRuntime, string canvasId = "graphicsCanvas")
    {
        this.jsRuntime = jsRuntime;
        this.canvasId = canvasId;
    }

    /// <summary>
    /// Set callback to be called when graphics mode is activated/deactivated.
    /// </summary>
    public void SetGraphicsModeCallback(Action<bool> callback)
    {
        graphicsModeCallback = callback;
    }

    public void Initialize(int width, int height)
    {
        this.width = width;
        this.height = height;

        // Notify that graphics mode is now active
        graphicsModeCallback?.Invoke(true);

        // Initialize canvas via JS interop (fire and forget for sync interface)
        _ = InitializeAsync(width, height);
    }

    private async Task InitializeAsync(int width, int height)
    {
        try
        {
            // Small delay to allow UI to switch to canvas
            await Task.Delay(50);
            await jsRuntime.InvokeVoidAsync("basicCanvas.initialize", canvasId, width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas initialization error: {ex.Message}");
        }
    }

    public void Present(ReadOnlySpan<byte> bgraPixels, int width, int height)
    {
        if (disposed) return;

        // Convert pixel data to base64 for JS interop
        var base64 = Convert.ToBase64String(bgraPixels.ToArray());

        // Fire and forget - async render
        _ = PresentAsync(base64, width, height);
    }

    private async Task PresentAsync(string pixelsBase64, int width, int height)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("basicCanvas.renderPixels", pixelsBase64, width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas render error: {ex.Message}");
        }
    }

    public bool ProcessEvents()
    {
        // In browser, events are handled by JavaScript
        // Poll for keys from JS
        _ = PollKeysAsync();
        return !disposed;
    }

    private async Task PollKeysAsync()
    {
        try
        {
            var key = await jsRuntime.InvokeAsync<string>("basicCanvas.readKey");
            if (!string.IsNullOrEmpty(key))
            {
                lock (keyLock)
                {
                    keyQueue.Add(key);
                }
            }
        }
        catch
        {
            // Ignore JS interop errors during polling
        }
    }

    public string ReadKey()
    {
        lock (keyLock)
        {
            if (keyQueue.Count > 0)
            {
                var key = keyQueue[0];
                keyQueue.RemoveAt(0);
                return key;
            }
        }
        return "";
    }

    /// <summary>
    /// Queue a key press from JavaScript callback.
    /// </summary>
    public void QueueKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            lock (keyLock)
            {
                keyQueue.Add(key);
            }
        }
    }

    /// <summary>
    /// Clear the key queue.
    /// </summary>
    public void ClearKeys()
    {
        lock (keyLock)
        {
            keyQueue.Clear();
        }
    }

    /// <summary>
    /// Play a beep sound via JavaScript.
    /// </summary>
    public async Task BeepAsync(int frequency = 800, int durationMs = 200)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("basicCanvas.beep", frequency, durationMs);
        }
        catch
        {
            // Ignore audio errors
        }
    }

    public void Dispose()
    {
        disposed = true;
    }
}
