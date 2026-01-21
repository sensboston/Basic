using Basic.Core.Ast;

namespace Basic.Core;

public sealed class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    private readonly IConsole console;
    private readonly IGraphics graphics;
    private readonly Dictionary<string, object?> variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BasicArray> arrays = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<int> gosubStack = new();
    private readonly Stack<ForLoopState> forStack = new();
    private readonly Stack<WhileLoopState> whileStack = new();
    private readonly List<object> dataValues = [];
    private int dataPointer;
    private Random random = new();

    // Graphics state
    private int lastX;
    private int lastY;

    // File I/O state
    private readonly Dictionary<int, FileHandle> files = new();

    // Error handling state
    private int lastError;
    private int lastErrorLine;
    private int errorHandler = -1;
    private bool inErrorHandler;
    private int resumeLine = -1;

    // Debug/trace state
    private bool traceOn;

    // Width settings
    private int screenWidth = 80;

    // User-defined functions
    private readonly Dictionary<string, UserFunction> userFunctions = new(StringComparer.OrdinalIgnoreCase);

    // QBasic-style features
    private readonly Dictionary<string, object?> constants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<object?> selectCaseStack = new();
    private readonly Stack<bool> selectCaseMatched = new();
    private readonly Stack<DoLoopState> doLoopStack = new();
    private readonly Dictionary<string, SubInfo> subs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FunctionInfo> functions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<int> subReturnStack = new(); // Return PC after SUB call
    private readonly Dictionary<string, int> labels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UserType> types = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<char, BasicVarType> defTypes = new();
    private int defSeg = 0; // Current DEF SEG segment
    private byte[] memory = new byte[65536]; // Simulated memory for PEEK/POKE
    private int viewPrintTop = 1;
    private int viewPrintBottom = 25;

    private BasicProgram? program;
    private Dictionary<int, int>? lineIndex;
    private int pc;
    private int jumpTarget = -1;
    private bool endProgram;

    // Cancellation support for Ctrl+C
    private CancellationToken cancellationToken;

    // Async yield support for single-threaded environments (Blazor WASM)
    private Func<Task>? yieldCallback;
    private int yieldCounter;
    private const int YieldInterval = 2000; // Yield every N statements

    // Frame-based execution state
    private bool initialized;

    /// <summary>
    /// Returns true if the program is still running (not ended and has more statements).
    /// </summary>
    public bool IsRunning => initialized && !endProgram && program != null && pc < program.Lines.Count;

    /// <summary>
    /// Sets a callback that will be called periodically to yield control.
    /// Used in single-threaded environments like Blazor WebAssembly.
    /// </summary>
    public void SetYieldCallback(Func<Task> callback)
    {
        yieldCallback = callback;
    }

    /// <summary>
    /// Initializes the interpreter with a program for chunk-based execution.
    /// Call ExecuteChunk() repeatedly after this to run the program.
    /// </summary>
    public void Initialize(BasicProgram program, CancellationToken cancellationToken = default)
    {
        this.program = program;
        lineIndex = BuildLineIndex(program);
        pc = 0;
        endProgram = false;
        dataPointer = 0;
        this.cancellationToken = cancellationToken;
        yieldCounter = 0;
        initialized = true;

        // Collect all DATA statements
        CollectDataStatements(program);

        // Pre-register all SUBs and FUNCTIONs so they can be called from anywhere
        RegisterSubsAndFunctions(program);
    }

    /// <summary>
    /// Executes a chunk of statements and returns.
    /// Returns true if there are more statements to execute, false if program ended.
    /// This is designed for frame-based execution in browsers (requestAnimationFrame pattern).
    /// </summary>
    public bool ExecuteChunk(int maxStatements = 1000)
    {
        if (!initialized || program == null || lineIndex == null)
        {
            return false;
        }

        int executed = 0;
        while (pc < program.Lines.Count && !endProgram && executed < maxStatements)
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                console.WriteLine();
                console.WriteLine("Break");
                endProgram = true;
                return false;
            }

            var line = program.Lines[pc];
            jumpTarget = -1;

            if (traceOn)
            {
                console.Write($"[{line.LineNumber}]");
            }

            Execute(line.Statement);

            if (jumpTarget >= 0)
            {
                if (!lineIndex.TryGetValue(jumpTarget, out int targetIndex))
                {
                    throw new RuntimeException($"Undefined line number {jumpTarget}");
                }
                pc = targetIndex;
            }
            else
            {
                pc++;
            }

            executed++;
        }

        return pc < program.Lines.Count && !endProgram;
    }

    /// <summary>
    /// Resets the interpreter state for a new run.
    /// </summary>
    public void Reset()
    {
        initialized = false;
        endProgram = false;
        pc = 0;
        variables.Clear();
        arrays.Clear();
        gosubStack.Clear();
        forStack.Clear();
        whileStack.Clear();
        dataValues.Clear();
        dataPointer = 0;
        lastError = 0;
        lastErrorLine = 0;
        errorHandler = -1;
        inErrorHandler = false;
        resumeLine = -1;
        traceOn = false;
        userFunctions.Clear();
        constants.Clear();
        selectCaseStack.Clear();
        selectCaseMatched.Clear();
        doLoopStack.Clear();
        subs.Clear();
        functions.Clear();
        subReturnStack.Clear();
        labels.Clear();
        foreach (var file in files.Values)
        {
            file.Close();
        }
        files.Clear();
    }

    public Interpreter(IConsole console) : this(console, new NullGraphics())
    {
    }

    public Interpreter(IConsole console, IGraphics graphics)
    {
        this.console = console;
        this.graphics = graphics;
    }

    public void Run(BasicProgram program, CancellationToken cancellationToken = default)
    {
        this.program = program;
        lineIndex = BuildLineIndex(program);
        pc = 0;
        endProgram = false;
        dataPointer = 0;
        this.cancellationToken = cancellationToken;

        // Collect all DATA statements
        CollectDataStatements(program);

        // Pre-register all SUBs and FUNCTIONs so they can be called from anywhere
        RegisterSubsAndFunctions(program);

        while (pc < program.Lines.Count && !endProgram)
        {
            // Check for Ctrl+C cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                console.WriteLine();
                console.WriteLine("Break");
                return;
            }

            var line = program.Lines[pc];
            jumpTarget = -1;

            if (traceOn)
            {
                console.Write($"[{line.LineNumber}]");
            }

            Execute(line.Statement);

            if (jumpTarget >= 0)
            {
                if (!lineIndex.TryGetValue(jumpTarget, out int targetIndex))
                {
                    throw new RuntimeException($"Undefined line number {jumpTarget}");
                }
                pc = targetIndex;
            }
            else
            {
                pc++;
            }
        }
    }

    /// <summary>
    /// Runs the program asynchronously with periodic yields.
    /// This is needed for single-threaded environments like Blazor WebAssembly.
    /// </summary>
    public async Task RunAsync(BasicProgram program, CancellationToken cancellationToken = default)
    {
        this.program = program;
        lineIndex = BuildLineIndex(program);
        pc = 0;
        endProgram = false;
        dataPointer = 0;
        this.cancellationToken = cancellationToken;
        yieldCounter = 0;

        // Collect all DATA statements
        CollectDataStatements(program);

        // Pre-register all SUBs and FUNCTIONs so they can be called from anywhere
        RegisterSubsAndFunctions(program);

        while (pc < program.Lines.Count && !endProgram)
        {
            // Check for Ctrl+C cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                console.WriteLine();
                console.WriteLine("Break");
                return;
            }

            var line = program.Lines[pc];
            jumpTarget = -1;

            if (traceOn)
            {
                console.Write($"[{line.LineNumber}]");
            }

            Execute(line.Statement);

            if (jumpTarget >= 0)
            {
                if (!lineIndex.TryGetValue(jumpTarget, out int targetIndex))
                {
                    throw new RuntimeException($"Undefined line number {jumpTarget}");
                }
                pc = targetIndex;
            }
            else
            {
                pc++;
            }

            // Periodically yield control to allow UI updates
            yieldCounter++;
            if (yieldCallback != null && yieldCounter >= YieldInterval)
            {
                yieldCounter = 0;
                await yieldCallback();
            }
        }
    }

    private void CollectDataStatements(BasicProgram program)
    {
        dataValues.Clear();
        foreach (var line in program.Lines)
        {
            CollectDataFromStatement(line.Statement);
        }
    }

    private void RegisterSubsAndFunctions(BasicProgram program)
    {
        // Pre-scan to register all SUBs and FUNCTIONs so they can be called from main code
        subs.Clear();
        functions.Clear();

        for (int i = 0; i < program.Lines.Count; i++)
        {
            var stmt = program.Lines[i].Statement;

            // Handle compound statements (multiple statements on one line)
            if (stmt is CompoundStatement compound)
            {
                stmt = compound.Statements[0];
            }

            if (stmt is SubStatement sub)
            {
                subs[sub.Name.Lexeme] = new SubInfo(sub, i);
            }
            else if (stmt is FunctionStatement func)
            {
                functions[func.Name.Lexeme] = new FunctionInfo(func, i);
            }
        }
    }

    private void CollectDataFromStatement(IStatement statement)
    {
        if (statement is DataStatement data)
        {
            ParseDataValues(data.RawData);
        }
        else if (statement is CompoundStatement compound)
        {
            foreach (var stmt in compound.Statements)
            {
                CollectDataFromStatement(stmt);
            }
        }
    }

    private void ParseDataValues(string rawData)
    {
        var parts = rawData.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            {
                dataValues.Add(trimmed[1..^1]);
            }
            else if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                dataValues.Add(num);
            }
            else
            {
                dataValues.Add(trimmed);
            }
        }
    }

    private static Dictionary<int, int> BuildLineIndex(BasicProgram program)
    {
        var index = new Dictionary<int, int>();
        for (int i = 0; i < program.Lines.Count; i++)
        {
            index[program.Lines[i].LineNumber] = i;
        }
        return index;
    }

    private void Execute(IStatement statement)
    {
        statement.Accept(this);
    }

    private object? Evaluate(IExpression expression)
    {
        return expression.Accept(this);
    }

    public object? VisitPrintStatement(PrintStatement stmt)
    {
        if (stmt.Expressions.Count == 0)
        {
            if (graphics.IsGraphicsMode)
            {
                graphics.PrintText("\n");
            }
            else
            {
                console.WriteLine();
            }
            return null;
        }

        var values = new List<string>();
        foreach (var expr in stmt.Expressions)
        {
            var value = Evaluate(expr);
            values.Add(Stringify(value));
        }

        var text = string.Join("", values);
        if (graphics.IsGraphicsMode)
        {
            graphics.PrintText(text + "\n");
        }
        else
        {
            console.WriteLine(text);
        }
        return null;
    }

    public object? VisitLetStatement(LetStatement stmt)
    {
        var value = Evaluate(stmt.Value);

        if (stmt.Indices != null && stmt.Indices.Count > 0)
        {
            // Array assignment
            var indices = stmt.Indices.Select(i => (int)ToDouble(Evaluate(i))).ToArray();
            SetArrayElement(stmt.Name.Lexeme, indices, value);
        }
        else
        {
            variables[stmt.Name.Lexeme] = value;
        }

        return null;
    }

    public object? VisitFieldAssignStatement(FieldAssignStatement stmt)
    {
        var value = Evaluate(stmt.Value);
        SetFieldValue(stmt.Target, value);
        return null;
    }

    private void SetFieldValue(IExpression target, object? value)
    {
        if (target is FieldAccessExpression fieldExpr)
        {
            var obj = Evaluate(fieldExpr.Object);
            if (obj is TypeInstance instance)
            {
                var fieldName = fieldExpr.FieldName.Lexeme;
                if (instance.Fields.ContainsKey(fieldName))
                {
                    instance.Fields[fieldName] = value;
                }
                else
                {
                    throw new RuntimeException($"Unknown field '{fieldName}' in type '{instance.Type.Name}'");
                }
            }
            else
            {
                throw new RuntimeException("Cannot access field on non-type value");
            }
        }
        else
        {
            throw new RuntimeException("Invalid assignment target");
        }
    }

    public object? VisitRemStatement(RemStatement stmt) => null;

    public object? VisitGotoStatement(GotoStatement stmt)
    {
        jumpTarget = stmt.TargetLine;
        return null;
    }

    public object? VisitIfStatement(IfStatement stmt)
    {
        // Check for block-IF
        if (stmt.ThenBranch is BlockIfPlaceholder)
        {
            return ExecuteBlockIf(stmt.Condition);
        }

        var condition = Evaluate(stmt.Condition);

        if (IsTruthy(condition))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch != null)
        {
            Execute(stmt.ElseBranch);
        }

        return null;
    }

    private object? ExecuteBlockIf(IExpression condition)
    {
        var conditionResult = Evaluate(condition);

        if (IsTruthy(conditionResult))
        {
            // Execute statements until ELSE, ELSEIF, or END IF
            while (pc < program!.Lines.Count - 1)
            {
                pc++;
                var nextStmt = GetStatementType(program.Lines[pc].Statement);

                if (nextStmt is EndIfStatement)
                {
                    return null; // Done
                }
                if (nextStmt is ElseStatement || nextStmt is ElseIfStatement)
                {
                    // Skip to END IF
                    SkipToEndIf();
                    return null;
                }

                Execute(program.Lines[pc].Statement);
                if (jumpTarget >= 0) return null;
            }
        }
        else
        {
            // Condition false - skip to ELSE, ELSEIF, or END IF
            while (pc < program!.Lines.Count - 1)
            {
                pc++;
                var nextStmt = GetStatementType(program.Lines[pc].Statement);

                if (nextStmt is EndIfStatement)
                {
                    return null; // No ELSE branch, done
                }
                if (nextStmt is ElseStatement)
                {
                    // Execute ELSE branch
                    while (pc < program.Lines.Count - 1)
                    {
                        pc++;
                        nextStmt = GetStatementType(program.Lines[pc].Statement);
                        if (nextStmt is EndIfStatement)
                        {
                            return null;
                        }
                        Execute(program.Lines[pc].Statement);
                        if (jumpTarget >= 0) return null;
                    }
                    return null;
                }
                if (nextStmt is ElseIfStatement elseIfStmt)
                {
                    // Evaluate ELSEIF condition
                    return ExecuteBlockIf(elseIfStmt.Condition);
                }
            }
        }

        return null;
    }

    private void SkipToEndIf()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1 && depth > 0)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);

            if (nextStmt is IfStatement ifStmt && ifStmt.ThenBranch is BlockIfPlaceholder)
            {
                depth++;
            }
            else if (nextStmt is EndIfStatement)
            {
                depth--;
            }
        }
    }

    public object? VisitBlockIfPlaceholder(BlockIfPlaceholder stmt)
    {
        // Should not be called directly - handled by VisitIfStatement
        return null;
    }

    public object? VisitEndIfStatement(EndIfStatement stmt)
    {
        // END IF - just a marker, handled by block-IF execution
        return null;
    }

    public object? VisitElseIfStatement(ElseIfStatement stmt)
    {
        // ELSEIF - handled by block-IF execution
        return null;
    }

    public object? VisitElseStatement(ElseStatement stmt)
    {
        // ELSE - handled by block-IF execution
        return null;
    }

    public object? VisitForStatement(ForStatement stmt)
    {
        var startVal = ToDouble(Evaluate(stmt.Start));
        var endVal = ToDouble(Evaluate(stmt.End));
        var stepVal = stmt.Step != null ? ToDouble(Evaluate(stmt.Step)) : 1.0;

        variables[stmt.Variable.Lexeme] = startVal;

        // Store FOR loop state
        forStack.Push(new ForLoopState(
            stmt.Variable.Lexeme,
            endVal,
            stepVal,
            pc,
            program!.Lines[pc].LineNumber
        ));

        return null;
    }

    public object? VisitNextStatement(NextStatement stmt)
    {
        if (forStack.Count == 0)
        {
            throw new RuntimeException("NEXT without FOR");
        }

        var state = forStack.Peek();

        // Check variable name if specified
        if (stmt.Variable != null && !string.Equals(stmt.Variable.Lexeme, state.Variable, StringComparison.OrdinalIgnoreCase))
        {
            throw new RuntimeException($"NEXT {stmt.Variable.Lexeme} doesn't match FOR {state.Variable}");
        }

        // Increment counter
        var currentVal = ToDouble(variables[state.Variable]);
        currentVal += state.Step;
        variables[state.Variable] = currentVal;

        // Check if loop should continue
        bool continueLoop;
        if (state.Step > 0)
        {
            continueLoop = currentVal <= state.EndValue;
        }
        else
        {
            continueLoop = currentVal >= state.EndValue;
        }

        if (continueLoop)
        {
            // Jump back to line after FOR
            jumpTarget = program!.Lines[state.LoopStartIndex + 1].LineNumber;
        }
        else
        {
            forStack.Pop();
        }

        return null;
    }

    public object? VisitWhileStatement(WhileStatement stmt)
    {
        var condition = Evaluate(stmt.Condition);

        if (IsTruthy(condition))
        {
            whileStack.Push(new WhileLoopState(pc, program!.Lines[pc].LineNumber));
        }
        else
        {
            // Check if WEND is on the same line (inline WHILE...WEND like "WHILE x: WEND")
            var currentLineStmt = program!.Lines[pc].Statement;
            if (currentLineStmt is CompoundStatement compound)
            {
                if (compound.Statements.Any(s => s is WendStatement))
                {
                    // WEND is on same line, nothing more to do - compound execution will handle it
                    return null;
                }
            }

            // Skip to matching WEND on a different line
            int depth = 1;
            while (pc < program.Lines.Count - 1 && depth > 0)
            {
                pc++;
                var nextStmt = program.Lines[pc].Statement;
                if (nextStmt is WhileStatement) depth++;
                else if (nextStmt is WendStatement) depth--;
            }
        }

        return null;
    }

    public object? VisitWendStatement(WendStatement stmt)
    {
        if (whileStack.Count == 0)
        {
            // Check if this is an inline WHILE...WEND where condition was false
            // In that case, the WHILE already handled skipping, so just return
            var currentLineStmt = program!.Lines[pc].Statement;
            if (currentLineStmt is CompoundStatement compound)
            {
                if (compound.Statements.Any(s => s is WhileStatement))
                {
                    // This is an inline WHILE...WEND, condition was false, just continue
                    return null;
                }
            }
            throw new RuntimeException("WEND without WHILE");
        }

        var state = whileStack.Pop();
        jumpTarget = state.LineNumber;

        return null;
    }

    public object? VisitGosubStatement(GosubStatement stmt)
    {
        gosubStack.Push(program!.Lines[pc].LineNumber);
        jumpTarget = stmt.TargetLine;
        return null;
    }

    public object? VisitReturnStatement(ReturnStatement stmt)
    {
        if (gosubStack.Count == 0)
        {
            throw new RuntimeException("RETURN without GOSUB");
        }

        var returnLine = gosubStack.Pop();
        // Find the next line after the GOSUB
        if (lineIndex!.TryGetValue(returnLine, out int returnIndex))
        {
            if (returnIndex + 1 < program!.Lines.Count)
            {
                jumpTarget = program.Lines[returnIndex + 1].LineNumber;
            }
            else
            {
                endProgram = true;
            }
        }

        return null;
    }

    public object? VisitEndStatement(EndStatement stmt)
    {
        // If we're in a SUB/FUNCTION call, return from it instead of ending
        if (subReturnStack.Count > 0)
        {
            ReturnFromSub();
            return null;
        }
        endProgram = true;
        return null;
    }

    public object? VisitInputStatement(InputStatement stmt)
    {
        if (stmt.Prompt != null)
        {
            console.Write(stmt.Prompt);
        }
        console.Write("? ");

        var input = console.ReadLine() ?? "";
        var parts = input.Split(',');

        for (int i = 0; i < stmt.Variables.Count; i++)
        {
            var varName = stmt.Variables[i].Lexeme;
            var value = i < parts.Length ? parts[i].Trim() : "";

            if (varName.EndsWith('$'))
            {
                variables[varName] = value;
            }
            else if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                variables[varName] = num;
            }
            else
            {
                variables[varName] = 0.0;
            }
        }

        return null;
    }

    public object? VisitDimStatement(DimStatement stmt)
    {
        foreach (var decl in stmt.Declarations)
        {
            var dims = decl.Dimensions.Select(d => (int)ToDouble(Evaluate(d)) + 1).ToArray();

            // Check if this is a typed array
            if (decl.AsType != null && types.TryGetValue(decl.AsType, out var userType))
            {
                // Create array of TypeInstance
                var array = new BasicArray(dims, false, userType);
                // Initialize all elements with TypeInstance
                InitializeTypedArray(array, userType, dims, new int[dims.Length], 0);
                arrays[decl.Name.Lexeme] = array;
            }
            else
            {
                arrays[decl.Name.Lexeme] = new BasicArray(dims, decl.Name.Lexeme.EndsWith('$'));
            }
        }
        return null;
    }

    private void InitializeTypedArray(BasicArray array, UserType type, int[] dims, int[] indices, int dim)
    {
        if (dim == dims.Length)
        {
            array.SetElement(indices, type.CreateInstance());
            return;
        }

        for (int i = 0; i < dims[dim]; i++)
        {
            indices[dim] = i;
            InitializeTypedArray(array, type, dims, indices, dim + 1);
        }
    }

    public object? VisitDataStatement(DataStatement stmt) => null;

    public object? VisitReadStatement(ReadStatement stmt)
    {
        foreach (var target in stmt.Targets)
        {
            if (dataPointer >= dataValues.Count)
            {
                throw new RuntimeException("Out of DATA");
            }

            var value = dataValues[dataPointer++];

            if (target.IsArray)
            {
                var indices = target.Indices!.Select(i => (int)ToDouble(Evaluate(i))).ToArray();
                SetArrayElement(target.Name.Lexeme, indices, value);
            }
            else
            {
                variables[target.Name.Lexeme] = value;
            }
        }
        return null;
    }

    public object? VisitRestoreStatement(RestoreStatement stmt)
    {
        dataPointer = 0;
        return null;
    }

    public object? VisitOnGotoStatement(OnGotoStatement stmt)
    {
        var index = (int)ToDouble(Evaluate(stmt.Selector));

        if (index < 1 || index > stmt.Targets.Count)
        {
            return null; // Out of range, continue to next line
        }

        var targetLine = stmt.Targets[index - 1];

        if (stmt.IsGosub)
        {
            gosubStack.Push(program!.Lines[pc].LineNumber);
        }

        jumpTarget = targetLine;
        return null;
    }

    public object? VisitSwapStatement(SwapStatement stmt)
    {
        var temp = variables.GetValueOrDefault(stmt.First.Lexeme);
        variables[stmt.First.Lexeme] = variables.GetValueOrDefault(stmt.Second.Lexeme);
        variables[stmt.Second.Lexeme] = temp;
        return null;
    }

    public object? VisitClsStatement(ClsStatement stmt)
    {
        console.Clear();
        graphics.ClearScreen();
        graphics.Render();
        return null;
    }

    public object? VisitScreenStatement(ScreenStatement stmt)
    {
        int mode = (int)ToDouble(Evaluate(stmt.Mode));
        int apage = stmt.ActivePage != null ? (int)ToDouble(Evaluate(stmt.ActivePage)) : graphics.ActivePage;
        int vpage = stmt.VisualPage != null ? (int)ToDouble(Evaluate(stmt.VisualPage)) : graphics.VisualPage;

        // Set screen mode with page parameters
        graphics.SetScreenMode(mode, apage, vpage);
        graphics.Render();
        return null;
    }

    public object? VisitPsetStatement(PsetStatement stmt)
    {
        int x = (int)ToDouble(Evaluate(stmt.X));
        int y = (int)ToDouble(Evaluate(stmt.Y));
        int color = stmt.Color != null ? (int)ToDouble(Evaluate(stmt.Color)) : graphics.ForegroundColor;

        if (stmt.IsPreset)
        {
            color = graphics.BackgroundColor;
        }

        graphics.SetPixel(x, y, color);
        graphics.Render();
        lastX = x;
        lastY = y;
        return null;
    }

    public object? VisitLineStatement(LineStatement stmt)
    {
        int x1 = stmt.X1 != null ? (int)ToDouble(Evaluate(stmt.X1)) : lastX;
        int y1 = stmt.Y1 != null ? (int)ToDouble(Evaluate(stmt.Y1)) : lastY;
        int x2 = (int)ToDouble(Evaluate(stmt.X2));
        int y2 = (int)ToDouble(Evaluate(stmt.Y2));
        int color = stmt.Color != null ? (int)ToDouble(Evaluate(stmt.Color)) : graphics.ForegroundColor;

        if (stmt.IsBox)
        {
            graphics.DrawBox(x1, y1, x2, y2, color, stmt.IsFilled);
        }
        else
        {
            graphics.DrawLine(x1, y1, x2, y2, color);
        }

        graphics.Render();
        lastX = x2;
        lastY = y2;
        return null;
    }

    public object? VisitCircleStatement(CircleStatement stmt)
    {
        int cx = (int)ToDouble(Evaluate(stmt.CX));
        int cy = (int)ToDouble(Evaluate(stmt.CY));
        int radius = (int)ToDouble(Evaluate(stmt.Radius));
        int color = stmt.Color != null ? (int)ToDouble(Evaluate(stmt.Color)) : graphics.ForegroundColor;
        double start = stmt.Start != null ? ToDouble(Evaluate(stmt.Start)) : 0;
        double end = stmt.End != null ? ToDouble(Evaluate(stmt.End)) : Math.PI * 2;
        double aspect = stmt.Aspect != null ? ToDouble(Evaluate(stmt.Aspect)) : 1.0;

        graphics.DrawCircle(cx, cy, radius, color, start, end, aspect);
        graphics.Render();
        lastX = cx;
        lastY = cy;
        return null;
    }

    public object? VisitPaintStatement(PaintStatement stmt)
    {
        int x = (int)ToDouble(Evaluate(stmt.X));
        int y = (int)ToDouble(Evaluate(stmt.Y));
        int fillColor = stmt.FillColor != null ? (int)ToDouble(Evaluate(stmt.FillColor)) : graphics.ForegroundColor;
        int borderColor = stmt.BorderColor != null ? (int)ToDouble(Evaluate(stmt.BorderColor)) : fillColor;

        graphics.Paint(x, y, fillColor, borderColor);
        graphics.Render();
        return null;
    }

    public object? VisitDrawStatement(DrawStatement stmt)
    {
        string commands = Stringify(Evaluate(stmt.Commands));
        ExecuteDrawCommands(commands);
        graphics.Render();
        return null;
    }

    private void ExecuteDrawCommands(string commands)
    {
        int x = lastX;
        int y = lastY;
        int color = graphics.ForegroundColor;
        bool penDown = true;
        int scale = 4; // Default scale factor

        int i = 0;
        while (i < commands.Length)
        {
            char cmd = char.ToUpperInvariant(commands[i]);
            i++;

            // Parse optional number
            int number = 1;
            int numStart = i;
            while (i < commands.Length && char.IsDigit(commands[i]))
            {
                i++;
            }
            if (i > numStart)
            {
                number = int.Parse(commands[numStart..i]);
            }

            int dx = 0, dy = 0;
            bool shouldMove = true;

            switch (cmd)
            {
                case 'U': dy = -number * scale; break;
                case 'D': dy = number * scale; break;
                case 'L': dx = -number * scale; break;
                case 'R': dx = number * scale; break;
                case 'E': dx = number * scale; dy = -number * scale; break;
                case 'F': dx = number * scale; dy = number * scale; break;
                case 'G': dx = -number * scale; dy = number * scale; break;
                case 'H': dx = -number * scale; dy = -number * scale; break;
                case 'M':
                    // Move to absolute or relative position
                    // Parse coordinates (simplified)
                    shouldMove = false;
                    break;
                case 'B':
                    penDown = false;
                    shouldMove = false;
                    break;
                case 'N':
                    shouldMove = false;
                    break;
                case 'C':
                    color = number;
                    shouldMove = false;
                    break;
                case 'S':
                    scale = number;
                    shouldMove = false;
                    break;
                default:
                    shouldMove = false;
                    break;
            }

            if (shouldMove)
            {
                int newX = x + dx;
                int newY = y + dy;
                if (penDown)
                {
                    graphics.DrawLine(x, y, newX, newY, color);
                }
                x = newX;
                y = newY;
                penDown = true;
            }
        }

        lastX = x;
        lastY = y;
    }

    public object? VisitColorStatement(ColorStatement stmt)
    {
        int foreground = stmt.Foreground != null ? (int)ToDouble(Evaluate(stmt.Foreground)) : graphics.ForegroundColor;
        int background = stmt.Background != null ? (int)ToDouble(Evaluate(stmt.Background)) : graphics.BackgroundColor;
        graphics.SetColor(foreground, background);
        return null;
    }

    public object? VisitLocateStatement(LocateStatement stmt)
    {
        int row = stmt.Row != null ? (int)ToDouble(Evaluate(stmt.Row)) : 1;
        int col = stmt.Col != null ? (int)ToDouble(Evaluate(stmt.Col)) : 1;
        graphics.Locate(row, col);
        return null;
    }

    public object? VisitBeepStatement(BeepStatement stmt)
    {
        graphics.Beep();
        return null;
    }

    // File I/O visitor methods

    public object? VisitOpenStatement(OpenStatement stmt)
    {
        var fileName = Stringify(Evaluate(stmt.FileName));
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));

        if (files.ContainsKey(fileNumber))
        {
            throw new RuntimeException($"File #{fileNumber} is already open");
        }

        FileHandle handle;
        switch (stmt.Mode)
        {
            case Ast.FileMode.Input:
                handle = new FileHandle(fileName, new StreamReader(fileName), null);
                break;
            case Ast.FileMode.Output:
                handle = new FileHandle(fileName, null, new StreamWriter(fileName, false));
                break;
            case Ast.FileMode.Append:
                handle = new FileHandle(fileName, null, new StreamWriter(fileName, true));
                break;
            case Ast.FileMode.Random:
                int recordLength = stmt.RecordLength != null
                    ? (int)ToDouble(Evaluate(stmt.RecordLength))
                    : 128;
                var stream = new FileStream(fileName, System.IO.FileMode.OpenOrCreate, FileAccess.ReadWrite);
                handle = new FileHandle(fileName, stream, recordLength);
                break;
            default:
                throw new RuntimeException($"Unknown file mode");
        }

        files[fileNumber] = handle;
        return null;
    }

    public object? VisitCloseStatement(CloseStatement stmt)
    {
        if (stmt.FileNumbers == null || stmt.FileNumbers.Count == 0)
        {
            // Close all files
            foreach (var handle in files.Values)
            {
                handle.Close();
            }
            files.Clear();
        }
        else
        {
            foreach (var expr in stmt.FileNumbers)
            {
                int fileNumber = (int)ToDouble(Evaluate(expr));
                if (files.TryGetValue(fileNumber, out var handle))
                {
                    handle.Close();
                    files.Remove(fileNumber);
                }
            }
        }
        return null;
    }

    public object? VisitPrintFileStatement(PrintFileStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle) || handle.Writer == null)
        {
            throw new RuntimeException($"File #{fileNumber} is not open for output");
        }

        var values = stmt.Expressions.Select(e => Stringify(Evaluate(e)));
        handle.Writer.WriteLine(string.Join("", values));
        return null;
    }

    public object? VisitInputFileStatement(InputFileStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle) || handle.Reader == null)
        {
            throw new RuntimeException($"File #{fileNumber} is not open for input");
        }

        var line = handle.Reader.ReadLine() ?? "";
        var parts = line.Split(',');

        for (int i = 0; i < stmt.Variables.Count; i++)
        {
            var varName = stmt.Variables[i].Lexeme;
            var value = i < parts.Length ? parts[i].Trim() : "";

            // Remove quotes if present
            if (value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            if (varName.EndsWith('$'))
            {
                variables[varName] = value;
            }
            else if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                variables[varName] = num;
            }
            else
            {
                variables[varName] = 0.0;
            }
        }

        return null;
    }

    public object? VisitLineInputFileStatement(LineInputFileStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle) || handle.Reader == null)
        {
            throw new RuntimeException($"File #{fileNumber} is not open for input");
        }

        var line = handle.Reader.ReadLine() ?? "";
        variables[stmt.Variable.Lexeme] = line;
        return null;
    }

    public object? VisitWriteFileStatement(WriteFileStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle) || handle.Writer == null)
        {
            throw new RuntimeException($"File #{fileNumber} is not open for output");
        }

        var values = new List<string>();
        foreach (var expr in stmt.Expressions)
        {
            var value = Evaluate(expr);
            if (value is string s)
            {
                values.Add($"\"{s}\"");
            }
            else
            {
                values.Add(Stringify(value));
            }
        }
        handle.Writer.WriteLine(string.Join(",", values));
        return null;
    }

    public object? VisitKillStatement(KillStatement stmt)
    {
        var fileName = Stringify(Evaluate(stmt.FileName));
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
        else
        {
            throw new RuntimeException($"File not found: {fileName}");
        }
        return null;
    }

    public object? VisitNameStatement(NameStatement stmt)
    {
        var oldName = Stringify(Evaluate(stmt.OldName));
        var newName = Stringify(Evaluate(stmt.NewName));
        File.Move(oldName, newName);
        return null;
    }

    public object? VisitFilesStatement(FilesStatement stmt)
    {
        var pattern = stmt.Pattern != null ? Stringify(Evaluate(stmt.Pattern)) : "*.*";
        var directory = Path.GetDirectoryName(pattern);
        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }
        var searchPattern = Path.GetFileName(pattern);
        if (string.IsNullOrEmpty(searchPattern))
        {
            searchPattern = "*.*";
        }

        try
        {
            var files = Directory.GetFiles(directory, searchPattern);
            foreach (var file in files)
            {
                console.WriteLine(Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            throw new RuntimeException($"Error listing files: {ex.Message}");
        }
        return null;
    }

    // Additional statement visitors

    public object? VisitRandomizeStatement(RandomizeStatement stmt)
    {
        if (stmt.Seed != null)
        {
            int seed = (int)ToDouble(Evaluate(stmt.Seed));
            random = new Random(seed);
        }
        else
        {
            random = new Random();
        }
        return null;
    }

    public object? VisitLineInputStatement(LineInputStatement stmt)
    {
        if (stmt.Prompt != null)
        {
            console.Write(stmt.Prompt);
        }

        var input = console.ReadLine() ?? "";
        variables[stmt.Variable.Lexeme] = input;
        return null;
    }

    public object? VisitDefFnStatement(DefFnStatement stmt)
    {
        var paramNames = stmt.Parameters.Select(p => p.Lexeme).ToList();
        userFunctions[stmt.Name.Lexeme] = new UserFunction(paramNames, stmt.Body);
        return null;
    }

    public object? VisitTronStatement(TronStatement stmt)
    {
        traceOn = true;
        return null;
    }

    public object? VisitTroffStatement(TroffStatement stmt)
    {
        traceOn = false;
        return null;
    }

    public object? VisitWidthStatement(WidthStatement stmt)
    {
        int width = (int)ToDouble(Evaluate(stmt.Width));
        screenWidth = width;
        graphics.SetWidth(width);
        return null;
    }

    public object? VisitSoundStatement(SoundStatement stmt)
    {
        int frequency = (int)ToDouble(Evaluate(stmt.Frequency));
        int duration = (int)ToDouble(Evaluate(stmt.Duration));
        graphics.Sound(frequency, duration);
        return null;
    }

    public object? VisitPlayStatement(PlayStatement stmt)
    {
        string commands = Stringify(Evaluate(stmt.Commands));
        graphics.Play(commands);
        return null;
    }

    public object? VisitOnErrorStatement(OnErrorStatement stmt)
    {
        if (stmt.TargetLine.HasValue)
        {
            errorHandler = stmt.TargetLine.Value;
        }
        else if (stmt.TargetLabel != null && labels.TryGetValue(stmt.TargetLabel, out int labelLine))
        {
            errorHandler = labelLine;
        }
        else
        {
            errorHandler = -1;
            inErrorHandler = false;
        }
        return null;
    }

    public object? VisitResumeStatement(ResumeStatement stmt)
    {
        if (!inErrorHandler)
        {
            throw new RuntimeException("RESUME without error");
        }

        inErrorHandler = false;

        switch (stmt.Type)
        {
            case ResumeType.Resume:
                jumpTarget = resumeLine;
                break;
            case ResumeType.ResumeNext:
                if (lineIndex!.TryGetValue(resumeLine, out int resumeIndex))
                {
                    if (resumeIndex + 1 < program!.Lines.Count)
                    {
                        jumpTarget = program.Lines[resumeIndex + 1].LineNumber;
                    }
                }
                break;
            case ResumeType.ResumeLine:
                jumpTarget = stmt.TargetLine ?? resumeLine;
                break;
        }

        return null;
    }

    public object? VisitErrorStatement(ErrorStatement stmt)
    {
        int errorCode = (int)ToDouble(Evaluate(stmt.ErrorCode));
        lastError = errorCode;
        lastErrorLine = program?.Lines[pc].LineNumber ?? 0;

        if (errorHandler > 0 && !inErrorHandler)
        {
            inErrorHandler = true;
            resumeLine = program?.Lines[pc].LineNumber ?? 0;
            jumpTarget = errorHandler;
        }
        else
        {
            throw new RuntimeException($"Error {errorCode} at line {lastErrorLine}");
        }

        return null;
    }

    public object? VisitPrintUsingStatement(PrintUsingStatement stmt)
    {
        var format = Stringify(Evaluate(stmt.Format));
        var output = new System.Text.StringBuilder();

        int exprIndex = 0;
        int formatIndex = 0;

        while (formatIndex < format.Length && exprIndex < stmt.Expressions.Count)
        {
            char c = format[formatIndex];

            if (c == '#' || c == '.' || c == '+' || c == '-' || c == '$' || c == '*')
            {
                // Numeric format - collect the format specifier
                var numFormat = new System.Text.StringBuilder();
                bool hasDecimal = false;
                bool hasDollar = c == '$';
                bool hasAsterisks = c == '*';
                bool hasPlus = c == '+';
                bool hasMinus = c == '-';

                while (formatIndex < format.Length)
                {
                    char fc = format[formatIndex];
                    if (fc == '#' || fc == ',' || (fc == '.' && !hasDecimal))
                    {
                        if (fc == '.') hasDecimal = true;
                        numFormat.Append(fc);
                        formatIndex++;
                    }
                    else if (fc == '$' && numFormat.Length == 0)
                    {
                        numFormat.Append(fc);
                        formatIndex++;
                    }
                    else if (fc == '*' && numFormat.Length <= 1)
                    {
                        numFormat.Append(fc);
                        formatIndex++;
                    }
                    else if (fc == '+' || fc == '-')
                    {
                        numFormat.Append(fc);
                        formatIndex++;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                // Format the number
                var value = ToDouble(Evaluate(stmt.Expressions[exprIndex++]));
                output.Append(FormatNumber(value, numFormat.ToString()));
            }
            else if (c == '!')
            {
                // Single character of string
                var value = Stringify(Evaluate(stmt.Expressions[exprIndex++]));
                output.Append(value.Length > 0 ? value[0] : ' ');
                formatIndex++;
            }
            else if (c == '&')
            {
                // Variable length string
                var value = Stringify(Evaluate(stmt.Expressions[exprIndex++]));
                output.Append(value);
                formatIndex++;
            }
            else if (c == '\\')
            {
                // Fixed-length string: \ spaces \
                int startPos = formatIndex;
                formatIndex++;
                int spaces = 0;
                while (formatIndex < format.Length && format[formatIndex] != '\\')
                {
                    spaces++;
                    formatIndex++;
                }
                if (formatIndex < format.Length) formatIndex++; // consume closing \
                int width = spaces + 2;
                var value = Stringify(Evaluate(stmt.Expressions[exprIndex++]));
                output.Append(value.PadRight(width).Substring(0, width));
            }
            else
            {
                output.Append(c);
                formatIndex++;
            }
        }

        // Append any remaining format characters
        while (formatIndex < format.Length)
        {
            output.Append(format[formatIndex++]);
        }

        // Output to console or file
        if (stmt.FileNumber != null)
        {
            int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
            if (files.TryGetValue(fileNumber, out var handle) && handle.Writer != null)
            {
                handle.Writer.WriteLine(output.ToString());
            }
        }
        else
        {
            console.WriteLine(output.ToString());
        }

        return null;
    }

    public object? VisitFieldStatement(FieldStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle))
        {
            throw new RuntimeException($"File #{fileNumber} not open");
        }

        handle.FieldMappings.Clear();
        int offset = 0;
        foreach (var field in stmt.Fields)
        {
            int width = (int)ToDouble(Evaluate(field.Width));
            handle.FieldMappings.Add((offset, width, field.Variable.Lexeme));
            offset += width;
        }

        return null;
    }

    public object? VisitGetRecordStatement(GetRecordStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle))
        {
            throw new RuntimeException($"File #{fileNumber} not open");
        }

        int recordNumber = stmt.RecordNumber != null
            ? (int)ToDouble(Evaluate(stmt.RecordNumber))
            : handle.CurrentRecord;

        if (handle.RandomStream != null)
        {
            // Position and read the record
            long position = (recordNumber - 1) * handle.RecordLength;
            handle.RandomStream.Seek(position, SeekOrigin.Begin);
            int bytesRead = handle.RandomStream.Read(handle.RecordBuffer, 0, handle.RecordLength);

            // Fill unread bytes with spaces
            for (int i = bytesRead; i < handle.RecordLength; i++)
            {
                handle.RecordBuffer[i] = (byte)' ';
            }

            // Update field variables from buffer
            foreach (var (offset, width, variable) in handle.FieldMappings)
            {
                string value = System.Text.Encoding.ASCII.GetString(handle.RecordBuffer, offset, width);
                variables[variable] = value;
            }

            handle.CurrentRecord = recordNumber + 1;
        }

        return null;
    }

    public object? VisitPutRecordStatement(PutRecordStatement stmt)
    {
        int fileNumber = (int)ToDouble(Evaluate(stmt.FileNumber));
        if (!files.TryGetValue(fileNumber, out var handle))
        {
            throw new RuntimeException($"File #{fileNumber} not open");
        }

        int recordNumber = stmt.RecordNumber != null
            ? (int)ToDouble(Evaluate(stmt.RecordNumber))
            : handle.CurrentRecord;

        if (handle.RandomStream != null)
        {
            // Position and write the record
            long position = (recordNumber - 1) * handle.RecordLength;
            handle.RandomStream.Seek(position, SeekOrigin.Begin);
            handle.RandomStream.Write(handle.RecordBuffer, 0, handle.RecordLength);
            handle.RandomStream.Flush();

            handle.CurrentRecord = recordNumber + 1;
        }

        return null;
    }

    public object? VisitLsetStatement(LsetStatement stmt)
    {
        string varName = stmt.Variable.Lexeme;
        string value = Stringify(Evaluate(stmt.Value));

        // Find the field width from any open file's field mappings
        int width = value.Length;
        foreach (var handle in files.Values)
        {
            var mapping = handle.FieldMappings.FirstOrDefault(m => m.variable.Equals(varName, StringComparison.OrdinalIgnoreCase));
            if (mapping.variable != null)
            {
                width = mapping.width;
                // Update the buffer
                int offset = mapping.offset;
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value.PadRight(width).Substring(0, width));
                Array.Copy(bytes, 0, handle.RecordBuffer, offset, width);
                break;
            }
        }

        // Left-justify in field
        variables[varName] = value.PadRight(width).Substring(0, width);
        return null;
    }

    public object? VisitRsetStatement(RsetStatement stmt)
    {
        string varName = stmt.Variable.Lexeme;
        string value = Stringify(Evaluate(stmt.Value));

        // Find the field width from any open file's field mappings
        int width = value.Length;
        foreach (var handle in files.Values)
        {
            var mapping = handle.FieldMappings.FirstOrDefault(m => m.variable.Equals(varName, StringComparison.OrdinalIgnoreCase));
            if (mapping.variable != null)
            {
                width = mapping.width;
                // Update the buffer
                int offset = mapping.offset;
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value.PadLeft(width).Substring(0, width));
                Array.Copy(bytes, 0, handle.RecordBuffer, offset, width);
                break;
            }
        }

        // Right-justify in field
        variables[varName] = value.PadLeft(width).Substring(0, width);
        return null;
    }

    public object? VisitCompoundStatement(CompoundStatement stmt)
    {
        foreach (var statement in stmt.Statements)
        {
            Execute(statement);
            // Stop if we hit a jump or end
            if (jumpTarget >= 0 || endProgram)
            {
                break;
            }
        }
        return null;
    }

    // QBasic-style statement visitors

    public object? VisitConstStatement(ConstStatement stmt)
    {
        foreach (var decl in stmt.Declarations)
        {
            var value = Evaluate(decl.Value);
            constants[decl.Name.Lexeme] = value;
        }
        return null;
    }

    public object? VisitSleepStatement(SleepStatement stmt)
    {
        if (stmt.Seconds != null)
        {
            int seconds = (int)ToDouble(Evaluate(stmt.Seconds));
            Thread.Sleep(seconds * 1000);
        }
        else
        {
            // SLEEP without argument waits for a key press
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Check both graphics and console for key press
                if (graphics.IsGraphicsMode)
                {
                    if (!string.IsNullOrEmpty(graphics.ReadKey()))
                        break;
                }
                else
                {
                    if (!string.IsNullOrEmpty(console.ReadKey()))
                        break;
                }
                Thread.Sleep(50);
            }
        }
        return null;
    }

    public object? VisitSelectCaseStatement(SelectCaseStatement stmt)
    {
        // Store the test expression value for CASE matching
        selectCaseStack.Push(Evaluate(stmt.TestExpression));
        return null;
    }

    public object? VisitCaseClauseStatement(CaseClauseStatement stmt)
    {
        if (selectCaseStack.Count == 0)
        {
            throw new RuntimeException("CASE without SELECT");
        }

        var testValue = selectCaseStack.Peek();

        // CASE ELSE matches everything
        if (stmt.Matches == null)
        {
            selectCaseMatched.Push(true);
            return null;
        }

        // Check if any match succeeds
        bool matched = false;
        foreach (var match in stmt.Matches)
        {
            if (match is CaseValueMatch valueMatch)
            {
                var value = Evaluate(valueMatch.Value);
                if (Compare(testValue, value) == 0)
                {
                    matched = true;
                    break;
                }
            }
            else if (match is CaseRangeMatch rangeMatch)
            {
                var from = Evaluate(rangeMatch.From);
                var to = Evaluate(rangeMatch.To);
                if (Compare(testValue, from) >= 0 && Compare(testValue, to) <= 0)
                {
                    matched = true;
                    break;
                }
            }
            else if (match is CaseComparisonMatch compMatch)
            {
                var value = Evaluate(compMatch.Value);
                var result = Compare(testValue, value);
                bool compResult = compMatch.Comparison switch
                {
                    TokenType.Less => result < 0,
                    TokenType.LessEqual => result <= 0,
                    TokenType.Greater => result > 0,
                    TokenType.GreaterEqual => result >= 0,
                    TokenType.Equal => result == 0,
                    TokenType.NotEqual => result != 0,
                    _ => false
                };
                if (compResult)
                {
                    matched = true;
                    break;
                }
            }
        }

        if (!matched)
        {
            // Skip to next CASE or END SELECT
            SkipToNextCase();
        }

        return null;
    }

    private void SkipToNextCase()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);
            if (nextStmt is SelectCaseStatement) depth++;
            else if (nextStmt is EndSelectStatement)
            {
                depth--;
                if (depth == 0)
                {
                    pc--; // Let normal flow hit END SELECT
                    break;
                }
            }
            else if (nextStmt is CaseClauseStatement && depth == 1)
            {
                pc--; // Let normal flow execute the CASE
                break;
            }
        }
    }

    private static IStatement GetStatementType(IStatement stmt)
    {
        if (stmt is CompoundStatement compound && compound.Statements.Count > 0)
            return compound.Statements[0];
        return stmt;
    }

    public object? VisitEndSelectStatement(EndSelectStatement stmt)
    {
        if (selectCaseStack.Count > 0)
        {
            selectCaseStack.Pop();
        }
        if (selectCaseMatched.Count > 0)
        {
            selectCaseMatched.Pop();
        }
        return null;
    }

    public object? VisitDoLoopStatement(DoLoopStatement stmt)
    {
        if (stmt.ConditionAtTop && stmt.Condition != null)
        {
            var condition = Evaluate(stmt.Condition);
            bool shouldContinue = stmt.IsDoWhile ? IsTruthy(condition) : !IsTruthy(condition);

            if (shouldContinue)
            {
                doLoopStack.Push(new DoLoopState(pc, program!.Lines[pc].LineNumber));
            }
            else
            {
                // Skip to matching LOOP
                SkipToMatchingLoop();
            }
        }
        else
        {
            // DO without condition at top - just push the state
            doLoopStack.Push(new DoLoopState(pc, program!.Lines[pc].LineNumber));
        }
        return null;
    }

    public object? VisitLoopStatement(LoopStatement stmt)
    {
        if (doLoopStack.Count == 0)
        {
            throw new RuntimeException("LOOP without DO");
        }

        var state = doLoopStack.Peek();

        if (stmt.Condition != null)
        {
            // LOOP WHILE/UNTIL
            var condition = Evaluate(stmt.Condition);
            bool shouldContinue = stmt.IsWhile ? IsTruthy(condition) : !IsTruthy(condition);

            if (shouldContinue)
            {
                jumpTarget = state.LineNumber;
            }
            else
            {
                doLoopStack.Pop();
            }
        }
        else
        {
            // Unconditional LOOP - check if DO had a condition
            // Jump back to DO
            jumpTarget = state.LineNumber;
        }

        return null;
    }

    private void SkipToMatchingLoop()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1 && depth > 0)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);
            if (nextStmt is DoLoopStatement) depth++;
            else if (nextStmt is LoopStatement) depth--;
        }
    }

    public object? VisitExitStatement(ExitStatement stmt)
    {
        switch (stmt.ExitType)
        {
            case ExitType.For:
                if (forStack.Count > 0)
                {
                    forStack.Pop();
                    SkipToMatchingNext();
                }
                break;
            case ExitType.While:
                if (whileStack.Count > 0)
                {
                    whileStack.Pop();
                    SkipToMatchingWend();
                }
                break;
            case ExitType.Do:
                if (doLoopStack.Count > 0)
                {
                    doLoopStack.Pop();
                    SkipToMatchingLoop();
                }
                break;
            case ExitType.Sub:
            case ExitType.Function:
                // Skip to END SUB/FUNCTION then return
                SkipToEndSubOrFunction();
                if (subReturnStack.Count > 0)
                {
                    ReturnFromSub();
                }
                break;
        }
        return null;
    }

    private void SkipToMatchingNext()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1 && depth > 0)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);
            if (nextStmt is ForStatement) depth++;
            else if (nextStmt is NextStatement) depth--;
        }
    }

    private void SkipToMatchingWend()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1 && depth > 0)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);
            if (nextStmt is WhileStatement) depth++;
            else if (nextStmt is WendStatement) depth--;
        }
    }

    public object? VisitDeclareStatement(DeclareStatement stmt)
    {
        // DECLARE is informational only - actual SUB/FUNCTION is defined later
        return null;
    }

    public object? VisitSubStatement(SubStatement stmt)
    {
        // SUB definition - register it for later calls with its start position
        int startPc = pc;
        subs[stmt.Name.Lexeme] = new SubInfo(stmt, startPc);

        // Skip past the SUB body (will be executed when called)
        SkipToEndSubOrFunction();
        return null;
    }

    public object? VisitFunctionStatement(FunctionStatement stmt)
    {
        // FUNCTION definition - register it for later calls
        int startPc = pc;
        functions[stmt.Name.Lexeme] = new FunctionInfo(stmt, startPc);

        // Skip past the FUNCTION body
        SkipToEndSubOrFunction();
        return null;
    }

    private void SkipToEndSubOrFunction()
    {
        int depth = 1;
        while (pc < program!.Lines.Count - 1)
        {
            pc++;
            var stmt = program.Lines[pc].Statement;
            var stmtToCheck = stmt is CompoundStatement compound ? compound.Statements[0] : stmt;

            if (stmtToCheck is SubStatement || stmtToCheck is FunctionStatement) depth++;
            else if (stmtToCheck is EndStatement) // END or END SUB/FUNCTION
            {
                depth--;
                if (depth == 0) return;
            }
        }
    }

    public object? VisitCallSubStatement(CallSubStatement stmt)
    {
        if (!subs.TryGetValue(stmt.Name, out var subInfo))
        {
            throw new RuntimeException($"Undefined SUB: {stmt.Name}");
        }

        // Set up parameters
        for (int i = 0; i < subInfo.Statement.Parameters.Count && i < stmt.Arguments.Count; i++)
        {
            var param = subInfo.Statement.Parameters[i];
            var paramName = param.Name.Lexeme;
            var argValue = Evaluate(stmt.Arguments[i]);

            // If parameter is an array (or argument is a BasicArray), bind to arrays
            if (param.IsArray || argValue is BasicArray)
            {
                if (argValue is BasicArray arr)
                {
                    arrays[paramName] = arr;
                }
            }
            else
            {
                variables[paramName] = argValue;
            }
        }

        // Save return address and jump to SUB
        subReturnStack.Push(pc);
        pc = subInfo.StartPc; // Will be incremented by main loop, then execute first SUB statement

        return null;
    }

    private void ReturnFromSub()
    {
        if (subReturnStack.Count > 0)
        {
            pc = subReturnStack.Pop();
        }
    }

    public object? VisitLabelStatement(LabelStatement stmt)
    {
        // Label is just a marker - record its position
        labels[stmt.Label] = pc;
        return null;
    }

    public object? VisitGotoLabelStatement(GotoLabelStatement stmt)
    {
        if (!labels.TryGetValue(stmt.Label, out int targetPc))
        {
            // Label might be defined later - scan for it
            targetPc = FindLabel(stmt.Label);
        }
        pc = targetPc;
        return null;
    }

    public object? VisitGosubLabelStatement(GosubLabelStatement stmt)
    {
        if (!labels.TryGetValue(stmt.Label, out int targetPc))
        {
            // Label might be defined later - scan for it
            targetPc = FindLabel(stmt.Label);
        }
        gosubStack.Push(program!.Lines[pc].LineNumber);
        pc = targetPc;
        return null;
    }

    private int FindLabel(string label)
    {
        // Scan through all lines looking for the label
        for (int i = 0; i < program!.Lines.Count; i++)
        {
            var stmt = program.Lines[i].Statement;
            if (stmt is LabelStatement labelStmt &&
                string.Equals(labelStmt.Label, label, StringComparison.OrdinalIgnoreCase))
            {
                labels[label] = i;
                return i;
            }
            // Check for labels inside compound statements
            if (stmt is CompoundStatement compound)
            {
                foreach (var s in compound.Statements)
                {
                    if (s is LabelStatement ls &&
                        string.Equals(ls.Label, label, StringComparison.OrdinalIgnoreCase))
                    {
                        labels[label] = i;
                        return i;
                    }
                }
            }
        }
        throw new RuntimeException($"Label not found: {label}");
    }

    public object? VisitTypeStatement(TypeStatement stmt)
    {
        // TYPE definition - collect fields from subsequent lines until END TYPE
        var fields = new List<TypeField>();

        while (pc < program!.Lines.Count - 1)
        {
            pc++;
            var nextStmt = GetStatementType(program.Lines[pc].Statement);

            if (nextStmt is EndStatement)
            {
                break;
            }

            if (nextStmt is TypeFieldDeclStatement fieldDecl)
            {
                fields.Add(new TypeField(fieldDecl.FieldName, fieldDecl.TypeName, fieldDecl.StringLength));
            }
        }

        types[stmt.Name.Lexeme] = new UserType(stmt.Name.Lexeme, fields);
        return null;
    }

    public object? VisitTypeFieldDeclStatement(TypeFieldDeclStatement stmt)
    {
        // This is handled within VisitTypeStatement when inside a TYPE block
        // If encountered outside a TYPE block, just ignore
        return null;
    }

    public object? VisitDefTypeStatement(DefTypeStatement stmt)
    {
        // DEFINT A-Z, etc.
        for (char c = stmt.StartLetter; c <= stmt.EndLetter; c++)
        {
            defTypes[c] = stmt.VarType;
        }
        return null;
    }

    public object? VisitPaletteStatement(PaletteStatement stmt)
    {
        // PALETTE attribute, color - not fully implemented
        // Just evaluate the expressions but ignore for now
        if (stmt.Attribute != null)
        {
            Evaluate(stmt.Attribute);
            if (stmt.Color != null)
            {
                Evaluate(stmt.Color);
            }
        }
        return null;
    }

    public object? VisitViewPrintStatement(ViewPrintStatement stmt)
    {
        // VIEW PRINT [top TO bottom]
        if (stmt.TopRow != null && stmt.BottomRow != null)
        {
            viewPrintTop = (int)ToDouble(Evaluate(stmt.TopRow));
            viewPrintBottom = (int)ToDouble(Evaluate(stmt.BottomRow));
        }
        else
        {
            viewPrintTop = 1;
            viewPrintBottom = 25;
        }
        return null;
    }

    public object? VisitRedimStatement(RedimStatement stmt)
    {
        // REDIM [PRESERVE] array(dimensions)
        foreach (var decl in stmt.Declarations)
        {
            // Add 1 because DIM/REDIM arr(N) creates indices 0 to N (N+1 elements)
            var dims = decl.Dimensions.Select(d => (int)ToDouble(Evaluate(d)) + 1).ToArray();
            var arrayName = decl.Name.Lexeme;

            if (stmt.Preserve && arrays.TryGetValue(arrayName, out var existingArray))
            {
                // PRESERVE: try to keep existing values
                // Simplified: just recreate the array (full preserve would copy values)
            }

            var newArray = new BasicArray(dims, arrayName.EndsWith('$'));
            arrays[arrayName] = newArray;
        }
        return null;
    }

    public object? VisitDefSegStatement(DefSegStatement stmt)
    {
        // DEF SEG [= segment]
        if (stmt.Segment != null)
        {
            defSeg = (int)ToDouble(Evaluate(stmt.Segment));
        }
        else
        {
            defSeg = 0;
        }
        return null;
    }

    public object? VisitPokeStatement(PokeStatement stmt)
    {
        // POKE address, value
        int address = (int)ToDouble(Evaluate(stmt.Address));
        int value = (int)ToDouble(Evaluate(stmt.Value)) & 0xFF;

        if (address >= 0 && address < memory.Length)
        {
            memory[address] = (byte)value;
        }
        return null;
    }

    public object? VisitPutGraphicsStatement(PutGraphicsStatement stmt)
    {
        // PUT (x, y), arrayname, action
        int x = (int)ToDouble(Evaluate(stmt.X));
        int y = (int)ToDouble(Evaluate(stmt.Y));

        // Get the graphics data from the array
        if (!arrays.TryGetValue(stmt.ArrayName.Lexeme, out var array))
        {
            throw new RuntimeException($"Array not found: {stmt.ArrayName.Lexeme}");
        }

        // Simplified PUT - actual implementation would blit the array data to screen
        // For now, just a placeholder
        return null;
    }

    public object? VisitGetGraphicsStatement(GetGraphicsStatement stmt)
    {
        // GET (x1, y1)-(x2, y2), arrayname
        int x1 = (int)ToDouble(Evaluate(stmt.X1));
        int y1 = (int)ToDouble(Evaluate(stmt.Y1));
        int x2 = (int)ToDouble(Evaluate(stmt.X2));
        int y2 = (int)ToDouble(Evaluate(stmt.Y2));

        // Calculate size needed
        int width = Math.Abs(x2 - x1) + 1;
        int height = Math.Abs(y2 - y1) + 1;

        // Ensure array exists and is sized appropriately
        // For GW-BASIC, GET stores: 2 bytes for width, 2 bytes for height, then pixel data
        int bytesNeeded = 4 + (width * height + 7) / 8;
        int elementsNeeded = (bytesNeeded + 1) / 2; // 2 bytes per integer element

        if (!arrays.TryGetValue(stmt.ArrayName.Lexeme, out var array))
        {
            // Create array if it doesn't exist
            arrays[stmt.ArrayName.Lexeme] = new BasicArray(new[] { elementsNeeded }, false);
        }

        // Simplified GET - actual implementation would capture screen data
        return null;
    }

    private double PeekMemory(int address)
    {
        if (address >= 0 && address < memory.Length)
        {
            return memory[address];
        }
        return 0.0;
    }

    private static string FormatNumber(double value, string format)
    {
        // Parse format string
        bool hasDollar = format.Contains('$');
        bool hasAsterisks = format.StartsWith("**");
        bool hasComma = format.Contains(',');
        int dotPos = format.IndexOf('.');
        int totalWidth = format.Replace(",", "").Replace("$", "").Length;
        int decimalPlaces = dotPos >= 0 ? format.Length - dotPos - 1 : 0;

        // Format the number
        string numStr;
        if (decimalPlaces > 0)
        {
            numStr = value.ToString($"F{decimalPlaces}", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            numStr = ((int)value).ToString();
        }

        if (hasComma)
        {
            // Add thousands separators
            var parts = numStr.Split('.');
            if (double.TryParse(parts[0], out var intPart))
            {
                parts[0] = intPart.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            }
            numStr = string.Join(".", parts);
        }

        // Pad to width
        if (hasDollar)
        {
            numStr = "$" + numStr;
        }

        if (hasAsterisks)
        {
            numStr = numStr.PadLeft(totalWidth, '*');
        }
        else
        {
            numStr = numStr.PadLeft(totalWidth);
        }

        return numStr;
    }

    private bool IsEof(int fileNumber)
    {
        if (!files.TryGetValue(fileNumber, out var handle) || handle.Reader == null)
        {
            return true;
        }
        return handle.Reader.EndOfStream;
    }

    public object? VisitBinaryExpression(BinaryExpression expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        return expr.Operator.Type switch
        {
            TokenType.Plus => Add(left, right),
            TokenType.Minus => ToDouble(left) - ToDouble(right),
            TokenType.Star => ToDouble(left) * ToDouble(right),
            TokenType.Slash => Divide(left, right),
            TokenType.Backslash => (double)((int)ToDouble(left) / (int)ToDouble(right)),
            TokenType.Caret => Math.Pow(ToDouble(left), ToDouble(right)),
            TokenType.Mod => ToDouble(left) % ToDouble(right),
            TokenType.Equal => Compare(left, right) == 0 ? -1.0 : 0.0,
            TokenType.NotEqual => Compare(left, right) != 0 ? -1.0 : 0.0,
            TokenType.Less => Compare(left, right) < 0 ? -1.0 : 0.0,
            TokenType.LessEqual => Compare(left, right) <= 0 ? -1.0 : 0.0,
            TokenType.Greater => Compare(left, right) > 0 ? -1.0 : 0.0,
            TokenType.GreaterEqual => Compare(left, right) >= 0 ? -1.0 : 0.0,
            TokenType.And => (double)((int)ToDouble(left) & (int)ToDouble(right)),
            TokenType.Or => (double)((int)ToDouble(left) | (int)ToDouble(right)),
            TokenType.Xor => (double)((int)ToDouble(left) ^ (int)ToDouble(right)),
            TokenType.Eqv => (double)(~((int)ToDouble(left) ^ (int)ToDouble(right))),
            TokenType.Imp => (double)((~(int)ToDouble(left)) | (int)ToDouble(right)),
            _ => throw new RuntimeException($"Unknown operator: {expr.Operator.Lexeme}")
        };
    }

    public object? VisitUnaryExpression(UnaryExpression expr)
    {
        var right = Evaluate(expr.Right);

        return expr.Operator.Type switch
        {
            TokenType.Minus => -ToDouble(right),
            TokenType.Not => (double)(~(int)ToDouble(right)),
            _ => throw new RuntimeException($"Unknown unary operator: {expr.Operator.Lexeme}")
        };
    }

    public object? VisitLiteralExpression(LiteralExpression expr) => expr.Value;

    public object? VisitVariableExpression(VariableExpression expr)
    {
        // Check constants first
        if (constants.TryGetValue(expr.Name.Lexeme, out var constValue))
        {
            return constValue;
        }

        if (variables.TryGetValue(expr.Name.Lexeme, out var value))
        {
            return value;
        }

        if (expr.Name.Lexeme.EndsWith('$'))
        {
            return "";
        }
        return 0.0;
    }

    public object? VisitGroupingExpression(GroupingExpression expr) => Evaluate(expr.Expression);

    public object? VisitCallExpression(CallExpression expr)
    {
        var args = expr.Arguments.Select(Evaluate).ToArray();
        return CallBuiltInFunction(expr.Name, args);
    }

    public object? VisitArrayAccessExpression(ArrayAccessExpression expr)
    {
        var name = expr.Name.Lexeme;

        // Check if this is actually a user-defined FUNCTION call
        if (functions.TryGetValue(name, out _))
        {
            var args = expr.Indices.Select(Evaluate).ToArray();
            return CallBuiltInFunction(name, args);
        }

        // Check if this is a user-defined DEF FN function
        if (userFunctions.TryGetValue(name, out _))
        {
            var args = expr.Indices.Select(Evaluate).ToArray();
            return CallBuiltInFunction(name, args);
        }

        // If no indices, return the array reference (for passing arrays to SUBs like BCoor())
        if (expr.Indices.Count == 0)
        {
            if (arrays.TryGetValue(name, out var array))
            {
                return array;
            }
            // Array doesn't exist yet, auto-create it with default size
            var newArray = new BasicArray([11], name.EndsWith('$'));
            arrays[name] = newArray;
            return newArray;
        }

        var indices = expr.Indices.Select(i => (int)ToDouble(Evaluate(i))).ToArray();
        return GetArrayElement(name, indices);
    }

    public object? VisitFieldAccessExpression(FieldAccessExpression expr)
    {
        var obj = Evaluate(expr.Object);
        if (obj is TypeInstance instance)
        {
            var fieldName = expr.FieldName.Lexeme;
            if (instance.Fields.TryGetValue(fieldName, out var value))
            {
                return value;
            }
            throw new RuntimeException($"Unknown field '{fieldName}' in type '{instance.Type.Name}'");
        }
        throw new RuntimeException("Cannot access field on non-type value");
    }

    private object? CallBuiltInFunction(string name, object?[] args)
    {
        return name.ToUpperInvariant() switch
        {
            "ABS" => Math.Abs(ToDouble(args[0])),
            "SGN" => (double)Math.Sign(ToDouble(args[0])),
            "INT" => Math.Floor(ToDouble(args[0])),
            "FIX" => Math.Truncate(ToDouble(args[0])),
            "SQR" => Math.Sqrt(ToDouble(args[0])),
            "SIN" => Math.Sin(ToDouble(args[0])),
            "COS" => Math.Cos(ToDouble(args[0])),
            "TAN" => Math.Tan(ToDouble(args[0])),
            "ATN" => Math.Atan(ToDouble(args[0])),
            "LOG" => Math.Log(ToDouble(args[0])),
            "EXP" => Math.Exp(ToDouble(args[0])),
            "RND" => args.Length > 0 && ToDouble(args[0]) < 0 ? random.NextDouble() : random.NextDouble(),
            "LEN" => (double)Stringify(args[0]).Length,
            "ASC" => Stringify(args[0]).Length > 0 ? (double)Stringify(args[0])[0] : 0.0,
            "CHR$" => ((char)(int)ToDouble(args[0])).ToString(),
            "STR$" => Stringify(args[0]),
            "VAL" => double.TryParse(Stringify(args[0]).Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.0,
            "LEFT$" => LeftString(Stringify(args[0]), (int)ToDouble(args[1])),
            "RIGHT$" => RightString(Stringify(args[0]), (int)ToDouble(args[1])),
            "MID$" => MidString(args),
            "INSTR" => InstrFunction(args),
            "STRING$" => new string(args.Length > 1 ? (Stringify(args[1]).Length > 0 ? Stringify(args[1])[0] : ' ') : ' ', (int)ToDouble(args[0])),
            "SPACE$" => new string(' ', (int)ToDouble(args[0])),
            "TAB" => new string(' ', Math.Max(0, (int)ToDouble(args[0]) - 1)),
            "SPC" => new string(' ', (int)ToDouble(args[0])),
            "UCASE$" => Stringify(args[0]).ToUpperInvariant(),
            "LCASE$" => Stringify(args[0]).ToLowerInvariant(),
            "TIMER" => (double)(DateTime.Now - DateTime.Today).TotalSeconds,
            "INKEY$" => graphics.IsGraphicsMode ? graphics.ReadKey() : console.ReadKey(),
            "PEEK" => PeekMemory((int)ToDouble(args[0])),
            "FRE" => 65536.0,
            "POINT" => args.Length >= 2 ? graphics.GetPixel((int)ToDouble(args[0]), (int)ToDouble(args[1])) : 0.0,
            "CSRLIN" => 1.0,
            "POS" => 1.0,
            "EOF" => IsEof((int)ToDouble(args[0])) ? -1.0 : 0.0,
            "LOF" => GetFileLength((int)ToDouble(args[0])),
            "LOC" => GetFilePosition((int)ToDouble(args[0])),
            // Type conversion functions
            "HEX$" => Convert.ToString((int)ToDouble(args[0]), 16).ToUpperInvariant(),
            "OCT$" => Convert.ToString((int)ToDouble(args[0]), 8),
            "CINT" => (double)(int)Math.Round(ToDouble(args[0])),
            "CDBL" => ToDouble(args[0]),
            "CSNG" => (double)(float)ToDouble(args[0]),
            // Random file conversion functions
            "CVI" => BitConverter.ToInt16(System.Text.Encoding.ASCII.GetBytes(Stringify(args[0]).PadRight(2, '\0')), 0),
            "CVS" => (double)BitConverter.ToSingle(System.Text.Encoding.ASCII.GetBytes(Stringify(args[0]).PadRight(4, '\0')), 0),
            "CVD" => BitConverter.ToDouble(System.Text.Encoding.ASCII.GetBytes(Stringify(args[0]).PadRight(8, '\0')), 0),
            "MKI$" => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes((short)ToDouble(args[0]))),
            "MKS$" => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes((float)ToDouble(args[0]))),
            "MKD$" => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(ToDouble(args[0]))),
            // RGB function for 24-bit color modes (SCREEN 15, 17, 19)
            "RGB" => RgbFunction(args),
            // Error functions
            "ERR" => (double)lastError,
            "ERL" => (double)lastErrorLine,
            // INPUT$ function
            "INPUT$" => ReadInputChars(args),
            _ => CallUserFunction(name, args)
        };
    }

    private object? CallUserFunction(string name, object?[] args)
    {
        // Check for FN user-defined functions (DEF FN style)
        if (userFunctions.TryGetValue(name, out var fn))
        {
            // Save current variable values for parameters
            var savedValues = new Dictionary<string, object?>();
            for (int i = 0; i < fn.Parameters.Count && i < args.Length; i++)
            {
                var paramName = fn.Parameters[i];
                savedValues[paramName] = variables.GetValueOrDefault(paramName);
                variables[paramName] = args[i];
            }

            // Evaluate the function body
            var result = Evaluate(fn.Body);

            // Restore parameter values
            foreach (var (paramName, value) in savedValues)
            {
                if (value != null)
                    variables[paramName] = value;
                else
                    variables.Remove(paramName);
            }

            return result;
        }

        // Check for QBasic-style FUNCTION definitions
        if (functions.TryGetValue(name, out var funcInfo))
        {
            // For QBasic-style functions, we need to execute the function body
            // This is simplified - we'd need to handle the function execution properly
            // For now, return the variable with the function name (QBasic convention)
            // Set parameters
            for (int i = 0; i < funcInfo.Statement.Parameters.Count && i < args.Length; i++)
            {
                var paramName = funcInfo.Statement.Parameters[i].Name.Lexeme;
                variables[paramName] = args[i];
            }

            // Initialize function return variable
            var funcName = funcInfo.Statement.Name.Lexeme;
            if (funcName.EndsWith('$'))
                variables[funcName] = "";
            else
                variables[funcName] = 0.0;

            // Execute the function body
            var savedPc = pc;
            subReturnStack.Push(pc);
            pc = funcInfo.StartPc + 1; // Start at line after FUNCTION

            // Execute until END FUNCTION
            while (pc < program!.Lines.Count && subReturnStack.Count > savedPc)
            {
                var stmt = GetStatementType(program.Lines[pc].Statement);
                if (stmt is EndStatement)
                {
                    break;
                }
                Execute(program.Lines[pc].Statement);
                if (jumpTarget >= 0)
                {
                    if (lineIndex!.TryGetValue(jumpTarget, out int targetIndex))
                    {
                        pc = targetIndex;
                        jumpTarget = -1;
                    }
                }
                else
                {
                    pc++;
                }
            }

            subReturnStack.Pop();
            pc = savedPc;

            // Return the function value
            return variables.GetValueOrDefault(funcName);
        }

        throw new RuntimeException($"Unknown function: {name}");
    }

    private static string LeftString(string s, int n) => n >= s.Length ? s : s[..Math.Max(0, n)];

    private static string RightString(string s, int n) => n >= s.Length ? s : s[^Math.Max(0, n)..];

    private static string MidString(object?[] args)
    {
        var s = Stringify(args[0]);
        var start = (int)ToDouble(args[1]) - 1;
        if (start < 0 || start >= s.Length) return "";

        if (args.Length > 2)
        {
            var len = (int)ToDouble(args[2]);
            return s.Substring(start, Math.Min(len, s.Length - start));
        }
        return s[start..];
    }

    private static double InstrFunction(object?[] args)
    {
        int startPos = 1;
        string haystack, needle;

        if (args.Length == 3)
        {
            startPos = (int)ToDouble(args[0]);
            haystack = Stringify(args[1]);
            needle = Stringify(args[2]);
        }
        else
        {
            haystack = Stringify(args[0]);
            needle = Stringify(args[1]);
        }

        if (startPos < 1) return 0;
        var idx = haystack.IndexOf(needle, startPos - 1, StringComparison.Ordinal);
        return idx < 0 ? 0 : idx + 1;
    }

    private int GetCurrentLineNumber()
    {
        if (program != null && pc >= 0 && pc < program.Lines.Count)
        {
            return program.Lines[pc].LineNumber;
        }
        return -1;
    }

    private object? GetArrayElement(string name, int[] indices)
    {
        if (!arrays.TryGetValue(name, out var array))
        {
            // Auto-dimension with default size 10
            var dims = indices.Select(_ => 11).ToArray();
            array = new BasicArray(dims, name.EndsWith('$'));
            arrays[name] = array;
        }
        try
        {
            return array.Get(indices);
        }
        catch (RuntimeException ex)
        {
            var lineNum = GetCurrentLineNumber();
            throw new RuntimeException($"{ex.Message} for {name}({string.Join(", ", indices)}) at line {lineNum}");
        }
    }

    private void SetArrayElement(string name, int[] indices, object? value)
    {
        if (!arrays.TryGetValue(name, out var array))
        {
            var dims = indices.Select(_ => 11).ToArray();
            array = new BasicArray(dims, name.EndsWith('$'));
            arrays[name] = array;
        }
        try
        {
            array.Set(indices, value);
        }
        catch (RuntimeException ex)
        {
            var lineNum = GetCurrentLineNumber();
            throw new RuntimeException($"{ex.Message} for {name}({string.Join(", ", indices)}) at line {lineNum}");
        }
    }

    private static bool IsTruthy(object? value) => ToDouble(value) != 0;

    private static int Compare(object? left, object? right)
    {
        if (left is string ls && right is string rs)
        {
            return string.Compare(ls, rs, StringComparison.Ordinal);
        }
        var l = ToDouble(left);
        var r = ToDouble(right);
        return l.CompareTo(r);
    }

    private static object? Add(object? left, object? right)
    {
        if (left is string || right is string)
        {
            return Stringify(left) + Stringify(right);
        }
        return ToDouble(left) + ToDouble(right);
    }

    private static double Divide(object? left, object? right)
    {
        var divisor = ToDouble(right);
        if (divisor == 0)
        {
            throw new RuntimeException("Division by zero");
        }
        return ToDouble(left) / divisor;
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            double d => d,
            int i => i,
            string s when double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d) => d,
            null => 0,
            _ => throw new RuntimeException($"Cannot convert '{value}' to number")
        };
    }

    private static string Stringify(object? value)
    {
        return value switch
        {
            null => "",
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }

    private double GetFileLength(int fileNumber)
    {
        if (!files.TryGetValue(fileNumber, out var handle))
        {
            return 0;
        }
        try
        {
            return new FileInfo(handle.FileName).Length;
        }
        catch
        {
            return 0;
        }
    }

    private double GetFilePosition(int fileNumber)
    {
        if (!files.TryGetValue(fileNumber, out var handle))
        {
            return 0;
        }
        // Return line-based position (simplified)
        return handle.Position;
    }

    /// <summary>
    /// RGB(r, g, b) function - creates a 24-bit color value for use in graphics commands.
    /// Returns a value > 255 which signals to FrameBuffer to use direct RGB instead of palette.
    /// </summary>
    private double RgbFunction(object?[] args)
    {
        if (args.Length < 3)
            throw new RuntimeException("RGB requires 3 arguments: RGB(r, g, b)");

        int r = Math.Clamp((int)ToDouble(args[0]), 0, 255);
        int g = Math.Clamp((int)ToDouble(args[1]), 0, 255);
        int b = Math.Clamp((int)ToDouble(args[2]), 0, 255);

        // Return packed RGB value (always > 255 to distinguish from palette index)
        // Format: 0x00RRGGBB with high bit set to ensure > 255
        return (r << 16) | (g << 8) | b | 0x1000000;
    }

    private string ReadInputChars(object?[] args)
    {
        int count = (int)ToDouble(args[0]);
        if (args.Length > 1)
        {
            // Read from file
            int fileNumber = (int)ToDouble(args[1]);
            if (files.TryGetValue(fileNumber, out var handle) && handle.Reader != null)
            {
                var buffer = new char[count];
                int read = handle.Reader.Read(buffer, 0, count);
                return new string(buffer, 0, read);
            }
            return "";
        }
        // Read from console (simplified - returns empty in non-interactive mode)
        return "";
    }
}

