namespace Basic.Core;

/// <summary>
/// Low-level display output abstraction.
/// Implementations handle platform-specific rendering of the framebuffer.
/// </summary>
public interface IDisplayOutput : IDisposable
{
    /// <summary>
    /// Initialize the display with specified dimensions.
    /// </summary>
    void Initialize(int width, int height);

    /// <summary>
    /// Present the framebuffer to the display.
    /// Pixels are in BGRA format (32 bits per pixel).
    /// </summary>
    void Present(ReadOnlySpan<byte> bgraPixels, int width, int height);

    /// <summary>
    /// Check if the display is still valid (window not closed, etc.).
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Process pending window messages/events.
    /// Returns false if the display should close.
    /// </summary>
    bool ProcessEvents();

    /// <summary>
    /// Read a key from the input queue (non-blocking).
    /// Returns empty string if no key is available.
    /// </summary>
    string ReadKey();

    /// <summary>
    /// Check if a key is available in the input queue.
    /// </summary>
    bool KeyAvailable { get; }
}

/// <summary>
/// Display mode for Windows platform.
/// </summary>
public enum WindowsDisplayMode
{
    /// <summary>
    /// Draw graphics as overlay on console window (GDI).
    /// </summary>
    ConsoleOverlay,

    /// <summary>
    /// Create a separate graphics window (GDI).
    /// </summary>
    SeparateWindow
}

/// <summary>
/// Null display output for headless/testing scenarios.
/// </summary>
public sealed class NullDisplayOutput : IDisplayOutput
{
    public bool IsValid => true;
    public bool KeyAvailable => false;

    public void Initialize(int width, int height) { }

    public void Present(ReadOnlySpan<byte> bgraPixels, int width, int height) { }

    public bool ProcessEvents() => true;

    public string ReadKey() => "";

    public void Dispose() { }
}
