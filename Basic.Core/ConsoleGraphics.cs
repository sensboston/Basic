namespace Basic.Core;

/// <summary>
/// Console-based graphics implementation using Unicode block characters.
/// Simulates pixel graphics in text mode with 2x2 pixel blocks per character.
/// </summary>
public sealed class ConsoleGraphics : IGraphics
{
    private int[,] buffer;
    private int screenMode;
    private int foreground = BasicColors.White;
    private int background = BasicColors.Black;
    private int cursorRow = 1;
    private int cursorCol = 1;
    private readonly IConsole console;

    // Screen dimensions per mode
    private static readonly (int width, int height)[] ModeDimensions =
    [
        (80, 25),     // Mode 0: Text mode
        (320, 200),   // Mode 1: 320x200 4-color
        (640, 200),   // Mode 2: 640x200 2-color
        (320, 200),   // Mode 7: EGA 320x200
        (640, 200),   // Mode 8: EGA 640x200
        (640, 350),   // Mode 9: EGA 640x350
    ];

    public ConsoleGraphics(IConsole console)
    {
        this.console = console;
        buffer = new int[320, 200];
        screenMode = 1;
    }

    public int Width => screenMode switch
    {
        0 => 80,
        1 => 320,
        2 => 640,
        7 => 320,
        8 => 640,
        9 => 640,
        _ => 320
    };

    public int Height => screenMode switch
    {
        0 => 25,
        1 => 200,
        2 => 200,
        7 => 200,
        8 => 200,
        9 => 350,
        _ => 200
    };

    public int ForegroundColor => foreground;
    public int BackgroundColor => background;

    public int ActivePage { get; private set; }
    public int VisualPage { get; private set; }

    public void SetScreenMode(int mode)
    {
        SetScreenMode(mode, 0, 0);
    }

    public void SetScreenMode(int mode, int activePage, int visualPage)
    {
        screenMode = mode;
        buffer = new int[Width, Height];
        ActivePage = Math.Clamp(activePage, 0, 1);
        VisualPage = Math.Clamp(visualPage, 0, 1);
        ClearScreen();
    }

    public void SetPages(int activePage, int visualPage)
    {
        ActivePage = Math.Clamp(activePage, 0, 1);
        VisualPage = Math.Clamp(visualPage, 0, 1);
    }