public class RuntimeException(string message) : Exception(message);

internal sealed class ForLoopState(string variable, double endValue, double step, int loopStartIndex, int lineNumber)
{
    public string Variable { get; } = variable;
    public double EndValue { get; } = endValue;
    public double Step { get; } = step;
    public int LoopStartIndex { get; } = loopStartIndex;
    public int LineNumber { get; } = lineNumber;
}

internal sealed class WhileLoopState(int loopStartIndex, int lineNumber)
{
    public int LoopStartIndex { get; } = loopStartIndex;
    public int LineNumber { get; } = lineNumber;
}

internal sealed class DoLoopState(int loopStartIndex, int lineNumber)
{
    public int LoopStartIndex { get; } = loopStartIndex;
    public int LineNumber { get; } = lineNumber;
}

internal sealed class BasicArray
{
    private readonly object?[] data;
    private readonly int[] dimensions;
    private readonly bool isString;
    public UserType? ElementType { get; }

    public BasicArray(int[] dimensions, bool isString, UserType? elementType = null)
    {
        this.dimensions = dimensions;
        this.isString = isString;
        ElementType = elementType;

        int totalSize = 1;
        foreach (var dim in dimensions)
        {
            totalSize *= dim;
        }
        data = new object?[totalSize];

        // Initialize with default values (for typed arrays, initialization is done separately)
        if (elementType == null)
        {
            var defaultValue = isString ? (object)"" : 0.0;
            Array.Fill(data, defaultValue);
        }
    }

