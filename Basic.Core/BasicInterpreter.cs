using Basic.Core.Ast;

namespace Basic.Core;

public sealed class BasicInterpreter
{
    private readonly IConsole console;
    private readonly IGraphics graphics;
    private BasicProgram? program;
    private string? currentFileName;
    private Interpreter? interpreter;
    private CancellationToken cancellationToken;

    public BasicInterpreter(IConsole console) : this(console, new NullGraphics())
    {
    }

    public BasicInterpreter(IConsole console, IGraphics graphics)
    {
        this.console = console;
        this.graphics = graphics;
    }

    /// <summary>
    /// Returns true if the program is currently running (for chunk-based execution).
    /// </summary>
    public bool IsRunning => interpreter?.IsRunning ?? false;

    /// <summary>
    /// Gets the currently loaded program.
    /// </summary>
    public BasicProgram? Program => program;

    /// <summary>
    /// Gets the current file name if loaded from file.
    /// </summary>
    public string? CurrentFileName => currentFileName;

    /// <summary>
    /// Loads a program from source code.
    /// </summary>
    public void Load(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new Parser(tokens, source);
        program = parser.Parse();
    }

    /// <summary>
    /// Loads a program from a .bas file.
    /// </summary>
    public void LoadFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new RuntimeException($"File not found: {fileName}");
        }

        var source = File.ReadAllText(fileName);
        Load(source);
        currentFileName = fileName;
    }

    /// <summary>
    /// Saves the current program to a .bas file.
    /// </summary>
    public void SaveFile(string? fileName = null)
    {
        if (program == null)
        {
            throw new RuntimeException("No program to save");
        }

        fileName ??= currentFileName;
        if (string.IsNullOrEmpty(fileName))
        {
            throw new RuntimeException("No file name specified");
        }

        // Ensure .bas extension
        if (!fileName.EndsWith(".bas", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".bas";
        }

        var source = ListProgram();
        File.WriteAllText(fileName, source);
        currentFileName = fileName;
    }

    /// <summary>
    /// Returns the program listing as a string.
    /// </summary>
    public string ListProgram()
    {
        if (program == null)
        {
            return "";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var line in program.Lines)
        {
            sb.AppendLine($"{line.LineNumber} {FormatStatement(line.Statement)}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Clears the current program (NEW command).
    /// </summary>
    public void New()
    {
        program = null;
        currentFileName = null;
    }

    /// <summary>
    /// Runs the currently loaded program.
    /// </summary>
    public void Run(CancellationToken cancellationToken = default)
    {
        if (program == null)
        {
            throw new RuntimeException("No program loaded");
        }

        // Reset graphics state before running new program
        graphics.Reset();

        var interpreter = new Interpreter(console, graphics);
        interpreter.Run(program, cancellationToken);
    }

    /// <summary>
    /// Runs the currently loaded program asynchronously with periodic yields.
    /// This is needed for single-threaded environments like Blazor WebAssembly.
    /// </summary>
    public async Task RunAsync(Func<Task> yieldCallback, CancellationToken cancellationToken = default)
    {
        if (program == null)
        {
            throw new RuntimeException("No program loaded");
        }

        var interpreter = new Interpreter(console, graphics);
        interpreter.SetYieldCallback(yieldCallback);
        await interpreter.RunAsync(program, cancellationToken);
    }

    /// <summary>
    /// Loads and runs a program from source code asynchronously.
    /// </summary>
    public async Task ExecuteAsync(string source, Func<Task> yieldCallback, CancellationToken cancellationToken = default)
    {
        Load(source);
        await RunAsync(yieldCallback, cancellationToken);
    }

    /// <summary>
    /// Loads and runs a program from source code.
    /// </summary>
    public void Execute(string source, CancellationToken cancellationToken = default)
    {
        Load(source);
        Run(cancellationToken);
    }

    /// <summary>
    /// Initializes the interpreter for chunk-based execution.
    /// Call ExecuteChunk() repeatedly after this to run the program.
    /// This is designed for frame-based execution in browsers.
    /// </summary>
    public void InitializeExecution(CancellationToken cancellationToken = default)
    {
        if (program == null)
        {
            throw new RuntimeException("No program loaded");
        }

        // Reset graphics state before running new program
        graphics.Reset();

        this.cancellationToken = cancellationToken;
        interpreter = new Interpreter(console, graphics);
        interpreter.Initialize(program, cancellationToken);
    }

    /// <summary>
    /// Executes a chunk of statements and returns immediately.
    /// Returns true if there are more statements to execute, false if program ended.
    /// This is designed for frame-based execution in browsers (requestAnimationFrame pattern).
    /// </summary>
    public bool ExecuteChunk(int maxStatements = 1000)
    {
        if (interpreter == null)
        {
            return false;
        }

        try
        {
            return interpreter.ExecuteChunk(maxStatements);
        }
        catch (Exception)
        {
            // Let caller handle the exception
            throw;
        }
    }

    /// <summary>
    /// Stops the current execution and resets the interpreter.
    /// </summary>
    public void StopExecution()
    {
        interpreter?.Reset();
        interpreter = null;
    }

    /// <summary>
    /// Loads and runs a program from a file.
    /// </summary>
    public void RunFile(string fileName, CancellationToken cancellationToken = default)
    {
        LoadFile(fileName);
        Run(cancellationToken);
    }

    private static string FormatStatement(IStatement statement)
    {
        return statement switch
        {
            PrintStatement ps => FormatPrint(ps),
            LetStatement ls => FormatLet(ls),
            RemStatement rs => $"REM{rs.Comment}",
            GotoStatement gs => $"GOTO {gs.TargetLine}",
            IfStatement ifs => FormatIf(ifs),
            ForStatement fs => FormatFor(fs),
            NextStatement ns => ns.Variable != null ? $"NEXT {ns.Variable.Lexeme}" : "NEXT",
            WhileStatement ws => $"WHILE {FormatExpression(ws.Condition)}",
            WendStatement => "WEND",
            GosubStatement gss => $"GOSUB {gss.TargetLine}",
            ReturnStatement => "RETURN",
            EndStatement => "END",
            InputStatement ins => FormatInput(ins),
            DimStatement ds => FormatDim(ds),
            DataStatement das => $"DATA {das.RawData}",
            ReadStatement rs => $"READ {string.Join(", ", rs.Targets.Select(t => t.IsArray ? $"{t.Name.Lexeme}(...)" : t.Name.Lexeme))}",
            RestoreStatement res => res.TargetLine.HasValue ? $"RESTORE {res.TargetLine}" : "RESTORE",
            OnGotoStatement ogs => $"ON {FormatExpression(ogs.Selector)} {(ogs.IsGosub ? "GOSUB" : "GOTO")} {string.Join(", ", ogs.Targets)}",
            SwapStatement ss => $"SWAP {ss.First.Lexeme}, {ss.Second.Lexeme}",
            ClsStatement => "CLS",
            ScreenStatement scr => $"SCREEN {FormatExpression(scr.Mode)}",
            BeepStatement => "BEEP",
            OpenStatement os => FormatOpen(os),
            CloseStatement cs => FormatClose(cs),
            _ => statement.GetType().Name.Replace("Statement", "").ToUpperInvariant()
        };
    }

    private static string FormatPrint(PrintStatement ps)
    {
        if (ps.Expressions.Count == 0) return "PRINT";
        return $"PRINT {string.Join("; ", ps.Expressions.Select(FormatExpression))}";
    }

    private static string FormatLet(LetStatement ls)
    {
        var name = ls.Name.Lexeme;
        if (ls.Indices != null && ls.Indices.Count > 0)
        {
            name += $"({string.Join(", ", ls.Indices.Select(FormatExpression))})";
        }
        return $"LET {name} = {FormatExpression(ls.Value)}";
    }

    private static string FormatIf(IfStatement ifs)
    {
        var result = $"IF {FormatExpression(ifs.Condition)} THEN {FormatStatement(ifs.ThenBranch)}";
        if (ifs.ElseBranch != null)
        {
            result += $" ELSE {FormatStatement(ifs.ElseBranch)}";
        }
        return result;
    }

    private static string FormatFor(ForStatement fs)
    {
        var result = $"FOR {fs.Variable.Lexeme} = {FormatExpression(fs.Start)} TO {FormatExpression(fs.End)}";
        if (fs.Step != null)
        {
            result += $" STEP {FormatExpression(fs.Step)}";
        }
        return result;
    }

    private static string FormatInput(InputStatement ins)
    {
        var result = "INPUT ";
        if (ins.Prompt != null)
        {
            result += $"\"{ins.Prompt}\"; ";
        }
        result += string.Join(", ", ins.Variables.Select(v => v.Lexeme));
        return result;
    }

    private static string FormatDim(DimStatement ds)
    {
        var decls = ds.Declarations.Select(d =>
            $"{d.Name.Lexeme}({string.Join(", ", d.Dimensions.Select(FormatExpression))})");
        return $"DIM {string.Join(", ", decls)}";
    }

    private static string FormatOpen(OpenStatement os)
    {
        var mode = os.Mode switch
        {
            Ast.FileMode.Input => "INPUT",
            Ast.FileMode.Output => "OUTPUT",
            Ast.FileMode.Append => "APPEND",
            _ => "INPUT"
        };
        return $"OPEN {FormatExpression(os.FileName)} FOR {mode} AS #{FormatExpression(os.FileNumber)}";
    }

    private static string FormatClose(CloseStatement cs)
    {
        if (cs.FileNumbers == null || cs.FileNumbers.Count == 0)
        {
            return "CLOSE";
        }
        return $"CLOSE #{string.Join(", #", cs.FileNumbers.Select(FormatExpression))}";
    }

    private static string FormatExpression(IExpression expr)
    {
        return expr switch
        {
            LiteralExpression le => le.Value is string s ? $"\"{s}\"" : le.Value?.ToString() ?? "0",
            VariableExpression ve => ve.Name.Lexeme,
            BinaryExpression be => $"{FormatExpression(be.Left)} {be.Operator.Lexeme} {FormatExpression(be.Right)}",
            UnaryExpression ue => $"{ue.Operator.Lexeme}{FormatExpression(ue.Right)}",
            GroupingExpression ge => $"({FormatExpression(ge.Expression)})",
            CallExpression ce => $"{ce.Name}({string.Join(", ", ce.Arguments.Select(FormatExpression))})",
            ArrayAccessExpression ae => $"{ae.Name.Lexeme}({string.Join(", ", ae.Indices.Select(FormatExpression))})",
            _ => expr.ToString() ?? ""
        };
    }
}
