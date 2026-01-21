using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Basic.Core;
using static Basic.Windows.NativeMethods;

namespace Basic.Windows;

/// <summary>
/// GDI-based display output that creates a separate graphics window.
/// </summary>
public sealed class GdiWindowOutput : IDisplayOutput
{
    private const string WindowClassName = "BasicGraphicsWindow";

    private IntPtr hwnd;
    private IntPtr memDc;
    private IntPtr bitmap;
    private IntPtr oldBitmap;
    private IntPtr pixelData;
    private int frameWidth;
    private int frameHeight;
    private bool disposed;
    private bool windowClosed;
    private readonly WndProcDelegate wndProcDelegate;
    private readonly ConcurrentQueue<string> keyQueue = new();

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Scale factor for rendering (1 = original size, 2 = double size).
    /// </summary>
    public int Scale { get; set; } = 2;

    /// <summary>
    /// Window title.
    /// </summary>
    public string Title { get; set; } = "SharpBasic Graphics";

    public bool IsValid => hwnd != IntPtr.Zero && !disposed && !windowClosed;
    public bool KeyAvailable => !keyQueue.IsEmpty;

    public GdiWindowOutput()
    {
        // Keep delegate alive to prevent GC
        wndProcDelegate = WndProc;
    }

    public void Initialize(int width, int height)
    {
        Cleanup();

        frameWidth = width;
        frameHeight = height;
        windowClosed = false;

        // Register window class
        var hInstance = GetModuleHandleW(IntPtr.Zero);

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = hInstance,
            hIcon = IntPtr.Zero,
            hCursor = LoadCursorW(IntPtr.Zero, IDC_ARROW),
            hbrBackground = IntPtr.Zero,
            lpszMenuName = null,
            lpszClassName = WindowClassName,
            hIconSm = IntPtr.Zero
        };

        RegisterClassExW(ref wc);

        // Calculate window size to fit client area
        int windowWidth = width * Scale + 16; // Add border
        int windowHeight = height * Scale + 39; // Add title bar + border

        // Create window
        hwnd = CreateWindowExW(
            0,
            WindowClassName,
            Title,
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            100, 100,
            windowWidth, windowHeight,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");
        }

        // Create memory DC and DIB section
        IntPtr hdc = GetDC(hwnd);
        if (hdc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get window DC");
        }

        try
        {
            memDc = CreateCompatibleDC(hdc);
            if (memDc == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create compatible DC");
            }

            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // Top-down DIB
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB
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
            ReleaseDC(hwnd, hdc);
        }

        ShowWindow(hwnd, SW_SHOW);
        UpdateWindow(hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_PAINT:
                OnPaint(hwnd);
                return IntPtr.Zero;

            case WM_CLOSE:
                windowClosed = true;
                DestroyWindow(hwnd);
                return IntPtr.Zero;

            case WM_DESTROY:
                windowClosed = true;
                return IntPtr.Zero;

            case WM_CHAR:
                // Regular character input
                char c = (char)wParam;
                if (c >= 32 || c == '\r' || c == '\b' || c == '\t' || c == 27) // printable, Enter, Backspace, Tab, Escape
                {
                    keyQueue.Enqueue(c == '\r' ? "\r" : c.ToString());
                }
                return IntPtr.Zero;

            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
                // Handle special keys that don't generate WM_CHAR
                int vk = (int)wParam;
                string? specialKey = vk switch
                {
                    VK_UP => "\0H",      // Extended key code for Up arrow
                    VK_DOWN => "\0P",    // Down arrow
                    VK_LEFT => "\0K",    // Left arrow
                    VK_RIGHT => "\0M",   // Right arrow
                    VK_HOME => "\0G",
                    VK_END => "\0O",
                    VK_PRIOR => "\0I",   // Page Up
                    VK_NEXT => "\0Q",    // Page Down
                    VK_INSERT => "\0R",
                    VK_DELETE => "\0S",
                    VK_F1 => "\0;",
                    VK_F2 => "\0<",
                    VK_F3 => "\0=",
                    VK_F4 => "\0>",
                    VK_F5 => "\0?",
                    VK_F6 => "\0@",
                    VK_F7 => "\0A",
                    VK_F8 => "\0B",
                    VK_F9 => "\0C",
                    VK_F10 => "\0D",
                    _ => null
                };
                if (specialKey != null)
                {
                    keyQueue.Enqueue(specialKey);
                    return IntPtr.Zero;
                }
                break;

            default:
                break;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private void OnPaint(IntPtr hwnd)
    {
        IntPtr hdc = BeginPaint(hwnd, out PAINTSTRUCT ps);

        if (memDc != IntPtr.Zero && pixelData != IntPtr.Zero)
        {
            if (Scale == 1)
            {
                BitBlt(hdc, 0, 0, frameWidth, frameHeight, memDc, 0, 0, SRCCOPY);
            }
            else
            {
                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = frameWidth,
                        biHeight = -frameHeight,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = BI_RGB
                    }
                };

                StretchDIBits(
                    hdc,
                    0, 0,
                    frameWidth * Scale, frameHeight * Scale,
                    0, 0,
                    frameWidth, frameHeight,
                    pixelData,
                    ref bmi,
                    DIB_RGB_COLORS,
                    SRCCOPY);
            }
        }

        EndPaint(hwnd, ref ps);
    }

    public void Present(ReadOnlySpan<byte> bgraPixels, int width, int height)
    {
        if (disposed || windowClosed || pixelData == IntPtr.Zero)
            return;

        if (width != frameWidth || height != frameHeight)
        {
            Initialize(width, height);
        }

        // Copy pixel data to DIB section
        unsafe
        {
            fixed (byte* src = bgraPixels)
            {
                Buffer.MemoryCopy(src, (void*)pixelData, bgraPixels.Length, bgraPixels.Length);
            }
        }

        // Invalidate window to trigger repaint
        InvalidateRect(hwnd, IntPtr.Zero, false);
        UpdateWindow(hwnd);
    }

    public bool ProcessEvents()
    {
        if (windowClosed)
            return false;

        while (PeekMessageW(out MSG msg, IntPtr.Zero, 0, 0, PM_REMOVE))
        {
            if (msg.message == WM_QUIT)
            {
                windowClosed = true;
                return false;
            }

            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }

        return !windowClosed;
    }

    public string ReadKey()
    {
        // Process pending messages to get any new key presses
        ProcessEvents();

        if (keyQueue.TryDequeue(out string? key))
        {
            return key;
        }
        return "";
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

        if (hwnd != IntPtr.Zero)
        {
            DestroyWindow(hwnd);
            hwnd = IntPtr.Zero;
        }

        pixelData = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Cleanup();
            disposed = true;
        }
    }
}
