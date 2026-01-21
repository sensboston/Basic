namespace Basic.Core;

/// <summary>
/// Software-based IGraphics implementation using FrameBuffer.
/// All rendering happens in memory; output is handled by IDisplayOutput.
/// Supports double buffering with two pages for smooth animation.
/// </summary>
public sealed class SoftwareRenderer : IGraphics
{
    private FrameBuffer[] pages;
    private int activePage;
    private int visualPage;
    private readonly IDisplayOutput output;
    private int screenMode;
    private int textRow = 1;
    private int textCol = 1;

    // Screen mode configurations (width, height, colors)
    // Colors: 16 = 16-color palette, 256 = 256-color palette, -1 = 24-bit RGB
    private static readonly Dictionary<int, (int w, int h, int colors)> ScreenModes = new()
    {
        [0] = (640, 200, 16),     // Text mode (simulated as graphics)
        [1] = (320, 200, 4),      // CGA 4-color
        [2] = (640, 200, 2),      // CGA 2-color
        [7] = (320, 200, 16),     // EGA 16-color
        [8] = (640, 200, 16),     // EGA 16-color
        [9] = (640, 350, 16),     // EGA 16-color
        [12] = (640, 480, 16),    // VGA 16-color
        [13] = (320, 200, 256),   // VGA 256-color (MCGA)
        // Extended modes (beyond original GW-BASIC)
        [14] = (640, 480, 256),   // SVGA 256-color
        [15] = (640, 480, -1),    // SVGA 24-bit RGB (16M colors)
        [16] = (800, 600, 256),   // SVGA 256-color
        [17] = (800, 600, -1),    // SVGA 24-bit RGB (16M colors)
        [18] = (1024, 768, 256),  // XGA 256-color
        [19] = (1024, 768, -1),   // XGA 24-bit RGB (16M colors)
    };

    /// <summary>
    /// Active framebuffer (page being drawn to).
    /// </summary>
    private FrameBuffer frameBuffer => pages[activePage];

    public int Width => frameBuffer.Width;
    public int Height => frameBuffer.Height;
    public int ForegroundColor => frameBuffer.ForegroundColor;
    public int BackgroundColor => frameBuffer.BackgroundColor;
    public int ActivePage => activePage;
    public int VisualPage => visualPage;

    /// <summary>
    /// The underlying framebuffer (active page).
    /// </summary>
    public FrameBuffer FrameBuffer => frameBuffer;

    public SoftwareRenderer(IDisplayOutput output, int initialMode = 0)
    {
        this.output = output;
        pages = [new FrameBuffer(640, 200), new FrameBuffer(640, 200)];
        activePage = 0;
        visualPage = 0;
        // Don't initialize display output in constructor - wait for SCREEN command
        screenMode = initialMode;
    }

    public void SetScreenMode(int mode)
    {
        SetScreenMode(mode, 0, 0);
    }

    public void SetScreenMode(int mode, int apage, int vpage)
    {
        // If mode hasn't changed, just update pages (fast page flip)
        if (mode == screenMode && pages[0].Width > 0)
        {
            SetPages(apage, vpage);
            return;
        }

        screenMode = mode;

        if (ScreenModes.TryGetValue(mode, out var config))
        {
            pages = [new FrameBuffer(config.w, config.h), new FrameBuffer(config.w, config.h)];
            // Only initialize display for graphics modes (mode > 0)
            if (mode > 0)
            {
                output.Initialize(config.w, config.h);
            }
        }
        else
        {
            // Default to mode 0 (text mode) - no window initialization
            pages = [new FrameBuffer(640, 200), new FrameBuffer(640, 200)];
        }

        activePage = Math.Clamp(apage, 0, 1);
        visualPage = Math.Clamp(vpage, 0, 1);
        textRow = 1;
        textCol = 1;
    }

    /// <summary>
    /// Reset graphics state for new program execution.
    /// Clears all buffers and resets to default state.
    /// </summary>
    public void Reset()
    {
        screenMode = 0;
        pages[0].Clear(0);
        pages[1].Clear(0);
        pages[0].ForegroundColor = 15;
        pages[0].BackgroundColor = 0;
        pages[1].ForegroundColor = 15;
        pages[1].BackgroundColor = 0;
        activePage = 0;
        visualPage = 0;
        textRow = 1;
        textCol = 1;
    }

    public void SetPages(int apage, int vpage)
    {
        activePage = Math.Clamp(apage, 0, 1);
        visualPage = Math.Clamp(vpage, 0, 1);
    }

