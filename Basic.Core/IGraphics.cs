namespace Basic.Core;

/// <summary>
/// Graphics interface for GW-BASIC graphics operations.
/// Implementations can render to console, web canvas, or other targets.
/// </summary>
public interface IGraphics
{
    /// <summary>
    /// Set screen mode. Mode 0 = text, 1 = 320x200 4-color, 2 = 640x200 2-color, etc.
    /// </summary>
    void SetScreenMode(int mode);

    /// <summary>
    /// Set screen mode with page parameters for double buffering.
    /// </summary>
    /// <param name="mode">Screen mode</param>
    /// <param name="activePage">Page to draw on (0 or 1)</param>
    /// <param name="visualPage">Page to display (0 or 1)</param>
    void SetScreenMode(int mode, int activePage, int visualPage);

    /// <summary>
    /// Set active and visual pages without changing screen mode.
    /// Used for page flipping / double buffering.
    /// </summary>
    void SetPages(int activePage, int visualPage);

    /// <summary>
    /// Get the current active (drawing) page.
    /// </summary>
    int ActivePage { get; }

    /// <summary>
    /// Get the current visual (displayed) page.
    /// </summary>
    int VisualPage { get; }

    /// <summary>
    /// Get current screen width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Get current screen height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Set a pixel at the specified coordinates.
    /// </summary>
    void SetPixel(int x, int y, int color);

    /// <summary>
    /// Get the color of a pixel at the specified coordinates.
    /// </summary>
    int GetPixel(int x, int y);

    /// <summary>
    /// Draw a line from (x1,y1) to (x2,y2).
    /// </summary>
    void DrawLine(int x1, int y1, int x2, int y2, int color);

    /// <summary>
    /// Draw a box (outline or filled).
    /// </summary>
    void DrawBox(int x1, int y1, int x2, int y2, int color, bool filled);

    /// <summary>
    /// Draw a circle or ellipse.
    /// </summary>
    void DrawCircle(int cx, int cy, int radius, int color, double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0);

    /// <summary>
    /// Flood fill starting at (x,y) with the specified color.
    /// </summary>
    void Paint(int x, int y, int fillColor, int borderColor);

    /// <summary>
    /// Set foreground and background colors.
    /// </summary>
    void SetColor(int foreground, int background);

    /// <summary>
    /// Get current foreground color.
    /// </summary>
    int ForegroundColor { get; }

    /// <summary>
    /// Get current background color.
    /// </summary>
    int BackgroundColor { get; }

    /// <summary>
    /// Move text cursor to specified position (1-based).
    /// </summary>
    void Locate(int row, int col);

    /// <summary>
    /// Clear the screen.
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Reset graphics state for new program execution.
    /// Clears all buffers and resets to default state.
    /// </summary>
    void Reset() { }

    /// <summary>
    /// Render the current frame buffer to the output.
    /// </summary>
    void Render();

    /// <summary>
    /// Play a beep sound.
    /// </summary>
    void Beep();

    /// <summary>
    /// Set the screen/output width in characters.
    /// </summary>
    void SetWidth(int width);

    /// <summary>
    /// Generate a sound at specified frequency (Hz) for specified duration (clock ticks, 18.2/sec).
    /// </summary>
    void Sound(int frequency, int duration);

    /// <summary>
    /// Play music commands using GW-BASIC PLAY macro language.
    /// </summary>
    void Play(string commands);

    /// <summary>
    /// Print text at current cursor position in graphics mode.
    /// </summary>
    void PrintText(string text);

    /// <summary>
    /// Returns true if currently in graphics mode (SCREEN > 0).
    /// </summary>
    bool IsGraphicsMode { get; }

    /// <summary>
    /// Read a key from the graphics window input queue (non-blocking).
    /// Returns empty string if no key is available.
    /// </summary>
    string ReadKey();

    /// <summary>
    /// Check if a key is available in the graphics window input queue.
    /// </summary>
    bool KeyAvailable { get; }
}

/// <summary>
/// GW-BASIC color palette (CGA colors).
/// </summary>
public static class BasicColors
{
    public const int Black = 0;
    public const int Blue = 1;
    public const int Green = 2;
    public const int Cyan = 3;
    public const int Red = 4;
    public const int Magenta = 5;
    public const int Brown = 6;
    public const int LightGray = 7;
    public const int DarkGray = 8;
    public const int LightBlue = 9;
    public const int LightGreen = 10;
    public const int LightCyan = 11;
    public const int LightRed = 12;
    public const int LightMagenta = 13;
    public const int Yellow = 14;
    public const int White = 15;
}
