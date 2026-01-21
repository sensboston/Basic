using Basic.Core;
using Microsoft.JSInterop;

namespace Basic.Web;

/// <summary>
/// Graphics implementation for Blazor that renders everything to canvas.
/// Both text mode (SCREEN 0) and graphics modes use the canvas.
/// </summary>
public sealed class BlazorCanvasGraphics : IGraphics
{
    private readonly IJSRuntime js;
    private readonly string canvasId;
    private Action<bool>? graphicsModeCallback;
    private BlazorConsole? console;

    private int screenMode;
    private int width = 640;
    private int height = 400;
    private int foregroundColor = 7;
    private int backgroundColor = 0;
    private int activePage;
    private int visualPage;
    private bool isGraphicsMode;
    private bool initialized;

    public int Width => width;
    public int Height => height;
    public int ForegroundColor => foregroundColor;
    public int BackgroundColor => backgroundColor;
    public int ActivePage => activePage;
    public int VisualPage => visualPage;
    public bool IsGraphicsMode => isGraphicsMode;

    public bool KeyAvailable
    {
        get
        {
            if (js is IJSInProcessRuntime jsInProcess)
            {
                return jsInProcess.Invoke<bool>("sharpCanvas.keyAvailable");
            }
            return false;
        }
    }

    public BlazorCanvasGraphics(IJSRuntime js, string canvasId = "graphicsCanvas")
    {
        this.js = js;
        this.canvasId = canvasId;
    }

    public void Initialize()
    {
        if (!initialized)
        {
            _ = js.InvokeVoidAsync("sharpCanvas.init", canvasId);
            initialized = true;
        }
    }

    public void SetGraphicsModeCallback(Action<bool> callback)
    {
        graphicsModeCallback = callback;
    }

    public void SetConsole(BlazorConsole blazorConsole)
    {
        console = blazorConsole;
        // Subscribe to console output events to route to canvas
        console.OnWrite += text => PrintText(text);
        console.OnClear += () => ClearScreen();
    }

    public void SetScreenMode(int mode)
    {
        SetScreenMode(mode, 0, 0);
    }

    public void SetScreenMode(int mode, int activePage, int visualPage)
    {
        screenMode = mode;
        this.activePage = activePage;
        this.visualPage = visualPage;

        // Set resolution based on mode
        switch (mode)
        {
            case 0: // Text mode 80x25
                width = 640;
                height = 400;
                isGraphicsMode = false;
                _ = js.InvokeVoidAsync("sharpCanvas.setTextMode", 80, 25);
                graphicsModeCallback?.Invoke(false);
                return;

            case 1: // 320x200 4-color CGA
                width = 320;
                height = 200;
                break;
            case 2: // 640x200 2-color CGA
                width = 640;
                height = 200;
                break;
            case 7: // 320x200 16-color EGA
                width = 320;
                height = 200;
                break;
            case 8: // 640x200 16-color EGA
                width = 640;
                height = 200;
                break;
            case 9: // 640x350 16-color EGA
                width = 640;
                height = 350;
                break;
            case 12: // 640x480 16-color VGA
                width = 640;
                height = 480;
                break;
            case 13: // 320x200 256-color VGA
                width = 320;
                height = 200;
                break;
            // Extended modes
            case 14: // 640x480 256-color SVGA
            case 15: // 640x480 24-bit RGB
                width = 640;
                height = 480;
                break;
            case 16: // 800x600 256-color SVGA
            case 17: // 800x600 24-bit RGB
                width = 800;
                height = 600;
                break;
            case 18: // 1024x768 256-color XGA
            case 19: // 1024x768 24-bit RGB
                width = 1024;
                height = 768;
                break;
            default:
                width = 640;
                height = 350;
                break;
        }

        isGraphicsMode = true;
        _ = js.InvokeVoidAsync("sharpCanvas.setGraphicsMode", width, height);
        graphicsModeCallback?.Invoke(true);
    }

    public void SetPages(int activePage, int visualPage)
    {
        this.activePage = activePage;
        this.visualPage = visualPage;
    }

    public void SetPixel(int x, int y, int color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        _ = js.InvokeVoidAsync("sharpCanvas.pixel", x, y, color);
    }

    public int GetPixel(int x, int y)
    {
        return 0;
    }

    public void DrawLine(int x1, int y1, int x2, int y2, int color)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.line", x1, y1, x2, y2, color);
    }

    public void DrawBox(int x1, int y1, int x2, int y2, int color, bool filled)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.box", x1, y1, x2, y2, color, filled);
    }

    public void DrawCircle(int cx, int cy, int radius, int color, double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.circle", cx, cy, radius, color, startAngle, endAngle, aspect);
    }

    public void Paint(int x, int y, int fillColor, int borderColor)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.paint", x, y, fillColor, borderColor);
    }

    public void SetColor(int foreground, int background)
    {
        foregroundColor = foreground & 15;
        backgroundColor = background & 15;
        _ = js.InvokeVoidAsync("sharpCanvas.color", foregroundColor, backgroundColor);
    }

    public void Locate(int row, int col)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.locate", row, col);
    }

    public void ClearScreen()
    {
        _ = js.InvokeVoidAsync("sharpCanvas.clear", backgroundColor);
    }

    public void Reset()
    {
        screenMode = 0;
        width = 640;
        height = 400;
        foregroundColor = 7;
        backgroundColor = 0;
        activePage = 0;
        visualPage = 0;
        isGraphicsMode = false;

        if (initialized)
        {
            _ = js.InvokeVoidAsync("sharpCanvas.setTextMode", 80, 25);
        }
    }

    public void Render()
    {
        // No-op: canvas renders immediately
    }

    public void Beep()
    {
        _ = js.InvokeVoidAsync("sharpCanvas.beep", 800, 200);
    }

    public void SetWidth(int width)
    {
        // Text width setting - could change cols
    }

    public void Sound(int frequency, int duration)
    {
        int durationMs = (int)(duration * 1000.0 / 18.2);
        _ = js.InvokeVoidAsync("sharpCanvas.beep", frequency, durationMs);
    }

    public void Play(string commands)
    {
        _ = js.InvokeVoidAsync("sharpCanvas.beep", 440, 200);
    }

    public void PrintText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (text.EndsWith("\n"))
        {
            _ = js.InvokeVoidAsync("sharpCanvas.println", text.TrimEnd('\n', '\r'));
        }
        else
        {
            _ = js.InvokeVoidAsync("sharpCanvas.print", text, false);
        }
    }

    public string ReadKey()
    {
        if (js is IJSInProcessRuntime jsInProcess)
        {
            return jsInProcess.Invoke<string>("sharpCanvas.readKey") ?? "";
        }
        return "";
    }
}