    public void SetPixel(int x, int y, int color)
    {
        frameBuffer.SetPixel(x, y, color);
    }

    public int GetPixel(int x, int y)
    {
        return frameBuffer.GetPixel(x, y);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, int color)
    {
        frameBuffer.DrawLine(x1, y1, x2, y2, color);
    }

    public void DrawBox(int x1, int y1, int x2, int y2, int color, bool filled)
    {
        frameBuffer.DrawBox(x1, y1, x2, y2, color, filled);
    }

    public void DrawCircle(int cx, int cy, int radius, int color,
        double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0)
    {
        frameBuffer.DrawCircle(cx, cy, radius, color, startAngle, endAngle, aspect);
    }

    public void Paint(int x, int y, int fillColor, int borderColor)
    {
        frameBuffer.FloodFill(x, y, fillColor, borderColor);
    }

    public void SetColor(int foreground, int background)
    {
        frameBuffer.ForegroundColor = foreground;
        frameBuffer.BackgroundColor = background;
    }

    public void Locate(int row, int col)
    {
        textRow = Math.Max(1, row);
        textCol = Math.Max(1, col);
    }

    public void ClearScreen()
    {
        frameBuffer.Clear(frameBuffer.BackgroundColor);
        textRow = 1;
        textCol = 1;
    }

    public void Render()
    {
        // Only render if in graphics mode
        if (screenMode > 0)
        {
            // Display the visual page (not necessarily the active page)
            var displayBuffer = pages[visualPage];
            output.Present(displayBuffer.Pixels, displayBuffer.Width, displayBuffer.Height);
        }
    }

    public void Beep()
    {
        // Platform-specific, delegate to output if supported
        Console.Beep();
    }

    public void SetWidth(int width)
    {
        // Text width - affects text mode layout
        // In graphics mode, this is typically ignored
    }

    public void Sound(int frequency, int duration)
    {
        // Platform-specific sound generation
        // Duration is in clock ticks (18.2 per second)
        int durationMs = (int)(duration * 1000.0 / 18.2);
        try
        {
            Console.Beep(frequency, durationMs);
        }
        catch
        {
            // Ignore if not supported
        }
    }

    public void Play(string commands)
    {
        // PLAY macro language - would need full parser
        // For now, just beep
        Beep();
    }

    /// <summary>
    /// Set palette color (for PALETTE statement).
    /// </summary>
    public void SetPalette(int index, byte r, byte g, byte b)
    {
        frameBuffer.SetPaletteColor(index, r, g, b);
    }

    /// <summary>
    /// Copy screen region (for GET graphics statement).
    /// </summary>
    public byte[] GetRegion(int x1, int y1, int x2, int y2)
    {
        return frameBuffer.CopyRegion(x1, y1, x2, y2);
    }

    /// <summary>
    /// Paste screen region (for PUT graphics statement).
    /// </summary>
    public void PutRegion(int x, int y, byte[] data, PutAction action = PutAction.Overwrite)
    {
        frameBuffer.PasteRegion(x, y, data, action);
    }

    /// <summary>
    /// Process display events. Returns false if should exit.
    /// </summary>
    public bool ProcessEvents()
    {
        return output.ProcessEvents();
    }

    public bool IsGraphicsMode => screenMode > 0;

    public string ReadKey() => output.ReadKey();

    public bool KeyAvailable => output.KeyAvailable;

    public void PrintText(string text)
    {
        // Render text at current cursor position
        // Character width is always 8, height depends on screen mode
        int charWidth = 8;
        int charHeight = screenMode == 9 ? 14 : (screenMode == 12 ? 16 : 8);
        int x = (textCol - 1) * charWidth;
        int y = (textRow - 1) * charHeight;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                textRow++;
                textCol = 1;
                x = 0;
                y = (textRow - 1) * charHeight;
                continue;
            }

            DrawChar(x, y, c, frameBuffer.ForegroundColor, charHeight);
            x += charWidth;
            textCol++;