    public void SetPixel(int x, int y, int color)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            buffer[x, y] = color;
        }
    }

    public int GetPixel(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return buffer[x, y];
        }
        return 0;
    }

    public void DrawLine(int x1, int y1, int x2, int y2, int color)
    {
        // Bresenham's line algorithm
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixel(x1, y1, color);

            if (x1 == x2 && y1 == y2) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    public void DrawBox(int x1, int y1, int x2, int y2, int color, bool filled)
    {
        // Ensure correct order
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        if (filled)
        {
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    SetPixel(x, y, color);
                }
            }
        }
        else
        {
            // Top and bottom
            for (int x = x1; x <= x2; x++)
            {
                SetPixel(x, y1, color);
                SetPixel(x, y2, color);
            }
            // Left and right
            for (int y = y1; y <= y2; y++)
            {
                SetPixel(x1, y, color);
                SetPixel(x2, y, color);
            }
        }
    }

    public void DrawCircle(int cx, int cy, int radius, int color, double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0)
    {
        // Midpoint circle algorithm with aspect ratio
        if (Math.Abs(startAngle) < 0.001 && Math.Abs(endAngle - Math.PI * 2) < 0.001)
        {
            // Full circle - use midpoint algorithm
            int x = radius;
            int y = 0;
            int err = 0;

            while (x >= y)
            {
                PlotCirclePoints(cx, cy, x, y, color, aspect);
                y++;
                err += 1 + 2 * y;
                if (2 * (err - x) + 1 > 0)
                {
                    x--;
                    err += 1 - 2 * x;
                }
            }
        }
        else
        {
            // Arc - use parametric approach
            double step = 1.0 / radius;
            for (double angle = startAngle; angle <= endAngle; angle += step)
            {
                int px = (int)(cx + radius * Math.Cos(angle));
                int py = (int)(cy + radius * Math.Sin(angle) * aspect);
                SetPixel(px, py, color);
            }
        }
    }

    private void PlotCirclePoints(int cx, int cy, int x, int y, int color, double aspect)
    {
        SetPixel(cx + x, (int)(cy + y * aspect), color);
        SetPixel(cx - x, (int)(cy + y * aspect), color);
        SetPixel(cx + x, (int)(cy - y * aspect), color);
        SetPixel(cx - x, (int)(cy - y * aspect), color);
        SetPixel(cx + y, (int)(cy + x * aspect), color);
        SetPixel(cx - y, (int)(cy + x * aspect), color);
        SetPixel(cx + y, (int)(cy - x * aspect), color);
        SetPixel(cx - y, (int)(cy - x * aspect), color);
    }

    public void Paint(int x, int y, int fillColor, int borderColor)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;

        int targetColor = GetPixel(x, y);
        if (targetColor == borderColor || targetColor == fillColor) return;

        var stack = new Stack<(int x, int y)>();
        stack.Push((x, y));

        while (stack.Count > 0)
        {
            var (px, py) = stack.Pop();
            if (px < 0 || px >= Width || py < 0 || py >= Height) continue;

            int currentColor = GetPixel(px, py);
            if (currentColor == borderColor || currentColor == fillColor) continue;

            SetPixel(px, py, fillColor);

            stack.Push((px + 1, py));
            stack.Push((px - 1, py));
            stack.Push((px, py + 1));
            stack.Push((px, py - 1));
        }
    }

    public void SetColor(int foreground, int background)
    {
        this.foreground = foreground;
        this.background = background;
    }

    public void Locate(int row, int col)
    {
        cursorRow = row;
        cursorCol = col;
    }

    public void ClearScreen()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                buffer[x, y] = background;
            }
        }
        console.WriteLine("\x1B[2J\x1B[H");
    }

    public void Render()
    {
        // Convert to ASCII art representation
        // Use 2 pixels per character width, 4 pixels per character height
        // Using Unicode block characters for better resolution

        var sb = new System.Text.StringBuilder();
        sb.Append("\x1B[H"); // Move cursor to home

        int charWidth = 2;
        int charHeight = 4;

        for (int charY = 0; charY < Height / charHeight; charY++)
        {
            for (int charX = 0; charX < Width / charWidth; charX++)
            {
                int x = charX * charWidth;
                int y = charY * charHeight;

                // Sample pixels in this block
                bool hasPixel = false;
                int pixelColor = background;

                for (int dy = 0; dy < charHeight && y + dy < Height; dy++)
                {
                    for (int dx = 0; dx < charWidth && x + dx < Width; dx++)
                    {
                        if (buffer[x + dx, y + dy] != background)
                        {
                            hasPixel = true;
                            pixelColor = buffer[x + dx, y + dy];
                            break;
                        }
                    }
                    if (hasPixel) break;
                }

                if (hasPixel)
                {
                    // Output colored block
                    sb.Append(GetAnsiColor(pixelColor));
                    sb.Append('\u2588'); // Full block
                    sb.Append("\x1B[0m");
                }
                else
                {
                    sb.Append(' ');
                }
            }
            sb.AppendLine();
        }

        console.Write(sb.ToString());
    }

    private static string GetAnsiColor(int color)
    {
        // Map GW-BASIC color to ANSI escape codes
        return color switch
        {
            0 => "\x1B[30m",    // Black
            1 => "\x1B[34m",    // Blue
            2 => "\x1B[32m",    // Green
            3 => "\x1B[36m",    // Cyan
            4 => "\x1B[31m",    // Red
            5 => "\x1B[35m",    // Magenta
            6 => "\x1B[33m",    // Brown/Yellow (dark)
            7 => "\x1B[37m",    // Light Gray
            8 => "\x1B[90m",    // Dark Gray
            9 => "\x1B[94m",    // Light Blue
            10 => "\x1B[92m",   // Light Green
            11 => "\x1B[96m",   // Light Cyan
            12 => "\x1B[91m",   // Light Red
            13 => "\x1B[95m",   // Light Magenta
            14 => "\x1B[93m",   // Yellow
            15 => "\x1B[97m",   // White
            _ => "\x1B[37m"
        };
    }

    public void Beep()
    {
        console.Write("\a"); // ASCII bell
    }

    public void SetWidth(int width)
    {
        // WIDTH primarily affects text mode output wrapping
        // For console graphics, we don't have direct control
    }

    public void Sound(int frequency, int duration)
    {
        // Sound is not supported in console mode, just beep
        console.Write("\a");
    }

    public void Play(string commands)
    {
        // PLAY music is not supported in console mode
    }

    public void PrintText(string text)
    {
        // In console graphics mode, we just use console output
        console.Write(text);
    }

    public bool IsGraphicsMode => screenMode > 0;

    public string ReadKey()
    {
        // In console graphics mode, read from console
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.KeyChar != '\0')
                return key.KeyChar.ToString();
        }
        return "";
    }

    public bool KeyAvailable => Console.KeyAvailable;
}

/// <summary>
/// Null graphics implementation for when graphics are not needed.
/// </summary>
public sealed class NullGraphics : IGraphics
{
    public int Width => 320;
    public int Height => 200;
    public int ForegroundColor => 15;
    public int BackgroundColor => 0;
    public int ActivePage => 0;
    public int VisualPage => 0;

    public void SetScreenMode(int mode) { }
    public void SetScreenMode(int mode, int activePage, int visualPage) { }
    public void SetPages(int activePage, int visualPage) { }
    public void SetPixel(int x, int y, int color) { }
    public int GetPixel(int x, int y) => 0;
    public void DrawLine(int x1, int y1, int x2, int y2, int color) { }
    public void DrawBox(int x1, int y1, int x2, int y2, int color, bool filled) { }
    public void DrawCircle(int cx, int cy, int radius, int color, double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0) { }
    public void Paint(int x, int y, int fillColor, int borderColor) { }
    public void SetColor(int foreground, int background) { }
    public void Locate(int row, int col) { }
    public void ClearScreen() { }
    public void Render() { }
    public void Beep() { }
    public void SetWidth(int width) { }
    public void Sound(int frequency, int duration) { }
    public void Play(string commands) { }
    public void PrintText(string text) { }
    public bool IsGraphicsMode => false;
    public string ReadKey() => "";
    public bool KeyAvailable => false;
}
