using System.Text;
using Basic.Core;

namespace Basic.Web;

/// <summary>
/// Console implementation for Blazor WebAssembly.
/// Buffers output for display in the UI and handles input via callbacks.
/// </summary>
public sealed class BlazorConsole : IConsole
{
    private readonly StringBuilder outputBuffer = new();
    private string? pendingInput;
    private TaskCompletionSource<string?>? inputTcs;
    private readonly object lockObj = new();

    /// <summary>
    /// Event raised when console output changes.
    /// </summary>
    public event Action? OutputChanged;

    /// <summary>
    /// Event raised when input is requested.
    /// </summary>
    public event Action<string>? InputRequested;

    /// <summary>
    /// Event raised when graphics mode changes (true = graphics, false = text).
    /// </summary>
    public event Action<bool>? GraphicsModeChanged;

    /// <summary>
    /// Event raised when text is written (for canvas rendering).
    /// </summary>
    public event Action<string>? OnWrite;

    /// <summary>
    /// Event raised when console is cleared (for canvas rendering).
    /// </summary>
    public event Action? OnClear;

    /// <summary>
    /// Set graphics mode and raise event.
    /// </summary>
    public void SetGraphicsMode(bool isGraphics)
    {
        GraphicsModeChanged?.Invoke(isGraphics);
    }

    /// <summary>
    /// Current foreground color (0-15).
    /// </summary>
    public int ForegroundColor { get; set; } = 7; // Light gray

    /// <summary>
    /// Get the current console output.
    /// </summary>
    public string Output
    {
        get
        {
            lock (lockObj)
            {
                return outputBuffer.ToString();
            }
        }
    }

    public void Write(string text)
    {
        lock (lockObj)
        {
            outputBuffer.Append(text);
        }
        OnWrite?.Invoke(text);
        OutputChanged?.Invoke();
    }

    public void WriteLine(string text)
    {
        lock (lockObj)
        {
            outputBuffer.AppendLine(text);
        }
        OnWrite?.Invoke(text + "\n");
        OutputChanged?.Invoke();
    }

    public void WriteLine()
    {
        lock (lockObj)
        {
            outputBuffer.AppendLine();
        }
        OnWrite?.Invoke("\n");
        OutputChanged?.Invoke();
    }

    public string? ReadLine()
    {
        // In async context, this would block - we need async version
        // For now, return pending input if available
        lock (lockObj)
        {
            if (pendingInput != null)
            {
                var input = pendingInput;
                pendingInput = null;
                return input;
            }
        }
        return null;
    }

    /// <summary>
    /// Async version of ReadLine for Blazor.
    /// </summary>
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        inputTcs = new TaskCompletionSource<string?>();
        InputRequested?.Invoke(""); // Signal that input is needed

        using var registration = cancellationToken.Register(() =>
        {
            inputTcs.TrySetCanceled();
        });

        try
        {
            return await inputTcs.Task;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Provide input from the UI.
    /// </summary>
    public void ProvideInput(string input)
    {
        lock (lockObj)
        {
            pendingInput = input;
        }
        inputTcs?.TrySetResult(input);
    }

    public void Clear()
    {
        lock (lockObj)
        {
            outputBuffer.Clear();
        }
        OnClear?.Invoke();
        OutputChanged?.Invoke();
    }

    public string ReadKey()
    {
        // Non-blocking key read - handled by JavaScript interop
        return "";
    }

    /// <summary>
    /// Append text with color (for future use).
    /// </summary>
    public void WriteColored(string text, int color)
    {
        ForegroundColor = color;
        Write(text);
    }
}
