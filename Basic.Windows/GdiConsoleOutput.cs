using System.Runtime.InteropServices;
using Basic.Core;
using static Basic.Windows.NativeMethods;

namespace Basic.Windows;

/// <summary>
/// GDI-based display output that draws directly on the console window.
/// Uses the Far Manager technique: GetConsoleWindow() + GetDC() + BitBlt().
/// </summary>
public sealed class GdiConsoleOutput : IDisplayOutput
{
    private IntPtr consoleHwnd;
    private IntPtr memDc;
    private IntPtr bitmap;
    private IntPtr oldBitmap;
    private IntPtr pixelData;
    private int frameWidth;
    private int frameHeight;
    private bool disposed;

    /// <summary>
    /// X offset for graphics rendering within the console window.
    /// </summary>
    public int OffsetX { get; set; } = 8;

    /// <summary>
    /// Y offset for graphics rendering within the console window.
    /// </summary>
    public int OffsetY { get; set; } = 8;

    /// <summary>
    /// Scale factor for rendering (1 = original size, 2 = double size).
    /// </summary>
    public int Scale { get; set; } = 2;

    public bool IsValid => consoleHwnd != IntPtr.Zero && !disposed;
    public bool KeyAvailable => Console.KeyAvailable;

    public GdiConsoleOutput()
    {
        consoleHwnd = GetConsoleWindow();
        if (consoleHwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("No console window available");
        }
    }

    public void Initialize(int width, int height)
    {
        Cleanup();

        frameWidth = width;
        frameHeight = height;

        // Get console window DC
        IntPtr hdc = GetDC(consoleHwnd);
        if (hdc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get console DC");
        }

        try
        {
            // Create memory DC
            memDc = CreateCompatibleDC(hdc);
            if (memDc == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create compatible DC");
            }

            // Create DIB section for direct pixel access
            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // Top-down DIB (negative height)
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB,
                    biSizeImage = 0,
                    biXPelsPerMeter = 0,
                    biYPelsPerMeter = 0,
                    biClrUsed = 0,
                    biClrImportant = 0
                }
            };

            bitmap = CreateDIBSection(hdc, ref bmi, DIB_RGB_COLORS, out pixelData, IntPtr.Zero, 0);
            if (bitmap == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create DIB section");
            }

            oldBitmap = SelectObject(memDc, bitmap);
        }
        finally
        {
            ReleaseDC(consoleHwnd, hdc);
        }
    }

    public void Present(ReadOnlySpan<byte> bgraPixels, int width, int height)
    {
        if (disposed || pixelData == IntPtr.Zero)
            return;

        if (width != frameWidth || height != frameHeight)
        {
            Initialize(width, height);
        }

        // Copy pixel data to DIB section (direct memory copy - very fast)
        unsafe
        {
            fixed (byte* src = bgraPixels)
            {
                Buffer.MemoryCopy(src, (void*)pixelData, bgraPixels.Length, bgraPixels.Length);
            }
        }

        // Blit to console window
        IntPtr hdc = GetDC(consoleHwnd);
        if (hdc != IntPtr.Zero)
        {
            try
            {
                if (Scale == 1)
                {
                    // Direct blit at original size - fastest path
                    BitBlt(hdc, OffsetX, OffsetY, frameWidth, frameHeight, memDc, 0, 0, SRCCOPY);
                }
                else
                {
                    // Use StretchBlt instead of StretchDIBits - faster when source is already in DC
                    // Set COLORONCOLOR mode for fastest stretching (no interpolation)
                    SetStretchBltMode(hdc, COLORONCOLOR);
                    StretchBlt(
                        hdc,
                        OffsetX, OffsetY,
                        frameWidth * Scale, frameHeight * Scale,
                        memDc,
                        0, 0,
                        frameWidth, frameHeight,
                        SRCCOPY);
                }
            }
            finally
            {
                ReleaseDC(consoleHwnd, hdc);
            }
        }
    }

    public bool ProcessEvents()
    {
        // Console handles its own events
        // Check if console window is still valid
        return GetWindowRect(consoleHwnd, out _);
    }

    public string ReadKey()
    {
        // In console overlay mode, read from console
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                return "\r";
            if (key.Key == ConsoleKey.Backspace)
                return "\b";
            if (key.Key == ConsoleKey.Escape)
                return "\x1B";
            if (key.Key == ConsoleKey.Tab)
                return "\t";
            // Handle arrow keys and special keys
            if (key.Key == ConsoleKey.UpArrow)
                return "\0H";
            if (key.Key == ConsoleKey.DownArrow)
                return "\0P";
            if (key.Key == ConsoleKey.LeftArrow)
                return "\0K";
            if (key.Key == ConsoleKey.RightArrow)
                return "\0M";
            if (key.KeyChar != '\0')
                return key.KeyChar.ToString();
        }
        return "";
    }

    /// <summary>
    /// Position the graphics below a specific console row.
    /// </summary>
    public void SetPositionBelowRow(int consoleRow)
    {
        // Approximate character height (depends on console font)
        const int charHeight = 16;
        OffsetY = consoleRow * charHeight + 8;
    }

    /// <summary>
    /// Clear the graphics area on the console.
    /// </summary>
    public void ClearOverlay()
    {
        IntPtr hdc = GetDC(consoleHwnd);
        if (hdc != IntPtr.Zero)
        {
            try
            {
                // Create a black brush
                IntPtr brush = CreateSolidBrush(0x00000000); // Black in COLORREF format

                var rect = new RECT
                {
                    Left = OffsetX,
                    Top = OffsetY,
                    Right = OffsetX + frameWidth * Scale,
                    Bottom = OffsetY + frameHeight * Scale
                };

                // Fill with black (effectively clearing the overlay)
                // Using PatBlt would be better but let's keep it simple
                unsafe
                {
                    if (pixelData != IntPtr.Zero)
                    {
                        // Clear pixel buffer
                        int size = frameWidth * frameHeight * 4;
                        new Span<byte>((void*)pixelData, size).Clear();

                        // Blit the cleared buffer
                        BitBlt(hdc, OffsetX, OffsetY, frameWidth * Scale, frameHeight * Scale, memDc, 0, 0, SRCCOPY);
                    }
                }

                DeleteObject(brush);
            }
            finally
            {
                ReleaseDC(consoleHwnd, hdc);
            }
        }
    }

    private void Cleanup()
    {
        if (oldBitmap != IntPtr.Zero && memDc != IntPtr.Zero)
        {
            SelectObject(memDc, oldBitmap);
            oldBitmap = IntPtr.Zero;
        }

        if (bitmap != IntPtr.Zero)
        {
            DeleteObject(bitmap);
            bitmap = IntPtr.Zero;
        }

        if (memDc != IntPtr.Zero)
        {
            DeleteDC(memDc);
            memDc = IntPtr.Zero;
        }

        pixelData = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            ClearOverlay();
            Cleanup();
            disposed = true;
        }
    }
}