            // Wrap at screen edge
            if (x + charWidth > frameBuffer.Width)
            {
                x = 0;
                textCol = 1;
                y += charHeight;
                textRow++;
            }
        }

        // Update cursor position
        Render();
    }

    private void DrawChar(int x, int y, char c, int color, int charHeight = 8)
    {
        // Simple 8x8 bitmap font - ASCII 32-127, scaled to charHeight
        byte[] charBitmap = GetCharBitmap(c);

        for (int row = 0; row < charHeight; row++)
        {
            // Scale 8 rows to charHeight rows
            int srcRow = row * 8 / charHeight;
            byte bits = charBitmap[srcRow];
            for (int col = 0; col < 8; col++)
            {
                if ((bits & (0x80 >> col)) != 0)
                {
                    frameBuffer.SetPixel(x + col, y + row, color);
                }
            }
        }
    }

    private static byte[] GetCharBitmap(char c)
    {
        // Basic 8x8 font for printable ASCII characters
        // Only implementing common characters for now
        return c switch
        {
            ' ' => [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            '!' => [0x18, 0x18, 0x18, 0x18, 0x18, 0x00, 0x18, 0x00],
            '"' => [0x6C, 0x6C, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00],
            '*' => [0x00, 0x66, 0x3C, 0xFF, 0x3C, 0x66, 0x00, 0x00],
            '+' => [0x00, 0x18, 0x18, 0x7E, 0x18, 0x18, 0x00, 0x00],
            '-' => [0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00, 0x00],
            '.' => [0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x00],
            ',' => [0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x30],
            ':' => [0x00, 0x18, 0x18, 0x00, 0x00, 0x18, 0x18, 0x00],
            '0' => [0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C, 0x00],
            '1' => [0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x7E, 0x00],
            '2' => [0x3C, 0x66, 0x06, 0x0C, 0x18, 0x30, 0x7E, 0x00],
            '3' => [0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C, 0x00],
            '4' => [0x0C, 0x1C, 0x3C, 0x6C, 0x7E, 0x0C, 0x0C, 0x00],
            '5' => [0x7E, 0x60, 0x7C, 0x06, 0x06, 0x66, 0x3C, 0x00],
            '6' => [0x1C, 0x30, 0x60, 0x7C, 0x66, 0x66, 0x3C, 0x00],
            '7' => [0x7E, 0x06, 0x0C, 0x18, 0x30, 0x30, 0x30, 0x00],
            '8' => [0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, 0x00],
            '9' => [0x3C, 0x66, 0x66, 0x3E, 0x06, 0x0C, 0x38, 0x00],
            'A' or 'a' => [0x18, 0x3C, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x00],
            'B' or 'b' => [0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C, 0x00],
            'C' or 'c' => [0x3C, 0x66, 0x60, 0x60, 0x60, 0x66, 0x3C, 0x00],
            'D' or 'd' => [0x78, 0x6C, 0x66, 0x66, 0x66, 0x6C, 0x78, 0x00],
            'E' or 'e' => [0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x7E, 0x00],
            'F' or 'f' => [0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x60, 0x00],
            'G' or 'g' => [0x3C, 0x66, 0x60, 0x6E, 0x66, 0x66, 0x3E, 0x00],
            'H' or 'h' => [0x66, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x66, 0x00],
            'I' or 'i' => [0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x7E, 0x00],
            'J' or 'j' => [0x3E, 0x0C, 0x0C, 0x0C, 0x0C, 0x6C, 0x38, 0x00],
            'K' or 'k' => [0x66, 0x6C, 0x78, 0x70, 0x78, 0x6C, 0x66, 0x00],
            'L' or 'l' => [0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, 0x00],
            'M' or 'm' => [0x63, 0x77, 0x7F, 0x6B, 0x63, 0x63, 0x63, 0x00],
            'N' or 'n' => [0x66, 0x76, 0x7E, 0x7E, 0x6E, 0x66, 0x66, 0x00],
            'O' or 'o' => [0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00],
            'P' or 'p' => [0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60, 0x00],
            'Q' or 'q' => [0x3C, 0x66, 0x66, 0x66, 0x6A, 0x6C, 0x36, 0x00],
            'R' or 'r' => [0x7C, 0x66, 0x66, 0x7C, 0x6C, 0x66, 0x66, 0x00],
            'S' or 's' => [0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C, 0x00],
            'T' or 't' => [0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x00],
            'U' or 'u' => [0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00],
            'V' or 'v' => [0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x18, 0x00],
            'W' or 'w' => [0x63, 0x63, 0x63, 0x6B, 0x7F, 0x77, 0x63, 0x00],
            'X' or 'x' => [0x66, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x66, 0x00],
            'Y' or 'y' => [0x66, 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, 0x00],
            'Z' or 'z' => [0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E, 0x00],
            '(' => [0x0C, 0x18, 0x30, 0x30, 0x30, 0x18, 0x0C, 0x00],
            ')' => [0x30, 0x18, 0x0C, 0x0C, 0x0C, 0x18, 0x30, 0x00],
            _ => [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], // Unknown chars
        };
    }
}
