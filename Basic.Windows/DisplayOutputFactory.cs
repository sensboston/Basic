using Basic.Core;

namespace Basic.Windows;

/// <summary>
/// Factory for creating Windows display outputs.
/// </summary>
public static class DisplayOutputFactory
{
    /// <summary>
    /// Create a display output based on the specified mode.
    /// </summary>
    public static IDisplayOutput Create(WindowsDisplayMode mode, int scale = 2)
    {
        return mode switch
        {
            WindowsDisplayMode.ConsoleOverlay => new GdiConsoleOutput { Scale = scale },
            WindowsDisplayMode.SeparateWindow => new GdiWindowOutput { Scale = scale },
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    /// <summary>
    /// Create a console overlay display output.
    /// </summary>
    public static GdiConsoleOutput CreateConsoleOverlay(int scale = 2, int offsetX = 8, int offsetY = 8)
    {
        return new GdiConsoleOutput
        {
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY
        };
    }

    /// <summary>
    /// Create a separate window display output.
    /// </summary>
    public static GdiWindowOutput CreateWindow(int scale = 2, string title = "SharpBasic Graphics")
    {
        return new GdiWindowOutput
        {
            Scale = scale,
            Title = title
        };
    }
}
