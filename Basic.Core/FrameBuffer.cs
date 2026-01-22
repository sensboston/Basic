namespace Basic.Core;

/// <summary>
/// In-memory framebuffer with software rendering algorithms.
/// All GW-BASIC graphics operations render to this buffer.
/// </summary>
public sealed class FrameBuffer
{
    private readonly byte[] pixels;
    private readonly uint[] palette;

    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Raw pixel data in BGRA format (4 bytes per pixel).
    /// </summary>
    public ReadOnlySpan<byte> Pixels => pixels;

    /// <summary>
    /// Current foreground color index.
    /// </summary>
    public int ForegroundColor { get; set; } = 15; // White

    /// <summary>
    /// Current background color index.
    /// </summary>
    public int BackgroundColor { get; set; } = 0; // Black

    /// <summary>
    /// Creates a new framebuffer with the specified dimensions.
    /// </summary>
    public FrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new byte[width * height * 4];
        palette = CreateDefaultPalette();
        Clear(0);
    }

    /// <summary>
    /// Creates the default 256-color palette (first 16 are EGA colors, rest are grayscale/color ramp).
    /// </summary>
    private static uint[] CreateDefaultPalette()
    {
        var pal = new uint[256];

        // First 16 colors: standard EGA palette
        pal[0] = 0xFF000000;  // Black
        pal[1] = 0xFF0000AA;  // Blue
        pal[2] = 0xFF00AA00;  // Green
        pal[3] = 0xFF00AAAA;  // Cyan
        pal[4] = 0xFFAA0000;  // Red
        pal[5] = 0xFFAA00AA;  // Magenta
        pal[6] = 0xFFAA5500;  // Brown
        pal[7] = 0xFFAAAAAA;  // Light Gray
        pal[8] = 0xFF555555;  // Dark Gray
        pal[9] = 0xFF5555FF;  // Light Blue
        pal[10] = 0xFF55FF55; // Light Green
        pal[11] = 0xFF55FFFF; // Light Cyan
        pal[12] = 0xFFFF5555; // Light Red
        pal[13] = 0xFFFF55FF; // Light Magenta
        pal[14] = 0xFFFFFF55; // Yellow
        pal[15] = 0xFFFFFFFF; // White

        // Colors 16-231: 6x6x6 color cube (216 colors)
        int idx = 16;
        for (int r = 0; r < 6; r++)
        {
            for (int g = 0; g < 6; g++)
            {
                for (int b = 0; b < 6; b++)
                {
                    byte rr = (byte)(r * 51);
                    byte gg = (byte)(g * 51);
                    byte bb = (byte)(b * 51);
                    pal[idx++] = 0xFF000000 | ((uint)rr << 16) | ((uint)gg << 8) | bb;
                }
            }
        }

        // Colors 232-255: grayscale ramp (24 shades)
        for (int i = 0; i < 24; i++)
        {
            byte gray = (byte)(8 + i * 10);
            pal[idx++] = 0xFF000000 | ((uint)gray << 16) | ((uint)gray << 8) | gray;
        }

        return pal;
    }

    /// <summary>
    /// Set a palette entry (0-255).
    /// </summary>
    public void SetPaletteColor(int index, byte r, byte g, byte b)
    {
        if (index >= 0 && index < 256)
        {
            palette[index] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
        }
    }

    /// <summary>
    /// Get palette color as BGRA.
    /// </summary>
    public uint GetPaletteColor(int index)
    {
        return index >= 0 && index < 256 ? palette[index] : 0xFF000000;
    }

    /// <summary>
    /// Clear the framebuffer with the specified color index.
    /// </summary>
    public void Clear(int colorIndex)
    {
        uint color = GetPaletteColor(colorIndex);
        byte b = (byte)(color & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte r = (byte)((color >> 16) & 0xFF);
        byte a = (byte)((color >> 24) & 0xFF);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
            pixels[i + 3] = a;
        }
    }

    /// <summary>
    /// Set a pixel at the specified coordinates.
    /// If colorValue > 255, it's treated as RGB24 (0x01RRGGBB format from RGB function).
    /// Otherwise it's treated as palette index (0-255).
    /// </summary>
    public void SetPixel(int x, int y, int colorValue)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        int offset = (y * Width + x) * 4;

        if (colorValue > 0xFFFFFF || colorValue > 255)
        {
            // 24-bit RGB color (0x01RRGGBB from RGB function, strip the marker bit)
            int rgb = colorValue & 0xFFFFFF;
            pixels[offset] = (byte)(rgb & 0xFF);             // B
            pixels[offset + 1] = (byte)((rgb >> 8) & 0xFF);  // G
            pixels[offset + 2] = (byte)((rgb >> 16) & 0xFF); // R
            pixels[offset + 3] = 0xFF;                        // A
        }
        else
        {
            // Palette index (0-255)
            uint color = GetPaletteColor(colorValue);
            pixels[offset] = (byte)(color & 0xFF);           // B
            pixels[offset + 1] = (byte)((color >> 8) & 0xFF);  // G
            pixels[offset + 2] = (byte)((color >> 16) & 0xFF); // R
            pixels[offset + 3] = (byte)((color >> 24) & 0xFF); // A
        }
    }

    /// <summary>
    /// Get the color value at the specified coordinates.
    /// In 24-bit mode returns RGB value, otherwise returns palette index.
    /// Returns -1 if out of bounds.
    /// </summary>
    public int GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return -1;

        int offset = (y * Width + x) * 4;
        byte b = pixels[offset];
        byte g = pixels[offset + 1];
        byte r = pixels[offset + 2];
        uint color = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;

        // Find matching palette color
        for (int i = 0; i < 256; i++)
        {
            if (palette[i] == color)
                return i;
        }

        // Return RGB value if no palette match (24-bit mode)
        return (r << 16) | (g << 8) | b;
    }

    /// <summary>
    /// Get raw BGRA value at coordinates.
    /// </summary>
    private uint GetPixelRaw(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        int offset = (y * Width + x) * 4;
        return (uint)(pixels[offset] |
                      (pixels[offset + 1] << 8) |
                      (pixels[offset + 2] << 16) |
                      (pixels[offset + 3] << 24));
    }

    /// <summary>
    /// Draw a line using Bresenham's algorithm.
    /// </summary>
    public void DrawLine(int x1, int y1, int x2, int y2, int colorIndex)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixel(x1, y1, colorIndex);

            if (x1 == x2 && y1 == y2)
                break;

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

    /// <summary>
    /// Draw a rectangle (outline or filled).
    /// </summary>
    public void DrawBox(int x1, int y1, int x2, int y2, int colorIndex, bool filled)
    {
        // Normalize coordinates
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        if (filled)
        {
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    SetPixel(x, y, colorIndex);
                }
            }
        }
        else
        {
            DrawLine(x1, y1, x2, y1, colorIndex); // Top
            DrawLine(x1, y2, x2, y2, colorIndex); // Bottom
            DrawLine(x1, y1, x1, y2, colorIndex); // Left
            DrawLine(x2, y1, x2, y2, colorIndex); // Right
        }
    }

    /// <summary>
    /// Draw a circle or ellipse using midpoint algorithm.
    /// </summary>
    public void DrawCircle(int cx, int cy, int radius, int colorIndex,
        double startAngle = 0, double endAngle = Math.PI * 2, double aspect = 1.0)
    {
        if (Math.Abs(startAngle) < 0.001 && Math.Abs(endAngle - Math.PI * 2) < 0.001)
        {
            // Full circle/ellipse - use midpoint algorithm
            DrawFullEllipse(cx, cy, radius, (int)(radius * aspect), colorIndex);
        }
        else
        {
            // Arc - use parametric drawing
            DrawArc(cx, cy, radius, colorIndex, startAngle, endAngle, aspect);
        }
    }

    private void DrawFullEllipse(int cx, int cy, int rx, int ry, int colorIndex)
    {
        if (rx == ry)
        {
            // Circle - use midpoint circle algorithm
            int x = 0;
            int y = rx;
            int d = 1 - rx;

            DrawCirclePoints(cx, cy, x, y, colorIndex);

            while (x < y)
            {
                x++;
                if (d < 0)
                {
                    d += 2 * x + 1;
                }
                else
                {
                    y--;
                    d += 2 * (x - y) + 1;
                }
                DrawCirclePoints(cx, cy, x, y, colorIndex);
            }
        }
        else
        {
            // Ellipse - use parametric approach for simplicity
            int steps = Math.Max(rx, ry) * 4;
            double angleStep = 2 * Math.PI / steps;

            int prevX = cx + rx;
            int prevY = cy;

            for (int i = 1; i <= steps; i++)
            {
                double angle = i * angleStep;
                int x = cx + (int)(rx * Math.Cos(angle));
                int y = cy + (int)(ry * Math.Sin(angle));

                DrawLine(prevX, prevY, x, y, colorIndex);
                prevX = x;
                prevY = y;
            }
        }
    }

    private void DrawCirclePoints(int cx, int cy, int x, int y, int colorIndex)
    {
        SetPixel(cx + x, cy + y, colorIndex);
        SetPixel(cx - x, cy + y, colorIndex);
        SetPixel(cx + x, cy - y, colorIndex);
        SetPixel(cx - x, cy - y, colorIndex);
        SetPixel(cx + y, cy + x, colorIndex);
        SetPixel(cx - y, cy + x, colorIndex);
        SetPixel(cx + y, cy - x, colorIndex);
        SetPixel(cx - y, cy - x, colorIndex);
    }

    private void DrawArc(int cx, int cy, int radius, int colorIndex,
        double startAngle, double endAngle, double aspect)
    {
        int rx = radius;
        int ry = (int)(radius * aspect);

        // Normalize angles
        if (endAngle < startAngle)
            endAngle += 2 * Math.PI;

        double arcLength = endAngle - startAngle;
        int steps = Math.Max(16, (int)(arcLength * Math.Max(rx, ry)));
        double angleStep = arcLength / steps;

        int prevX = cx + (int)(rx * Math.Cos(startAngle));
        int prevY = cy - (int)(ry * Math.Sin(startAngle));

        for (int i = 1; i <= steps; i++)
        {
            double angle = startAngle + i * angleStep;
            int x = cx + (int)(rx * Math.Cos(angle));
            int y = cy - (int)(ry * Math.Sin(angle));

            DrawLine(prevX, prevY, x, y, colorIndex);
            prevX = x;
            prevY = y;
        }
    }

    /// <summary>
    /// Flood fill using stack-based algorithm.
    /// </summary>
    public void FloodFill(int x, int y, int fillColorIndex, int borderColorIndex)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        uint fillColor = GetPaletteColor(fillColorIndex);
        uint borderColor = GetPaletteColor(borderColorIndex);
        uint targetColor = GetPixelRaw(x, y);

        // Don't fill if starting on border or already fill color
        if (targetColor == borderColor || targetColor == fillColor)
            return;

        var stack = new Stack<(int x, int y)>();
        stack.Push((x, y));

        while (stack.Count > 0)
        {
            var (px, py) = stack.Pop();

            if (px < 0 || px >= Width || py < 0 || py >= Height)
                continue;

            uint currentColor = GetPixelRaw(px, py);

            if (currentColor == borderColor || currentColor == fillColor)
                continue;

            // Scan left
            int left = px;
            while (left > 0)
            {
                uint c = GetPixelRaw(left - 1, py);
                if (c == borderColor || c == fillColor)
                    break;
                left--;
            }

            // Scan right
            int right = px;
            while (right < Width - 1)
            {
                uint c = GetPixelRaw(right + 1, py);
                if (c == borderColor || c == fillColor)
                    break;
                right++;
            }

            // Fill the span
            for (int i = left; i <= right; i++)
            {
                SetPixel(i, py, fillColorIndex);
            }

            // Check line above
            if (py > 0)
            {
                bool inSpan = false;
                for (int i = left; i <= right; i++)
                {
                    uint c = GetPixelRaw(i, py - 1);
                    if (c != borderColor && c != fillColor)
                    {
                        if (!inSpan)
                        {
                            stack.Push((i, py - 1));
                            inSpan = true;
                        }
                    }
                    else
                    {
                        inSpan = false;
                    }
                }
            }

            // Check line below
            if (py < Height - 1)
            {
                bool inSpan = false;
                for (int i = left; i <= right; i++)
                {
                    uint c = GetPixelRaw(i, py + 1);
                    if (c != borderColor && c != fillColor)
                    {
                        if (!inSpan)
                        {
                            stack.Push((i, py + 1));
                            inSpan = true;
                        }
                    }
                    else
                    {
                        inSpan = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Copy a rectangular region to a byte array (for GET command).
    /// </summary>
    public byte[] CopyRegion(int x1, int y1, int x2, int y2)
    {
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        int w = x2 - x1 + 1;
        int h = y2 - y1 + 1;
        byte[] result = new byte[4 + w * h]; // 4 bytes for dimensions

        result[0] = (byte)(w & 0xFF);
        result[1] = (byte)((w >> 8) & 0xFF);
        result[2] = (byte)(h & 0xFF);
        result[3] = (byte)((h >> 8) & 0xFF);

        int idx = 4;
        for (int y = y1; y <= y2; y++)
        {
            for (int x = x1; x <= x2; x++)
            {
                result[idx++] = (byte)GetPixel(x, y);
            }
        }

        return result;
    }

    /// <summary>
    /// Paste a region from byte array (for PUT command).
    /// </summary>
    public void PasteRegion(int x, int y, byte[] data, PutAction action = PutAction.Overwrite)
    {
        if (data.Length < 4) return;

        int w = data[0] | (data[1] << 8);
        int h = data[2] | (data[3] << 8);

        int idx = 4;
        for (int dy = 0; dy < h && idx < data.Length; dy++)
        {
            for (int dx = 0; dx < w && idx < data.Length; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                int colorIndex = data[idx++];

                if (px >= 0 && px < Width && py >= 0 && py < Height)
                {
                    switch (action)
                    {
                        case PutAction.Overwrite:
                            SetPixel(px, py, colorIndex);
                            break;
                        case PutAction.Xor:
                            SetPixel(px, py, GetPixel(px, py) ^ colorIndex);
                            break;
                        case PutAction.Or:
                            SetPixel(px, py, GetPixel(px, py) | colorIndex);
                            break;
                        case PutAction.And:
                            SetPixel(px, py, GetPixel(px, py) & colorIndex);
                            break;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Action for PUT graphics command.
/// </summary>
public enum PutAction
{
    Overwrite,
    Xor,
    Or,
    And
}
