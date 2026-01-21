using System.Collections.Concurrent;
using Basic.Core;
using static Basic.Windows.NativeMethods;

namespace Basic.Windows;

/// <summary>
/// Fast console output using WriteConsoleOutput (Far Manager style).
/// Much faster than ANSI escape codes for graphics rendering.
/// </summary>
public sealed class FastConsoleOutput : IDisplayOutput
{
    private readonly IntPtr hConsole;
    private CHAR_INFO[] buffer = [];
    private int frameWidth;
    private int frameHeight;
    private int consoleWidth;
    private int consoleHeight;
    private bool disposed;
    private readonly ConcurrentQueue<string> keyQueue = new();

    // Character used for pixels (full block)
    private const char PixelChar = '█';

    // Map GW-BASIC colors to console attributes
    private static readonly ushort[] ColorMap =
    [
        0,                                                          // 0 - Black
        FOREGROUND_BLUE,                                           // 1 - Blue
        FOREGROUND_GREEN,                                          // 2 - Green
        FOREGROUND_GREEN | FOREGROUND_BLUE,                        // 3 - Cyan
        FOREGROUND_RED,                                            // 4 - Red
        FOREGROUND_RED | FOREGROUND_BLUE,                          // 5 - Magenta
        FOREGROUND_RED | FOREGROUND_GREEN,                         // 6 - Brown/Yellow (dark)
        FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,       // 7 - Light Gray
        FOREGROUND_INTENSITY,                                      // 8 - Dark Gray
        FOREGROUND_BLUE | FOREGROUND_INTENSITY,                    // 9 - Light Blue
        FOREGROUND_GREEN | FOREGROUND_INTENSITY,                   // 10 - Light Green
        FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY, // 11 - Light Cyan
        FOREGROUND_RED | FOREGROUND_INTENSITY,                     // 12 - Light Red
        FOREGROUND_RED | FOREGROUND_BLUE | FOREGROUND_INTENSITY,   // 13 - Light Magenta
        FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY,  // 14 - Yellow
        FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY, // 15 - White
    ];

    private bool hasValidConsole;