    public object? Get(int[] indices)
    {
        int index = CalculateIndex(indices);
        return data[index];
    }

    public void Set(int[] indices, object? value)
    {
        int index = CalculateIndex(indices);
        data[index] = value;
    }

    public void SetElement(int[] indices, object? value)
    {
        int index = CalculateIndex(indices);
        data[index] = value;
    }

    private int CalculateIndex(int[] indices)
    {
        if (indices.Length != dimensions.Length)
        {
            throw new RuntimeException("Wrong number of dimensions");
        }

        int index = 0;
        int multiplier = 1;

        for (int i = dimensions.Length - 1; i >= 0; i--)
        {
            if (indices[i] < 0 || indices[i] >= dimensions[i])
            {
                throw new RuntimeException("Subscript out of range");
            }
            index += indices[i] * multiplier;
            multiplier *= dimensions[i];
        }

        return index;
    }
}

internal sealed class FileHandle
{
    public string FileName { get; }
    public StreamReader? Reader { get; }
    public StreamWriter? Writer { get; }
    public FileStream? RandomStream { get; }
    public int Position { get; private set; }
    public int RecordLength { get; }
    public int CurrentRecord { get; set; }
    public byte[] RecordBuffer { get; }
    public List<(int offset, int width, string variable)> FieldMappings { get; } = [];

