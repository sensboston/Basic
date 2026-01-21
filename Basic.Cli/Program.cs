using System.Diagnostics;
using Basic.Core;
using Basic.Windows;

namespace Basic.Cli;

public static class Program
{
    private static IDisplayOutput? displayOutput;
    private static SoftwareRenderer? graphicsRenderer;
    private static WindowsDisplayMode displayMode = WindowsDisplayMode.ConsoleOverlay;
    private static CancellationTokenSource? cts;

    public static int Main(string[] args)
    {
        // Ensure cursor is visible
        try { Console.CursorVisible = true; } catch { }

        var console = new SystemConsole();

        // Set up Ctrl+C handler to interrupt BASIC programs, not the console
        Console.CancelKeyPress += (sender, e) =>
        {
            if (cts != null)
            {
                e.Cancel = true; // Prevent console from closing
                cts.Cancel();   // Signal cancellation to BASIC interpreter
            }
        };

        // Parse command line args
        var fileArgs = new List<string>();
        foreach (var arg in args)
        {
            if (arg.Equals("--window", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-w", StringComparison.OrdinalIgnoreCase))
            {
                displayMode = WindowsDisplayMode.SeparateWindow;
            }
            else if (arg.Equals("--console", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                displayMode = WindowsDisplayMode.ConsoleOverlay;
            }
            else
            {
                fileArgs.Add(arg);
            }
        }

        // Create graphics output
        displayOutput = CreateDisplayOutput();
        graphicsRenderer = new SoftwareRenderer(displayOutput);

        try
        {
            if (fileArgs.Count > 0)
            {
                return RunFile(fileArgs[0], console, graphicsRenderer);
            }

            RunRepl(console, graphicsRenderer);
            return 0;
        }
        finally
        {
            displayOutput?.Dispose();
        }
    }

    private static IDisplayOutput CreateDisplayOutput()
    {
        try
        {
            return displayMode switch
            {
                // Use GDI-based console overlay for real graphics on console window
                WindowsDisplayMode.ConsoleOverlay => new GdiConsoleOutput(),
                WindowsDisplayMode.SeparateWindow => new GdiWindowOutput
                {
                    Scale = 2,
                    Title = "SharpBasic Graphics"
                },
                _ => new NullDisplayOutput()
            };
        }
        catch (InvalidOperationException)
        {
            // No console window available - fallback to separate window or null
            if (displayMode == WindowsDisplayMode.ConsoleOverlay)
            {
                try
                {
                    return new GdiWindowOutput { Scale = 2, Title = "SharpBasic Graphics" };
                }
                catch
                {
                    return new NullDisplayOutput();
                }
            }
            return new NullDisplayOutput();
        }
    }

    private static int RunFile(string path, IConsole console, SoftwareRenderer renderer)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        try
        {
            var source = File.ReadAllText(path);
            var interpreter = new BasicInterpreter(console, renderer);

            cts = new CancellationTokenSource();
            try
            {
                interpreter.Execute(source, cts.Token);
            }
            finally
            {
                cts.Dispose();
                cts = null;
            }

            // Keep window open if graphics was used
            WaitForClose(renderer);

            return 0;
        }
        catch (Exception ex) when (ex is LexerException or ParserException or RuntimeException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void RunRepl(IConsole console, SoftwareRenderer renderer)
    {
        Console.WriteLine("SharpBasic Interpreter");
        Console.WriteLine("Type program lines with line numbers, RUN to execute, or NEW to clear.");
        Console.WriteLine("Graphics: " + (displayMode == WindowsDisplayMode.ConsoleOverlay ? "Console Overlay" : "Separate Window"));
        Console.WriteLine("  SCREEN n - set graphics mode | DISPLAY WINDOW/CONSOLE - switch output");
        Console.WriteLine("Shell commands: DIR, CD, TYPE, COPY, etc. work directly");
        Console.WriteLine();

        var lines = new SortedDictionary<int, string>();
        var interpreter = new BasicInterpreter(console, renderer);

        while (true)
        {
            // Process graphics events
            if (displayOutput != null && !displayOutput.ProcessEvents())
            {
                break;
            }

            // Ensure cursor is visible before input
            try { Console.CursorVisible = true; } catch { }

            Console.Write("Ok\n");
            var input = Console.ReadLine();

            if (input == null)
            {
                break;
            }

            var trimmed = input.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            // Check for immediate commands
            if (trimmed.Equals("RUN", StringComparison.OrdinalIgnoreCase))
            {
                RunProgram(lines, interpreter);
                continue;
            }

            // RUN "filename" - load and run
            if (trimmed.StartsWith("RUN ", StringComparison.OrdinalIgnoreCase))
            {
                var runMatch = System.Text.RegularExpressions.Regex.Match(
                    trimmed, @"^RUN\s+""?([^""]+)""?\s*$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (runMatch.Success)
                {
                    HandleLoadCommand("LOAD " + runMatch.Groups[1].Value, lines);
                    RunProgram(lines, interpreter);
                    continue;
                }
            }

            if (trimmed.Equals("NEW", StringComparison.OrdinalIgnoreCase))
            {
                lines.Clear();
                continue;
            }

            if (trimmed.Equals("LIST", StringComparison.OrdinalIgnoreCase))
            {
                ListProgram(lines);
                continue;
            }

            if (trimmed.Equals("CLS", StringComparison.OrdinalIgnoreCase))
            {
                console.Clear();
                continue;
            }

            if (trimmed.Equals("EXIT", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            // DISPLAY command to switch modes
            if (trimmed.StartsWith("DISPLAY", StringComparison.OrdinalIgnoreCase))
            {
                HandleDisplayCommand(trimmed, ref interpreter, console);
                continue;
            }

            // LOAD/OPEN command
            if (trimmed.StartsWith("LOAD", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("OPEN", StringComparison.OrdinalIgnoreCase))
            {
                HandleLoadCommand(trimmed, lines);
                continue;
            }

            // SAVE command
            if (trimmed.StartsWith("SAVE", StringComparison.OrdinalIgnoreCase))
            {
                HandleSaveCommand(trimmed, lines);
                continue;
            }

            // Shell commands (directly execute Windows commands)
            if (TryExecuteShellCommand(trimmed))
            {
                continue;
            }

            // Try to parse as a numbered line
            if (TryParseLineNumber(trimmed, out int lineNumber, out string? rest))
            {
                if (string.IsNullOrWhiteSpace(rest))
                {
                    // Delete line
                    lines.Remove(lineNumber);
                }
                else
                {
                    // Add/replace line
                    lines[lineNumber] = rest;
                }
            }
            else
            {
                Console.WriteLine("Syntax error - enter line number before statement");
            }
        }
    }

    private static void HandleDisplayCommand(string command, ref BasicInterpreter interpreter, IConsole console)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            Console.WriteLine("Current mode: " + (displayMode == WindowsDisplayMode.ConsoleOverlay ? "CONSOLE" : "WINDOW"));
            Console.WriteLine("Usage: DISPLAY CONSOLE | DISPLAY WINDOW");
            return;
        }

        var mode = parts[1].ToUpperInvariant();
        var newMode = mode switch
        {
            "CONSOLE" => WindowsDisplayMode.ConsoleOverlay,
            "WINDOW" => WindowsDisplayMode.SeparateWindow,
            _ => displayMode
        };

        if (newMode != displayMode)
        {
            displayMode = newMode;
            displayOutput?.Dispose();
            displayOutput = CreateDisplayOutput();
            graphicsRenderer = new SoftwareRenderer(displayOutput);
            interpreter = new BasicInterpreter(console, graphicsRenderer);
            Console.WriteLine($"Switched to {mode} mode");
        }
    }

    private static void HandleLoadCommand(string command, SortedDictionary<int, string> lines)
    {
        // Parse: LOAD "filename" or LOAD filename or OPEN filename
        var match = System.Text.RegularExpressions.Regex.Match(
            command, @"^(?:LOAD|OPEN)\s+""?([^""]+)""?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Usage: LOAD \"filename.bas\" or OPEN filename");
            return;
        }

        var filename = match.Groups[1].Value.Trim();

        // Add .bas extension if not present
        if (!filename.EndsWith(".bas", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".bas";
        }

        // Try to find file in current directory, then in samples/
        if (!File.Exists(filename))
        {
            var samplesPath = Path.Combine("samples", filename);
            if (File.Exists(samplesPath))
            {
                filename = samplesPath;
            }
            else
            {
                // Try relative to executable
                var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (exeDir != null)
                {
                    var exeSamplesPath = Path.Combine(exeDir, "samples", filename);
                    if (File.Exists(exeSamplesPath))
                    {
                        filename = exeSamplesPath;
                    }
                }
            }
        }

        if (!File.Exists(filename))
        {
            Console.WriteLine($"File not found: {filename}");
            return;
        }

        try
        {
            lines.Clear();
            var content = File.ReadAllLines(filename);
            int autoLineNum = 10;
            bool hasLineNumbers = false;

            // First pass: check if file has line numbers
            foreach (var line in content)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                int i = 0;
                while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;

                if (i > 0 && int.TryParse(trimmed[..i], out _))
                {
                    hasLineNumbers = true;
                    break;
                }
            }

            // Second pass: load lines
            foreach (var line in content)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (hasLineNumbers)
                {
                    // Parse line number
                    int i = 0;
                    while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;

                    if (i > 0 && int.TryParse(trimmed[..i], out int lineNum))
                    {
                        lines[lineNum] = trimmed[i..].TrimStart();
                    }
                }
                else
                {
                    // QBasic-style: auto-assign line numbers
                    lines[autoLineNum] = trimmed;
                    autoLineNum += 10;
                }
            }

            Console.WriteLine($"Loaded {lines.Count} lines from {filename}" +
                (hasLineNumbers ? "" : " (QBasic format, auto-numbered)"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
        }
    }

    private static void HandleSaveCommand(string command, SortedDictionary<int, string> lines)
    {
        // Parse: SAVE "filename" or SAVE filename
        var match = System.Text.RegularExpressions.Regex.Match(
            command, @"^SAVE\s+""?([^""]+)""?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Usage: SAVE \"filename.bas\"");
            return;
        }

        var filename = match.Groups[1].Value.Trim();

        // Add .bas extension if not present
        if (!filename.EndsWith(".bas", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".bas";
        }

        try
        {
            using var writer = new StreamWriter(filename);
            foreach (var kvp in lines)
            {
                writer.WriteLine($"{kvp.Key} {kvp.Value}");
            }

            Console.WriteLine($"Saved {lines.Count} lines to {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving file: {ex.Message}");
        }
    }

    // Shell commands that can be executed directly
    private static readonly HashSet<string> ShellCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "DIR", "CD", "CHDIR", "MD", "MKDIR", "RD", "RMDIR",
        "DEL", "ERASE", "COPY", "MOVE", "REN", "RENAME",
        "TYPE", "CLS", "DATE", "TIME", "VER", "VOL",
        "TREE", "ATTRIB", "FIND", "MORE", "SORT",
        "PATH", "SET", "ECHO", "TITLE", "COLOR",
        "START", "TASKLIST", "HOSTNAME", "WHOAMI", "PWD"
    };

    private static bool TryExecuteShellCommand(string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var command = parts[0].ToUpperInvariant();

        // Special handling for CD (needs to change current directory)
        if (command == "CD" || command == "CHDIR")
        {
            if (parts.Length == 1)
            {
                // Just CD - show current directory
                Console.WriteLine(Directory.GetCurrentDirectory());
            }
            else
            {
                var path = parts[1].Trim('"');
                try
                {
                    // Handle special cases
                    if (path == "..")
                    {
                        var parent = Directory.GetParent(Directory.GetCurrentDirectory());
                        if (parent != null)
                            Directory.SetCurrentDirectory(parent.FullName);
                    }
                    else if (path == "~" || path == "%USERPROFILE%")
                    {
                        Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    }
                    else
                    {
                        Directory.SetCurrentDirectory(Path.GetFullPath(path));
                    }
                    Console.WriteLine(Directory.GetCurrentDirectory());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            return true;
        }

        // PWD - print working directory (Unix-style alias)
        if (command == "PWD")
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            return true;
        }

        // Check if it's a known shell command
        if (!ShellCommands.Contains(command))
        {
            return false;
        }

        // Execute via cmd.exe
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {input}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    Console.Write(output);
                if (!string.IsNullOrEmpty(error))
                    Console.Write(error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
        }

        return true;
    }

    private static void RunProgram(SortedDictionary<int, string> lines, BasicInterpreter interpreter)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var source = string.Join("\n", lines.Select(kvp => $"{kvp.Key} {kvp.Value}"));

        cts = new CancellationTokenSource();
        try
        {
            interpreter.Execute(source, cts.Token);
        }
        catch (Exception ex) when (ex is LexerException or ParserException or RuntimeException)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            cts.Dispose();
            cts = null;
        }
    }

    private static void WaitForClose(SoftwareRenderer renderer)
    {
        // If using separate window, wait for it to close
        if (displayMode == WindowsDisplayMode.SeparateWindow && displayOutput != null)
        {
            Console.WriteLine("Close the graphics window to continue...");

            // Wait for window close
            while (displayOutput.IsValid && displayOutput.ProcessEvents())
            {
                // Check for key press if console is available
                try
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        break;
                    }
                }
                catch
                {
                    // No interactive console available
                }

                Thread.Sleep(50);
            }
        }
    }

    private static void ListProgram(SortedDictionary<int, string> lines)
    {
        foreach (var kvp in lines)
        {
            Console.WriteLine($"{kvp.Key} {kvp.Value}");
        }
    }

    private static bool TryParseLineNumber(string input, out int lineNumber, out string? rest)
    {
        lineNumber = 0;
        rest = null;

        int i = 0;
        while (i < input.Length && char.IsDigit(input[i]))
        {
            i++;
        }

        if (i == 0)
        {
            return false;
        }

        if (!int.TryParse(input[..i], out lineNumber))
        {
            return false;
        }

        rest = input[i..].TrimStart();
        return true;
    }
}