    public bool IsValid => hConsole != IntPtr.Zero && !disposed;
    public bool KeyAvailable
    {
        get
        {
            if (!keyQueue.IsEmpty) return true;
            if (!hasValidConsole) return false;
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                hasValidConsole = false;
                return false;
            }
        }
    }

    public FastConsoleOutput()
    {
        hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        // Check if we have a valid console for keyboard input
        try
        {
            _ = Console.KeyAvailable;
            hasValidConsole = true;
        }
        catch
        {
            hasValidConsole = false;
        }
    }

    public void Initialize(int width, int height)
    {
        frameWidth = width;
        frameHeight = height;

        // Get console dimensions
        if (GetConsoleScreenBufferInfo(hConsole, out var info))
        {
            consoleWidth = info.srWindow.Right - info.srWindow.Left + 1;
            consoleHeight = info.srWindow.Bottom - info.srWindow.Top + 1;
        }
        else
        {
            // Fallback when console is not available (e.g., piped input)
            try
            {
                consoleWidth = Console.WindowWidth;
                consoleHeight = Console.WindowHeight;
            }
            catch
            {
                consoleWidth = 80;
                consoleHeight = 25;
            }
        }

        // Allocate buffer for console output
        // Each graphics pixel maps to a console character
        // We'll use 2x4 pixel blocks per character for better aspect ratio
        int charWidth = (frameWidth + 1) / 2;
        int charHeight = (frameHeight + 3) / 4;

        // Limit to console size
        charWidth = Math.Min(charWidth, consoleWidth);
        charHeight = Math.Min(charHeight, consoleHeight - 1);

        buffer = new CHAR_INFO[charWidth * charHeight];
    }

    public void Present(ReadOnlySpan<byte> bgraPixels, int width, int height)
    {
        if (disposed || buffer.Length == 0)
            return;

        if (width != frameWidth || height != frameHeight)
        {
            Initialize(width, height);
        }

        int charWidth = (frameWidth + 1) / 2;
        int charHeight = (frameHeight + 3) / 4;
        charWidth = Math.Min(charWidth, consoleWidth);
        charHeight = Math.Min(charHeight, consoleHeight - 1);

        // Convert BGRA pixels to console characters
        int pixelWidth = 2;
        int pixelHeight = 4;

        for (int cy = 0; cy < charHeight; cy++)
        {
            for (int cx = 0; cx < charWidth; cx++)
            {
                int px = cx * pixelWidth;
                int py = cy * pixelHeight;

                // Sample the pixel block - find dominant color
                int bestColor = 0;
                bool hasPixel = false;

                for (int dy = 0; dy < pixelHeight && py + dy < height; dy++)
                {
                    for (int dx = 0; dx < pixelWidth && px + dx < width; dx++)
                    {
                        int pixelIndex = ((py + dy) * width + (px + dx)) * 4;
                        if (pixelIndex + 2 < bgraPixels.Length)
                        {
                            byte b = bgraPixels[pixelIndex];
                            byte g = bgraPixels[pixelIndex + 1];
                            byte r = bgraPixels[pixelIndex + 2];

                            // Check if not black (background)
                            if (r > 10 || g > 10 || b > 10)
                            {
                                hasPixel = true;
                                bestColor = RgbToConsoleColor(r, g, b);
                                break;
                            }
                        }
                    }
                    if (hasPixel) break;
                }

                int bufferIndex = cy * charWidth + cx;
                if (bufferIndex < buffer.Length)
                {
                    buffer[bufferIndex].UnicodeChar = hasPixel ? PixelChar : ' ';
                    buffer[bufferIndex].Attributes = hasPixel ? ColorMap[bestColor & 0xF] : (ushort)0;
                }
            }
        }

        // Write to console in one call
        var bufferSize = new COORD((short)charWidth, (short)charHeight);
        var bufferCoord = new COORD(0, 0);
        var writeRegion = new SMALL_RECT
        {
            Left = 0,
            Top = 0,
            Right = (short)(charWidth - 1),
            Bottom = (short)(charHeight - 1)
        };

        WriteConsoleOutputW(hConsole, buffer, bufferSize, bufferCoord, ref writeRegion);
    }

    private static int RgbToConsoleColor(byte r, byte g, byte b)
    {
        // Convert RGB to closest GW-BASIC color (0-15)
        int color = 0;

        // Determine intensity threshold
        int maxVal = Math.Max(r, Math.Max(g, b));
        bool bright = maxVal > 128;

        // Build color from RGB components
        if (r > 64) color |= 4;  // Red
        if (g > 64) color |= 2;  // Green
        if (b > 64) color |= 1;  // Blue
        if (bright && color > 0) color |= 8;  // Intensity

        return color;
    }

    public bool ProcessEvents()
    {
        return !disposed;
    }

    public string ReadKey()
    {
        if (keyQueue.TryDequeue(out string? key))
            return key;

        if (!hasValidConsole)
            return "";

        try
        {
            if (Console.KeyAvailable)
            {
                var consoleKey = Console.ReadKey(true);
                if (consoleKey.Key == ConsoleKey.Enter)
                    return "\r";
                if (consoleKey.Key == ConsoleKey.Backspace)
                    return "\b";
                if (consoleKey.Key == ConsoleKey.Escape)
                    return "\x1B";
                if (consoleKey.Key == ConsoleKey.UpArrow)
                    return "\0H";
                if (consoleKey.Key == ConsoleKey.DownArrow)
                    return "\0P";
                if (consoleKey.Key == ConsoleKey.LeftArrow)
                    return "\0K";
                if (consoleKey.Key == ConsoleKey.RightArrow)
                    return "\0M";
                if (consoleKey.KeyChar != '\0')
                    return consoleKey.KeyChar.ToString();
            }
        }
        catch
        {
            hasValidConsole = false;
        }
        return "";
    }

    public void Dispose()
    {
        disposed = true;
    }
}