    public FileHandle(string fileName, StreamReader? reader, StreamWriter? writer, int recordLength = 128)
    {
        FileName = fileName;
        Reader = reader;
        Writer = writer;
        Position = 0;
        RecordLength = recordLength;
        CurrentRecord = 1;
        RecordBuffer = new byte[recordLength];
    }

    public FileHandle(string fileName, FileStream randomStream, int recordLength)
    {
        FileName = fileName;
        RandomStream = randomStream;
        RecordLength = recordLength;
        CurrentRecord = 1;
        RecordBuffer = new byte[recordLength];
    }

    public void Close()
    {
        Reader?.Close();
        Writer?.Close();
        RandomStream?.Close();
    }
}

internal sealed class UserFunction(IReadOnlyList<string> parameters, IExpression body)
{
    public IReadOnlyList<string> Parameters { get; } = parameters;
    public IExpression Body { get; } = body;
}

internal sealed class SubInfo(SubStatement statement, int startPc)
{
    public SubStatement Statement { get; } = statement;
    public int StartPc { get; } = startPc;
}

internal sealed class FunctionInfo(FunctionStatement statement, int startPc)
{
    public FunctionStatement Statement { get; } = statement;
    public int StartPc { get; } = startPc;
}

internal sealed class UserType(string name, IReadOnlyList<TypeField> fields)
{
    public string Name { get; } = name;
    public IReadOnlyList<TypeField> Fields { get; } = fields;

    public TypeInstance CreateInstance()
    {
        var instance = new TypeInstance(this);
        foreach (var field in Fields)
        {
            instance.Fields[field.Name.Lexeme] = field.TypeName.Equals("STRING", StringComparison.OrdinalIgnoreCase) ? "" : 0.0;
        }
        return instance;
    }
}

internal sealed class TypeInstance(UserType type)
{
    public UserType Type { get; } = type;
    public Dictionary<string, object?> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
}
