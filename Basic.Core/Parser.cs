using Basic.Core.Ast;

namespace Basic.Core;

public sealed class Parser
{
    private readonly List<Token> tokens;
    private readonly string[] sourceLines;
    private int current;

    public Parser(List<Token> tokens, string? source = null)
    {
        this.tokens = tokens;
        sourceLines = source?.Split('\n') ?? [];
    }

    private ParserException Error(string message)
    {
        int line = Peek().Line;
        string sourceContext = "";
        if (line > 0 && line <= sourceLines.Length)
        {
            sourceContext = $"\n  {sourceLines[line - 1].TrimEnd()}";
        }
        return new ParserException($"{message} at line {line}{sourceContext}");
    }

    private static readonly HashSet<string> BuiltInFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ABS", "SGN", "INT", "FIX", "SQR", "SIN", "COS", "TAN", "ATN", "LOG", "EXP",
        "RND", "LEN", "ASC", "CHR$", "STR$", "VAL", "LEFT$", "RIGHT$", "MID$",
        "INSTR", "STRING$", "SPACE$", "TAB", "SPC", "UCASE$", "LCASE$", "TIMER",
        "INKEY$", "PEEK", "FRE", "POINT", "CSRLIN", "POS",
        "EOF", "LOF", "LOC",
        "HEX$", "OCT$", "CINT", "CDBL", "CSNG",
        "CVI", "CVS", "CVD", "MKI$", "MKS$", "MKD$",
        "ERR", "ERL", "INPUT$",
        "RGB"
    };

    // Functions that can be called without parentheses
    private static readonly HashSet<string> ZeroArgFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ERR", "ERL", "TIMER", "INKEY$", "RND", "CSRLIN", "FRE"
    };

    public BasicProgram Parse()
    {
        var lines = new List<ProgramLine>();

        while (!IsAtEnd())
        {
            SkipNewlines();
            if (IsAtEnd()) break;

            var line = ParseLine();
            if (line != null)
            {
                lines.Add(line);
            }
        }

        // Sort lines by line number (GW-BASIC sorts them)
        lines.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));

        return new BasicProgram(lines);
    }

    private ProgramLine? ParseLine()
    {
        // Expect line number
        if (!Check(TokenType.Number))
        {
            throw Error("Expected line number");
        }

        var lineNumToken = Advance();
        int lineNumber = (int)(double)lineNumToken.Literal!;

        // Parse first statement
        var statements = new List<IStatement>();
        statements.Add(ParseStatement());

        // Parse additional statements separated by colons
        while (Match(TokenType.Colon))
        {
            // Skip if we hit EOL or EOF after colon
            if (Check(TokenType.Eol) || IsAtEnd())
                break;
            statements.Add(ParseStatement());
        }

        // Skip trailing REM comment (from ' syntax)
        if (Check(TokenType.Rem))
        {
            Advance();
        }

        // Consume EOL if present
        if (Check(TokenType.Eol))
        {
            Advance();
        }

        // If only one statement, return it directly; otherwise wrap in CompoundStatement
        IStatement resultStatement = statements.Count == 1
            ? statements[0]
            : new CompoundStatement(statements);

        return new ProgramLine(lineNumber, resultStatement);
    }

    private IStatement ParseStatement()
    {
        if (Match(TokenType.Print)) return ParsePrintStatement();
        if (Match(TokenType.Let)) return ParseLetStatement();
        if (Match(TokenType.Rem)) return ParseRemStatement();
        if (Match(TokenType.Goto)) return ParseGotoStatement();
        if (Match(TokenType.If)) return ParseIfStatement();
        if (Match(TokenType.For)) return ParseForStatement();
        if (Match(TokenType.Next)) return ParseNextStatement();
        if (Match(TokenType.While)) return ParseWhileStatement();
        if (Match(TokenType.Wend)) return new WendStatement();
        if (Match(TokenType.Gosub)) return ParseGosubStatement();
        if (Match(TokenType.Return)) return new ReturnStatement();
        if (Match(TokenType.End))
        {
            // Check for END SELECT, END SUB, END FUNCTION, END TYPE, END IF
            if (Match(TokenType.Select)) return new EndSelectStatement();
            if (Match(TokenType.Sub)) return new EndStatement(); // Will be handled as SUB terminator
            if (Match(TokenType.Function)) return new EndStatement(); // Will be handled as FUNCTION terminator
            if (Match(TokenType.Type)) return new EndStatement(); // Will be handled as TYPE terminator
            if (Match(TokenType.If)) return new EndIfStatement();
            return new EndStatement();
        }
        if (Match(TokenType.Else))
        {
            // Standalone ELSE for block-IF, or ELSE IF (two tokens)
            if (Match(TokenType.If))
            {
                // ELSE IF condition THEN
                var condition = ParseExpression();
                Consume(TokenType.Then, "Expected THEN after ELSEIF condition");
                return new ElseIfStatement(condition);
            }
            return new ElseStatement();
        }
        if (Match(TokenType.ElseIf))
        {
            // ELSEIF condition THEN (single token)
            var condition = ParseExpression();
            Consume(TokenType.Then, "Expected THEN after ELSEIF condition");
            return new ElseIfStatement(condition);
        }
        if (Match(TokenType.Stop)) return new EndStatement();
        if (Match(TokenType.Input)) return ParseInputStatement();
        if (Match(TokenType.Dim)) return ParseDimStatement();
        if (Match(TokenType.Data)) return ParseDataStatement();
        if (Match(TokenType.Read)) return ParseReadStatement();
        if (Match(TokenType.Restore)) return ParseRestoreStatement();
        if (Match(TokenType.On)) return ParseOnStatement();
        if (Match(TokenType.Swap)) return ParseSwapStatement();
        if (Match(TokenType.Cls))
        {
            IExpression? mode = null;
            if (Check(TokenType.Number))
            {
                mode = ParseExpression();
            }
            return new ClsStatement(mode);
        }

        // Graphics statements
        if (Match(TokenType.Screen)) return ParseScreenStatement();
        if (Match(TokenType.Pset)) return ParsePsetStatement(false);
        if (Match(TokenType.Preset)) return ParsePsetStatement(true);
        if (Match(TokenType.Circle)) return ParseCircleStatement();
        if (Match(TokenType.Paint)) return ParsePaintStatement();
        if (Match(TokenType.Draw)) return ParseDrawStatement();
        if (Match(TokenType.Color)) return ParseColorStatement();
        if (Match(TokenType.Locate)) return ParseLocateStatement();
        if (Match(TokenType.Beep)) return new BeepStatement();

        // File I/O statements
        if (Match(TokenType.Open)) return ParseOpenStatement();
        if (Match(TokenType.Close)) return ParseCloseStatement();
        if (Match(TokenType.Write)) return ParseWriteFileStatement();
        if (Match(TokenType.Kill)) return ParseKillStatement();
        if (Match(TokenType.Name)) return ParseNameStatement();
        if (Match(TokenType.Files)) return ParseFilesStatement();

        // Additional statements
        if (Match(TokenType.Randomize)) return ParseRandomizeStatement();
        if (Match(TokenType.Line)) return ParseLineStatementOrLineInput();
        if (Match(TokenType.Def)) return ParseDefStatement();
        if (Match(TokenType.Tron)) return new TronStatement();
        if (Match(TokenType.Troff)) return new TroffStatement();
        if (Match(TokenType.Width)) return ParseWidthStatement();
        if (Match(TokenType.Sound)) return ParseSoundStatement();
        if (Match(TokenType.Play)) return ParsePlayStatement();
        if (Match(TokenType.Error)) return ParseErrorStatement();
        if (Match(TokenType.Resume)) return ParseResumeStatement();

        // QBasic-style statements
        if (Match(TokenType.Const)) return ParseConstStatement();
        if (Match(TokenType.Sleep)) return ParseSleepStatement();
        if (Match(TokenType.Select)) return ParseSelectCaseStatement();
        if (Match(TokenType.Case)) return ParseCaseClauseStatement();
        if (Match(TokenType.Do)) return ParseDoStatement();
        if (Match(TokenType.Loop)) return ParseLoopStatement();
        if (Match(TokenType.Exit)) return ParseExitStatement();
        if (Match(TokenType.Declare)) return ParseDeclareStatement();
        if (Match(TokenType.Sub)) return ParseSubStatement();
        if (Match(TokenType.Function)) return ParseFunctionStatement();
        if (Match(TokenType.Call)) return ParseCallStatement();
        if (Match(TokenType.Type)) return ParseTypeStatement();
        if (Match(TokenType.Defint)) return ParseDefTypeStatement(BasicVarType.Integer);
        if (Match(TokenType.Deflng)) return ParseDefTypeStatement(BasicVarType.Long);
        if (Match(TokenType.Defsng)) return ParseDefTypeStatement(BasicVarType.Single);
        if (Match(TokenType.Defdbl)) return ParseDefTypeStatement(BasicVarType.Double);
        if (Match(TokenType.Defstr)) return ParseDefTypeStatement(BasicVarType.String);
        if (Match(TokenType.Palette)) return ParsePaletteStatement();
        if (Match(TokenType.View)) return ParseViewStatement();
        if (Match(TokenType.Redim)) return ParseRedimStatement();
        if (Match(TokenType.Poke)) return ParsePokeStatement();

        // Random access file statements
        if (Match(TokenType.Field)) return ParseFieldStatement();
        if (Match(TokenType.Get)) return ParseGetStatement();
        if (Match(TokenType.Put)) return ParsePutStatement();
        if (Match(TokenType.Lset)) return ParseLsetStatement();
        if (Match(TokenType.Rset)) return ParseRsetStatement();

        // Check for label definition (identifier followed by colon)
        // or implicit LET: A = 5 is same as LET A = 5
        // or SUB call without CALL: SubName arg1, arg2
        if (Check(TokenType.Identifier))
        {
            // Look ahead to see if this is a label (identifier followed by colon)
            if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.Colon)
            {
                var labelToken = Advance(); // consume identifier
                Advance(); // consume colon
                return new LabelStatement(labelToken.Lexeme);
            }

            // Check for TYPE field definition (identifier AS typename)
            if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.As)
            {
                return ParseTypeFieldDeclaration();
            }

            // Check if this could be a SUB call (no = sign following)
            // identifier EOL/REM -> SUB call with no args
            // identifier args (not starting with =) -> SUB call with args
            var nextToken = current + 1 < tokens.Count ? tokens[current + 1].Type : TokenType.Eof;
            if (nextToken == TokenType.Eol || nextToken == TokenType.Rem || nextToken == TokenType.Eof ||
                nextToken == TokenType.Colon)
            {
                // SUB call with no arguments
                var subName = Advance();
                return new CallSubStatement(subName.Lexeme, new List<IExpression>());
            }

            // If next is not = or ( followed by something then =, it's likely a SUB call
            // But we need to be careful with array access: A(1) = 5 is assignment
            // So check: identifier ( ... ) = -> assignment, otherwise could be SUB call
            if (nextToken != TokenType.Equal)
            {
                // Could be SUB call with args or array assignment
                // Peek further to determine
                if (nextToken == TokenType.LeftParen)
                {
                    // Find matching ) and check what follows
                    int parenDepth = 0;
                    int i = current + 1;
                    while (i < tokens.Count)
                    {
                        if (tokens[i].Type == TokenType.LeftParen) parenDepth++;
                        else if (tokens[i].Type == TokenType.RightParen)
                        {
                            parenDepth--;
                            if (parenDepth == 0)
                            {
                                i++;
                                break;
                            }
                        }
                        i++;
                    }
                    // After ), check what's next
                    if (i < tokens.Count)
                    {
                        var afterParen = tokens[i].Type;
                        if (afterParen == TokenType.Equal)
                        {
                            // Array assignment: A(1) = 5
                            return ParseImplicitLetStatement();
                        }
                        else if (afterParen == TokenType.Dot)
                        {
                            // Field access: A(1).field = 5
                            return ParseImplicitLetStatement();
                        }
                    }
                    // Otherwise it's a SUB call: SubName(arg1, arg2)
                    return ParseImplicitSubCall();
                }
                else
                {
                    // SUB call with space-separated args: SubName arg1, arg2
                    return ParseImplicitSubCall();
                }
            }

            return ParseImplicitLetStatement();
        }

        throw Error($"Unexpected token '{Peek().Lexeme}'");
    }

    private IStatement ParsePrintStatement()
    {
        IExpression? fileNumber = null;

        // Check for PRINT# (file output)
        if (Match(TokenType.Hash))
        {
            fileNumber = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after file number");
        }

        // Check for PRINT USING or PRINT# USING
        if (Match(TokenType.Using))
        {
            var format = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after format in PRINT USING");
            var expressions = ParseExpressionList();
            return new PrintUsingStatement(format, expressions, fileNumber);
        }

        if (fileNumber != null)
        {
            var expressions = ParseExpressionList();
            return new PrintFileStatement(fileNumber, expressions);
        }

        var exprs = new List<IExpression>();

        if (Check(TokenType.Eol) || IsAtEnd() || Check(TokenType.Colon) || Check(TokenType.Else))
        {
            return new PrintStatement(exprs);
        }

        exprs.Add(ParseExpression());

        while (Match(TokenType.Semicolon) || Match(TokenType.Comma))
        {
            if (Check(TokenType.Eol) || IsAtEnd() || Check(TokenType.Colon) || Check(TokenType.Else))
            {
                break;
            }
            exprs.Add(ParseExpression());
        }

        return new PrintStatement(exprs);
    }

    private LetStatement ParseLetStatement()
    {
        var name = Consume(TokenType.Identifier, "Expected variable name");

        // Check for array assignment
        IReadOnlyList<IExpression>? indices = null;
        if (Match(TokenType.LeftParen))
        {
            indices = ParseArgumentList();
            Consume(TokenType.RightParen, "Expected ')' after array indices");
        }

        Consume(TokenType.Equal, "Expected '=' after variable name");
        var value = ParseExpression();
        return new LetStatement(name, value, indices);
    }

    private IStatement ParseImplicitLetStatement()
    {
        var name = Advance();

        // Check for array assignment
        IReadOnlyList<IExpression>? indices = null;
        if (Match(TokenType.LeftParen))
        {
            indices = ParseArgumentList();
            Consume(TokenType.RightParen, "Expected ')' after array indices");
        }

        // Check for field access (e.g., BCoor(i).XCoor = value)
        if (Match(TokenType.Dot))
        {
            IExpression target;
            if (indices != null)
            {
                target = new ArrayAccessExpression(name, indices);
            }
            else
            {
                target = new VariableExpression(name);
            }

            // Parse field chain (could have multiple dots)
            while (true)
            {
                var fieldName = Consume(TokenType.Identifier, "Expected field name after '.'");
                target = new FieldAccessExpression(target, fieldName);

                if (!Match(TokenType.Dot))
                {
                    break;
                }
            }

            Consume(TokenType.Equal, "Expected '=' after field access");
            var fieldValue = ParseExpression();
            return new FieldAssignStatement(target, fieldValue);
        }

        Consume(TokenType.Equal, "Expected '=' after variable name");
        var value = ParseExpression();
        return new LetStatement(name, value, indices);
    }

    private CallSubStatement ParseImplicitSubCall()
    {
        // SUB call without CALL keyword: SubName arg1, arg2 or SubName(arg1, arg2)
        var subName = Advance();
        var arguments = new List<IExpression>();

        // Check for parenthesized arguments
        if (Match(TokenType.LeftParen))
        {
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after arguments");
        }
        // Check for space-separated arguments (until EOL, REM, colon, or ELSE)
        else if (!Check(TokenType.Eol) && !Check(TokenType.Rem) && !Check(TokenType.Colon) && !Check(TokenType.Else) && !IsAtEnd())
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }

        return new CallSubStatement(subName.Lexeme, arguments);
    }

    private RemStatement ParseRemStatement()
    {
        string comment = Previous().Literal?.ToString() ?? "";
        return new RemStatement(comment);
    }

    private IStatement ParseGotoStatement()
    {
        // GOTO can target a line number or a label
        if (Check(TokenType.Number))
        {
            var lineNumToken = Advance();
            int targetLine = (int)(double)lineNumToken.Literal!;
            return new GotoStatement(targetLine);
        }
        else if (Check(TokenType.Identifier))
        {
            var labelToken = Advance();
            return new GotoLabelStatement(labelToken.Lexeme);
        }
        throw Error("Expected line number or label after GOTO");
    }

    private IfStatement ParseIfStatement()
    {
        var condition = ParseExpression();
        Consume(TokenType.Then, "Expected THEN after IF condition");

        IStatement thenBranch;
        IStatement? elseBranch = null;

        // Check for block-IF (THEN followed by EOL or REM)
        if (Check(TokenType.Eol) || Check(TokenType.Rem) || IsAtEnd())
        {
            // Block IF - return a placeholder, will be handled by interpreter
            // by scanning until END IF or ELSE
            thenBranch = new BlockIfPlaceholder();
        }
        // IF condition THEN linenum  OR  IF condition THEN statement
        else if (Check(TokenType.Number))
        {
            var lineNumToken = Advance();
            int targetLine = (int)(double)lineNumToken.Literal!;
            thenBranch = new GotoStatement(targetLine);
        }
        else
        {
            thenBranch = ParseStatement();

            // Check for ELSE on same line
            if (Match(TokenType.Else))
            {
                if (Check(TokenType.Number))
                {
                    var lineNumToken = Advance();
                    int targetLine = (int)(double)lineNumToken.Literal!;
                    elseBranch = new GotoStatement(targetLine);
                }
                else
                {
                    elseBranch = ParseStatement();
                }
            }
        }

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private ForStatement ParseForStatement()
    {
        var variable = Consume(TokenType.Identifier, "Expected variable after FOR");
        Consume(TokenType.Equal, "Expected '=' in FOR statement");
        var start = ParseExpression();
        Consume(TokenType.To, "Expected TO in FOR statement");
        var end = ParseExpression();

        IExpression? step = null;
        if (Match(TokenType.Step))
        {
            step = ParseExpression();
        }

        return new ForStatement(variable, start, end, step);
    }

    private NextStatement ParseNextStatement()
    {
        Token? variable = null;
        if (Check(TokenType.Identifier))
        {
            variable = Advance();
        }
        return new NextStatement(variable);
    }

    private WhileStatement ParseWhileStatement()
    {
        var condition = ParseExpression();
        return new WhileStatement(condition);
    }

    private IStatement ParseGosubStatement()
    {
        // GOSUB can target a line number or a label
        if (Check(TokenType.Number))
        {
            var lineNumToken = Advance();
            int targetLine = (int)(double)lineNumToken.Literal!;
            return new GosubStatement(targetLine);
        }
        else if (Check(TokenType.Identifier))
        {
            var labelToken = Advance();
            return new GosubLabelStatement(labelToken.Lexeme);
        }
        throw Error("Expected line number or label after GOSUB");
    }

    private IStatement ParseInputStatement()
    {
        // Check for INPUT# (file input)
        if (Match(TokenType.Hash))
        {
            var fileNumber = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after file number");

            var fileVars = new List<Token>();
            fileVars.Add(Consume(TokenType.Identifier, "Expected variable in INPUT# statement"));

            while (Match(TokenType.Comma))
            {
                fileVars.Add(Consume(TokenType.Identifier, "Expected variable in INPUT# statement"));
            }

            return new InputFileStatement(fileNumber, fileVars);
        }

        string? prompt = null;

        // Check for optional prompt
        if (Check(TokenType.String))
        {
            prompt = (string)Advance().Literal!;
            // Expect semicolon or comma after prompt
            if (!Match(TokenType.Semicolon) && !Match(TokenType.Comma))
            {
                throw Error("Expected ';' or ',' after INPUT prompt");
            }
        }

        var variables = new List<Token>();
        variables.Add(Consume(TokenType.Identifier, "Expected variable in INPUT statement"));

        while (Match(TokenType.Comma))
        {
            variables.Add(Consume(TokenType.Identifier, "Expected variable in INPUT statement"));
        }

        return new InputStatement(prompt, variables);
    }

    private DimStatement ParseDimStatement()
    {
        // DIM [SHARED] array(dims) [AS TypeName] [, array(dims), ...]
        bool isShared = Match(TokenType.Shared);
        var declarations = new List<ArrayDeclaration>();

        do
        {
            var name = Consume(TokenType.Identifier, "Expected array name in DIM statement");
            string? asType = null;

            // Check for AS type without array (e.g., DIM x AS STRING)
            if (Match(TokenType.As))
            {
                var typeName = Consume(TokenType.Identifier, "Expected type name after AS");
                asType = typeName.Lexeme;
                declarations.Add(new ArrayDeclaration(name, new List<IExpression>(), asType));
                continue;
            }

            if (Match(TokenType.LeftParen))
            {
                var dimensions = ParseDimDimensions();
                Consume(TokenType.RightParen, "Expected ')' after dimensions");

                // Check for AS type after array dimensions (e.g., DIM x(10) AS XYPoint)
                if (Match(TokenType.As))
                {
                    var typeName = Consume(TokenType.Identifier, "Expected type name after AS");
                    asType = typeName.Lexeme;
                }

                declarations.Add(new ArrayDeclaration(name, dimensions, asType));
            }
            else
            {
                // No dimensions - simple variable declaration
                declarations.Add(new ArrayDeclaration(name, new List<IExpression>()));
            }
        } while (Match(TokenType.Comma));

        return new DimStatement(declarations, isShared);
    }

    private List<IExpression> ParseDimDimensions()
    {
        // Parse array dimensions with optional "lower TO upper" syntax
        var dimensions = new List<IExpression>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                var lower = ParseExpression();

                // Check for TO keyword (e.g., 0 TO 30)
                if (Match(TokenType.To))
                {
                    var upper = ParseExpression();
                    // For now, we only use the upper bound (like traditional BASIC)
                    // The lower bound is stored for potential later use
                    dimensions.Add(upper);
                }
                else
                {
                    dimensions.Add(lower);
                }
            } while (Match(TokenType.Comma));
        }

        return dimensions;
    }

    private DataStatement ParseDataStatement()
    {
        string rawData = Previous().Literal?.ToString() ?? "";
        return new DataStatement(rawData);
    }

    private ReadStatement ParseReadStatement()
    {
        var targets = new List<ReadTarget>();
        targets.Add(ParseReadTarget());

        while (Match(TokenType.Comma))
        {
            targets.Add(ParseReadTarget());
        }

        return new ReadStatement(targets);
    }

    private ReadTarget ParseReadTarget()
    {
        var name = Consume(TokenType.Identifier, "Expected variable in READ statement");
        IReadOnlyList<IExpression>? indices = null;

        if (Match(TokenType.LeftParen))
        {
            var indexList = new List<IExpression>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    indexList.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after array indices");
            indices = indexList;
        }

        return new ReadTarget(name, indices);
    }

    private RestoreStatement ParseRestoreStatement()
    {
        int? targetLine = null;
        string? targetLabel = null;

        if (Check(TokenType.Number))
        {
            targetLine = (int)(double)Advance().Literal!;
        }
        else if (Check(TokenType.Identifier))
        {
            targetLabel = Advance().Lexeme;
        }

        return new RestoreStatement(targetLine, targetLabel);
    }

    private IStatement ParseOnStatement()
    {
        // ON ERROR GOTO line or ON expr GOTO/GOSUB lines
        if (Match(TokenType.Error))
        {
            Consume(TokenType.Goto, "Expected GOTO after ON ERROR");
            int? targetLine = null;
            string? targetLabel = null;

            if (Check(TokenType.Number))
            {
                targetLine = (int)(double)Advance().Literal!;
                if (targetLine == 0) targetLine = null; // ON ERROR GOTO 0 disables error handling
            }
            else if (Check(TokenType.Identifier))
            {
                targetLabel = Advance().Lexeme;
            }

            return new OnErrorStatement(targetLine, targetLabel);
        }

        var selector = ParseExpression();

        bool isGosub = false;
        if (Match(TokenType.Goto))
        {
            isGosub = false;
        }
        else if (Match(TokenType.Gosub))
        {
            isGosub = true;
        }
        else
        {
            throw Error("Expected GOTO or GOSUB after ON");
        }

        var targets = new List<int>();
        targets.Add((int)(double)Consume(TokenType.Number, "Expected line number").Literal!);

        while (Match(TokenType.Comma))
        {
            targets.Add((int)(double)Consume(TokenType.Number, "Expected line number").Literal!);
        }

        return new OnGotoStatement(selector, targets, isGosub);
    }

    private SwapStatement ParseSwapStatement()
    {
        var first = Consume(TokenType.Identifier, "Expected variable in SWAP statement");
        Consume(TokenType.Comma, "Expected ',' in SWAP statement");
        var second = Consume(TokenType.Identifier, "Expected variable in SWAP statement");
        return new SwapStatement(first, second);
    }

    private ScreenStatement ParseScreenStatement()
    {
        // SCREEN mode [,[colorswitch] [,[apage] [,vpage]]]
        var mode = ParseExpression();
        IExpression? colorSwitch = null;
        IExpression? activePage = null;
        IExpression? visualPage = null;

        if (Match(TokenType.Comma))
        {
            // ColorSwitch (can be empty)
            if (!Check(TokenType.Comma) && !IsAtEnd() && !IsStatementEnd())
            {
                colorSwitch = ParseExpression();
            }

            if (Match(TokenType.Comma))
            {
                // ActivePage (can be empty)
                if (!Check(TokenType.Comma) && !IsAtEnd() && !IsStatementEnd())
                {
                    activePage = ParseExpression();
                }

                if (Match(TokenType.Comma))
                {
                    // VisualPage
                    if (!IsAtEnd() && !IsStatementEnd())
                    {
                        visualPage = ParseExpression();
                    }
                }
            }
        }

        return new ScreenStatement(mode, colorSwitch, activePage, visualPage);
    }

    private bool IsStatementEnd()
    {
        return Check(TokenType.Colon) || Check(TokenType.Eol) || Check(TokenType.Eof);
    }

    private PsetStatement ParsePsetStatement(bool isPreset)
    {
        // PSET (x, y) [, color]  or  PSET STEP(x, y) [, color]
        Consume(TokenType.LeftParen, "Expected '(' after PSET/PRESET");
        var x = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in PSET/PRESET");
        var y = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after coordinates");

        IExpression? color = null;
        if (Match(TokenType.Comma))
        {
            color = ParseExpression();
        }

        return new PsetStatement(x, y, color, isPreset);
    }

    private LineStatement ParseLineStatement()
    {
        // LINE [(x1,y1)]-(x2,y2) [,[color][,B[F]]]
        IExpression? x1 = null, y1 = null;

        // Check for optional starting point
        if (Match(TokenType.LeftParen))
        {
            x1 = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' in LINE coordinates");
            y1 = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after starting coordinates");
        }

        Consume(TokenType.Minus, "Expected '-' in LINE statement");
        Consume(TokenType.LeftParen, "Expected '(' before end coordinates");
        var x2 = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in LINE coordinates");
        var y2 = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after end coordinates");

        IExpression? color = null;
        bool isBox = false;
        bool isFilled = false;

        if (Match(TokenType.Comma))
        {
            // Check for B or BF immediately (no color specified)
            if (Check(TokenType.Identifier))
            {
                var ident = Peek().Lexeme.ToUpperInvariant();
                if (ident == "B")
                {
                    Advance();
                    isBox = true;
                }
                else if (ident == "BF")
                {
                    Advance();
                    isBox = true;
                    isFilled = true;
                }
                else
                {
                    // It's a color variable
                    color = ParseExpression();
                }
            }
            else if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
            {
                // Parse color expression
                color = ParseExpression();
            }

            // Check for second comma and B/BF
            if (Match(TokenType.Comma))
            {
                if (Check(TokenType.Identifier))
                {
                    var boxToken = Advance();
                    if (boxToken.Lexeme.Equals("B", StringComparison.OrdinalIgnoreCase))
                    {
                        isBox = true;
                    }
                    else if (boxToken.Lexeme.Equals("BF", StringComparison.OrdinalIgnoreCase))
                    {
                        isBox = true;
                        isFilled = true;
                    }
                }
            }
        }

        return new LineStatement(x1, y1, x2, y2, color, isBox, isFilled);
    }

    private CircleStatement ParseCircleStatement()
    {
        // CIRCLE (cx, cy), radius [,[color][,[start][,[end][,aspect]]]]
        Consume(TokenType.LeftParen, "Expected '(' after CIRCLE");
        var cx = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in CIRCLE");
        var cy = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after center coordinates");
        Consume(TokenType.Comma, "Expected ',' before radius");
        var radius = ParseExpression();

        IExpression? color = null, start = null, end = null, aspect = null;

        if (Match(TokenType.Comma))
        {
            if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
            {
                color = ParseExpression();
            }

            if (Match(TokenType.Comma))
            {
                if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
                {
                    start = ParseExpression();
                }

                if (Match(TokenType.Comma))
                {
                    if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
                    {
                        end = ParseExpression();
                    }

                    if (Match(TokenType.Comma))
                    {
                        aspect = ParseExpression();
                    }
                }
            }
        }

        return new CircleStatement(cx, cy, radius, color, start, end, aspect);
    }

    private PaintStatement ParsePaintStatement()
    {
        // PAINT (x, y) [,[fillcolor][,bordercolor]]
        Consume(TokenType.LeftParen, "Expected '(' after PAINT");
        var x = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in PAINT");
        var y = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after coordinates");

        IExpression? fillColor = null, borderColor = null;

        if (Match(TokenType.Comma))
        {
            if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
            {
                fillColor = ParseExpression();
            }

            if (Match(TokenType.Comma))
            {
                borderColor = ParseExpression();
            }
        }

        return new PaintStatement(x, y, fillColor, borderColor);
    }

    private DrawStatement ParseDrawStatement()
    {
        // DRAW command-string
        var commands = ParseExpression();
        return new DrawStatement(commands);
    }

    private ColorStatement ParseColorStatement()
    {
        // COLOR [foreground][,[background][,border]]
        IExpression? foreground = null, background = null, border = null;

        if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
        {
            foreground = ParseExpression();
        }

        if (Match(TokenType.Comma))
        {
            if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
            {
                background = ParseExpression();
            }

            if (Match(TokenType.Comma))
            {
                border = ParseExpression();
            }
        }

        return new ColorStatement(foreground, background, border);
    }

    private LocateStatement ParseLocateStatement()
    {
        // LOCATE [row][,[col][,cursor]]
        IExpression? row = null, col = null, cursor = null;

        if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
        {
            row = ParseExpression();
        }

        if (Match(TokenType.Comma))
        {
            if (!Check(TokenType.Comma) && !Check(TokenType.Eol) && !IsAtEnd())
            {
                col = ParseExpression();
            }

            if (Match(TokenType.Comma))
            {
                cursor = ParseExpression();
            }
        }

        return new LocateStatement(row, col, cursor);
    }

    // File I/O parse methods

    private OpenStatement ParseOpenStatement()
    {
        // OPEN filename FOR mode AS #filenumber [LEN = recordlength]
        // or OPEN "mode", #filenumber, filename (older syntax)
        var fileName = ParseExpression();

        Consume(TokenType.For, "Expected FOR after filename in OPEN");

        Ast.FileMode mode;
        if (Match(TokenType.Input))
        {
            mode = Ast.FileMode.Input;
        }
        else if (Match(TokenType.Output))
        {
            mode = Ast.FileMode.Output;
        }
        else if (Match(TokenType.Append))
        {
            mode = Ast.FileMode.Append;
        }
        else if (Match(TokenType.Random))
        {
            mode = Ast.FileMode.Random;
        }
        else
        {
            throw Error("Expected INPUT, OUTPUT, APPEND, or RANDOM in OPEN");
        }

        Consume(TokenType.As, "Expected AS in OPEN statement");
        Match(TokenType.Hash); // # is optional
        var fileNumber = ParseExpression();

        // Optional LEN = recordlength for random access files
        IExpression? recordLength = null;
        if (Check(TokenType.Identifier) && Peek().Lexeme.Equals("LEN", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // consume LEN identifier
            Consume(TokenType.Equal, "Expected '=' after LEN");
            recordLength = ParseExpression();
        }

        return new OpenStatement(fileName, mode, fileNumber, recordLength);
    }

    private CloseStatement ParseCloseStatement()
    {
        // CLOSE [#filenumber [, #filenumber ...]]
        if (Check(TokenType.Eol) || IsAtEnd())
        {
            return new CloseStatement(null); // Close all files
        }

        var fileNumbers = new List<IExpression>();
        Match(TokenType.Hash); // # is optional
        fileNumbers.Add(ParseExpression());

        while (Match(TokenType.Comma))
        {
            Match(TokenType.Hash); // # is optional
            fileNumbers.Add(ParseExpression());
        }

        return new CloseStatement(fileNumbers);
    }

    private WriteFileStatement ParseWriteFileStatement()
    {
        // WRITE #filenumber, expression [, expression ...]
        Consume(TokenType.Hash, "Expected '#' after WRITE");
        var fileNumber = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' after file number");
        var expressions = ParseExpressionList();

        return new WriteFileStatement(fileNumber, expressions);
    }

    private KillStatement ParseKillStatement()
    {
        // KILL filename
        var fileName = ParseExpression();
        return new KillStatement(fileName);
    }

    private NameStatement ParseNameStatement()
    {
        // NAME oldname AS newname
        var oldName = ParseExpression();
        Consume(TokenType.As, "Expected AS in NAME statement");
        var newName = ParseExpression();
        return new NameStatement(oldName, newName);
    }

    private FilesStatement ParseFilesStatement()
    {
        // FILES [filespec]
        IExpression? pattern = null;
        if (!Check(TokenType.Eol) && !IsAtEnd())
        {
            pattern = ParseExpression();
        }
        return new FilesStatement(pattern);
    }

    // Additional statement parse methods

    private RandomizeStatement ParseRandomizeStatement()
    {
        // RANDOMIZE [seed]
        IExpression? seed = null;
        if (!Check(TokenType.Eol) && !IsAtEnd() && !Check(TokenType.Colon) && !Check(TokenType.Else))
        {
            seed = ParseExpression();
        }
        return new RandomizeStatement(seed);
    }

    private IStatement ParseLineStatementOrLineInput()
    {
        // LINE INPUT or LINE graphics
        if (Match(TokenType.Input))
        {
            return ParseLineInputStatement();
        }
        // Graphics LINE statement
        return ParseLineStatement();
    }

    private LineInputStatement ParseLineInputStatement()
    {
        // LINE INPUT [prompt;] variable
        // or LINE INPUT# filenumber, variable
        if (Match(TokenType.Hash))
        {
            var fileNumber = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after file number");
            var fileVar = Consume(TokenType.Identifier, "Expected variable in LINE INPUT#");
            return new LineInputStatement(null, fileVar); // Note: this is simplified
        }

        string? prompt = null;
        if (Check(TokenType.String))
        {
            prompt = (string)Advance().Literal!;
            Match(TokenType.Semicolon);
        }

        var variable = Consume(TokenType.Identifier, "Expected variable in LINE INPUT");
        return new LineInputStatement(prompt, variable);
    }

    private IStatement ParseDefStatement()
    {
        // DEF SEG [= segment] or DEF FNname(params) = expression
        if (Match(TokenType.Seg))
        {
            // DEF SEG [= segment]
            IExpression? segment = null;
            if (Match(TokenType.Equal))
            {
                segment = ParseExpression();
            }
            return new DefSegStatement(segment);
        }

        Token name;

        // DEF FN name(params) = expression  OR  DEF FNname(params) = expression
        if (Match(TokenType.Fn))
        {
            // DEF FN name - FN is separate keyword
            name = Consume(TokenType.Identifier, "Expected function name after DEF FN");
        }
        else if (Check(TokenType.Identifier) && Peek().Lexeme.StartsWith("FN", StringComparison.OrdinalIgnoreCase))
        {
            // DEF FNname - FN is part of identifier (e.g., FnRan)
            name = Advance();
        }
        else
        {
            throw Error("Expected FN or SEG after DEF");
        }

        var parameters = new List<Token>();
        if (Match(TokenType.LeftParen))
        {
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name"));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");
        }

        Consume(TokenType.Equal, "Expected '=' in DEF FN");
        var body = ParseExpression();

        return new DefFnStatement(name, parameters, body);
    }

    private WidthStatement ParseWidthStatement()
    {
        // WIDTH width  or  WIDTH device, width
        var first = ParseExpression();

        if (Match(TokenType.Comma))
        {
            var second = ParseExpression();
            return new WidthStatement(first, second);
        }

        return new WidthStatement(null, first);
    }

    private SoundStatement ParseSoundStatement()
    {
        // SOUND frequency, duration
        var frequency = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in SOUND statement");
        var duration = ParseExpression();
        return new SoundStatement(frequency, duration);
    }

    private PlayStatement ParsePlayStatement()
    {
        // PLAY string
        var commands = ParseExpression();
        return new PlayStatement(commands);
    }

    private ErrorStatement ParseErrorStatement()
    {
        // ERROR code
        var errorCode = ParseExpression();
        return new ErrorStatement(errorCode);
    }

    private ResumeStatement ParseResumeStatement()
    {
        // RESUME [NEXT | line]
        if (Match(TokenType.Next))
        {
            return new ResumeStatement(ResumeType.ResumeNext);
        }

        if (Check(TokenType.Number))
        {
            int targetLine = (int)(double)Advance().Literal!;
            return new ResumeStatement(ResumeType.ResumeLine, targetLine);
        }

        return new ResumeStatement(ResumeType.Resume);
    }

    private FieldStatement ParseFieldStatement()
    {
        // FIELD #filenumber, width AS var$, width AS var$, ...
        Match(TokenType.Hash); // optional #
        var fileNumber = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' after file number");

        var fields = new List<FieldSpec>();
        do
        {
            var width = ParseExpression();
            Consume(TokenType.As, "Expected AS in FIELD statement");
            var variable = Consume(TokenType.Identifier, "Expected variable in FIELD statement");
            fields.Add(new FieldSpec(width, variable));
        } while (Match(TokenType.Comma));

        return new FieldStatement(fileNumber, fields);
    }

    private IStatement ParseGetStatement()
    {
        // GET (x1, y1)-(x2, y2), array  -- graphics
        // GET #filenumber [, recordnumber]  -- file

        // Graphics GET starts with (
        if (Check(TokenType.LeftParen))
        {
            // GET (x1, y1)-(x2, y2), array
            Consume(TokenType.LeftParen, "Expected '(' after GET");
            var x1 = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' in GET coordinates");
            var y1 = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after GET coordinates");
            Consume(TokenType.Minus, "Expected '-' between GET coordinates");
            Consume(TokenType.LeftParen, "Expected '(' for second GET coordinate");
            var x2 = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' in GET coordinates");
            var y2 = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after GET coordinates");
            Consume(TokenType.Comma, "Expected ',' after GET coordinates");
            var array = Consume(TokenType.Identifier, "Expected array name in GET");

            return new GetGraphicsStatement(x1, y1, x2, y2, array);
        }

        // File GET
        Match(TokenType.Hash); // optional #
        var fileNumber = ParseExpression();
        IExpression? recordNumber = null;
        if (Match(TokenType.Comma))
        {
            recordNumber = ParseExpression();
        }
        return new GetRecordStatement(fileNumber, recordNumber);
    }

    private IStatement ParsePutStatement()
    {
        // PUT (x, y), array, action  -- graphics
        // PUT #filenumber [, recordnumber]  -- file

        // Graphics PUT starts with (
        if (Check(TokenType.LeftParen))
        {
            // PUT (x, y), array [, action]
            Consume(TokenType.LeftParen, "Expected '(' after PUT");
            var x = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' in PUT coordinates");
            var y = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after PUT coordinates");
            Consume(TokenType.Comma, "Expected ',' after PUT coordinates");

            var array = Consume(TokenType.Identifier, "Expected array name in PUT");

            string action = "XOR"; // default
            if (Match(TokenType.Comma))
            {
                // Action can be PSET, PRESET, AND, OR, XOR
                if (Check(TokenType.Pset))
                {
                    Advance();
                    action = "PSET";
                }
                else if (Check(TokenType.Preset))
                {
                    Advance();
                    action = "PRESET";
                }
                else if (Check(TokenType.And))
                {
                    Advance();
                    action = "AND";
                }
                else if (Check(TokenType.Or))
                {
                    Advance();
                    action = "OR";
                }
                else if (Check(TokenType.Xor))
                {
                    Advance();
                    action = "XOR";
                }
                else if (Check(TokenType.Identifier))
                {
                    action = Advance().Lexeme.ToUpperInvariant();
                }
            }

            return new PutGraphicsStatement(x, y, array, action);
        }

        // File PUT
        Match(TokenType.Hash); // optional #
        var fileNumber = ParseExpression();
        IExpression? recordNumber = null;
        if (Match(TokenType.Comma))
        {
            recordNumber = ParseExpression();
        }
        return new PutRecordStatement(fileNumber, recordNumber);
    }

    private LsetStatement ParseLsetStatement()
    {
        // LSET var$ = value
        var variable = Consume(TokenType.Identifier, "Expected variable in LSET statement");
        Consume(TokenType.Equal, "Expected '=' in LSET statement");
        var value = ParseExpression();
        return new LsetStatement(variable, value);
    }

    private RsetStatement ParseRsetStatement()
    {
        // RSET var$ = value
        var variable = Consume(TokenType.Identifier, "Expected variable in RSET statement");
        Consume(TokenType.Equal, "Expected '=' in RSET statement");
        var value = ParseExpression();
        return new RsetStatement(variable, value);
    }

    private List<IExpression> ParseExpressionList()
    {
        var expressions = new List<IExpression>();
        expressions.Add(ParseExpression());

        while (Match(TokenType.Comma) || Match(TokenType.Semicolon))
        {
            if (Check(TokenType.Eol) || IsAtEnd())
            {
                break;
            }
            expressions.Add(ParseExpression());
        }

        return expressions;
    }

    private IExpression ParseExpression()
    {
        return ParseImp();
    }

    private IExpression ParseImp()
    {
        var expr = ParseEqv();

        while (Match(TokenType.Imp))
        {
            var op = Previous();
            var right = ParseEqv();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseEqv()
    {
        var expr = ParseXor();

        while (Match(TokenType.Eqv))
        {
            var op = Previous();
            var right = ParseXor();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseXor()
    {
        var expr = ParseOr();

        while (Match(TokenType.Xor))
        {
            var op = Previous();
            var right = ParseOr();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseOr()
    {
        var expr = ParseAnd();

        while (Match(TokenType.Or))
        {
            var op = Previous();
            var right = ParseAnd();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseAnd()
    {
        var expr = ParseNot();

        while (Match(TokenType.And))
        {
            var op = Previous();
            var right = ParseNot();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseNot()
    {
        if (Match(TokenType.Not))
        {
            var op = Previous();
            var right = ParseNot();
            return new UnaryExpression(op, right);
        }

        return ParseComparison();
    }

    private IExpression ParseComparison()
    {
        var expr = ParseAddition();

        while (Match(TokenType.Equal, TokenType.NotEqual, TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
        {
            var op = Previous();
            var right = ParseAddition();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseAddition()
    {
        var expr = ParseModulo();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous();
            var right = ParseModulo();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseModulo()
    {
        var expr = ParseIntegerDivision();

        while (Match(TokenType.Mod))
        {
            var op = Previous();
            var right = ParseIntegerDivision();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseIntegerDivision()
    {
        var expr = ParseMultiplication();

        while (Match(TokenType.Backslash))
        {
            var op = Previous();
            var right = ParseMultiplication();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseMultiplication()
    {
        var expr = ParsePower();

        while (Match(TokenType.Star, TokenType.Slash))
        {
            var op = Previous();
            var right = ParsePower();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParsePower()
    {
        var expr = ParseUnary();

        if (Match(TokenType.Caret))
        {
            var op = Previous();
            var right = ParsePower(); // Right associative
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseUnary()
    {
        if (Match(TokenType.Minus))
        {
            var op = Previous();
            var right = ParseUnary();
            return new UnaryExpression(op, right);
        }

        return ParsePrimary();
    }

    private IExpression ParsePrimary()
    {
        if (Match(TokenType.Number))
        {
            return new LiteralExpression(Previous().Literal);
        }

        if (Match(TokenType.String))
        {
            return new LiteralExpression(Previous().Literal);
        }

        // Handle FN user-defined function calls: FN name(args)
        if (Match(TokenType.Fn))
        {
            var fnName = Consume(TokenType.Identifier, "Expected function name after FN");
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var args = ParseArgumentList();
            Consume(TokenType.RightParen, "Expected ')' after arguments");
            return new CallExpression(fnName.Lexeme, args);
        }

        if (Match(TokenType.Identifier))
        {
            var name = Previous();
            IExpression expr;

            // Check if it's a function call or array access
            if (Match(TokenType.LeftParen))
            {
                var args = ParseArgumentList();
                Consume(TokenType.RightParen, "Expected ')' after arguments");

                // Check if it's a built-in function
                if (BuiltInFunctions.Contains(name.Lexeme))
                {
                    expr = new CallExpression(name.Lexeme, args);
                }
                else
                {
                    // Array access
                    expr = new ArrayAccessExpression(name, args);
                }
            }
            // Handle zero-argument functions that don't require parentheses
            else if (ZeroArgFunctions.Contains(name.Lexeme))
            {
                expr = new CallExpression(name.Lexeme, []);
            }
            else
            {
                expr = new VariableExpression(name);
            }

            // Handle field access (e.g., obj.field or array(i).field)
            while (Match(TokenType.Dot))
            {
                var fieldName = Consume(TokenType.Identifier, "Expected field name after '.'");
                expr = new FieldAccessExpression(expr, fieldName);
            }

            return expr;
        }

        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return new GroupingExpression(expr);
        }

        throw Error("Expected expression");
    }

    private List<IExpression> ParseArgumentList()
    {
        var args = new List<IExpression>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                args.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }

        return args;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.Eof;

    private Token Peek() => tokens[current];

    private Token Previous() => tokens[current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(message);
    }

    private void SkipNewlines()
    {
        while (Check(TokenType.Eol))
        {
            Advance();
        }
    }

    // QBasic-style statement parsers

    private ConstStatement ParseConstStatement()
    {
        // CONST name = value [, name = value, ...]
        var declarations = new List<ConstDeclaration>();
        do
        {
            var name = Consume(TokenType.Identifier, "Expected constant name");
            Consume(TokenType.Equal, "Expected '=' after constant name");
            var value = ParseExpression();
            declarations.Add(new ConstDeclaration(name, value));
        } while (Match(TokenType.Comma));

        return new ConstStatement(declarations);
    }

    private SleepStatement ParseSleepStatement()
    {
        // SLEEP [seconds]
        IExpression? seconds = null;
        if (!Check(TokenType.Eol) && !IsAtEnd() && !Check(TokenType.Colon) && !Check(TokenType.Else))
        {
            seconds = ParseExpression();
        }
        return new SleepStatement(seconds);
    }

    private SelectCaseStatement ParseSelectCaseStatement()
    {
        // SELECT CASE expression
        Consume(TokenType.Case, "Expected CASE after SELECT");
        var testExpression = ParseExpression();

        // Note: For line-numbered BASIC, SELECT CASE spans multiple lines
        // We'll store the SELECT CASE and handle matching at runtime
        return new SelectCaseStatement(testExpression, new List<CaseClause>());
    }

    private CaseClauseStatement ParseCaseClauseStatement()
    {
        // CASE value [, value, ...] | CASE IS <|>|<=|>=|= value | CASE value TO value | CASE ELSE
        if (Match(TokenType.Else))
        {
            return new CaseClauseStatement(null); // CASE ELSE
        }

        var matches = new List<CaseMatch>();
        do
        {
            // Check for CASE IS comparison
            if (Check(TokenType.Identifier) && Peek().Lexeme.Equals("IS", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // consume IS
                TokenType comparison = TokenType.Equal;
                if (Match(TokenType.Less))
                    comparison = Match(TokenType.Equal) ? TokenType.LessEqual : TokenType.Less;
                else if (Match(TokenType.Greater))
                    comparison = Match(TokenType.Equal) ? TokenType.GreaterEqual : TokenType.Greater;
                else if (Match(TokenType.LessEqual))
                    comparison = TokenType.LessEqual;
                else if (Match(TokenType.GreaterEqual))
                    comparison = TokenType.GreaterEqual;
                else if (Match(TokenType.NotEqual))
                    comparison = TokenType.NotEqual;
                else if (Match(TokenType.Equal))
                    comparison = TokenType.Equal;

                var value = ParseExpression();
                matches.Add(new CaseComparisonMatch(comparison, value));
            }
            else
            {
                var value = ParseExpression();

                // Check for TO (range match)
                if (Match(TokenType.To))
                {
                    var toValue = ParseExpression();
                    matches.Add(new CaseRangeMatch(value, toValue));
                }
                else
                {
                    matches.Add(new CaseValueMatch(value));
                }
            }
        } while (Match(TokenType.Comma));

        return new CaseClauseStatement(matches);
    }

    private LoopStatement ParseLoopStatement()
    {
        // LOOP [WHILE|UNTIL condition]
        IExpression? condition = null;
        bool isWhile = true;

        if (Match(TokenType.While))
        {
            condition = ParseExpression();
            isWhile = true;
        }
        else if (Match(TokenType.Until))
        {
            condition = ParseExpression();
            isWhile = false;
        }

        return new LoopStatement(condition, isWhile);
    }

    private DoLoopStatement ParseDoStatement()
    {
        // DO [WHILE|UNTIL condition]
        // ...
        // LOOP [WHILE|UNTIL condition]

        IExpression? condition = null;
        bool isDoWhile = true;
        bool conditionAtTop = false;

        if (Match(TokenType.While))
        {
            condition = ParseExpression();
            conditionAtTop = true;
            isDoWhile = true;
        }
        else if (Match(TokenType.Until))
        {
            condition = ParseExpression();
            conditionAtTop = true;
            isDoWhile = false;
        }

        // In line-numbered BASIC, we just store the DO header
        // LOOP will be matched at runtime
        return new DoLoopStatement(condition, isDoWhile, conditionAtTop, new List<IStatement>());
    }

    private ExitStatement ParseExitStatement()
    {
        // EXIT FOR|WHILE|DO|SUB|FUNCTION
        if (Match(TokenType.For))
            return new ExitStatement(ExitType.For);
        if (Match(TokenType.While))
            return new ExitStatement(ExitType.While);
        if (Match(TokenType.Do))
            return new ExitStatement(ExitType.Do);
        if (Match(TokenType.Sub))
            return new ExitStatement(ExitType.Sub);
        if (Match(TokenType.Function))
            return new ExitStatement(ExitType.Function);

        throw Error("Expected FOR, WHILE, DO, SUB, or FUNCTION after EXIT");
    }

    private DeclareStatement ParseDeclareStatement()
    {
        // DECLARE SUB name (params)
        // DECLARE FUNCTION name (params)
        bool isFunction = false;
        if (Match(TokenType.Sub))
        {
            isFunction = false;
        }
        else if (Match(TokenType.Function))
        {
            isFunction = true;
        }
        else
        {
            throw Error("Expected SUB or FUNCTION after DECLARE");
        }

        var name = Consume(TokenType.Identifier, "Expected procedure name");
        var parameters = ParseParameterList();

        return new DeclareStatement(isFunction, name.Lexeme, parameters);
    }

    private List<ParameterDeclaration> ParseParameterList()
    {
        var parameters = new List<ParameterDeclaration>();

        if (Match(TokenType.LeftParen))
        {
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    bool isByRef = true;
                    if (Match(TokenType.ByVal))
                    {
                        isByRef = false;
                    }
                    else
                    {
                        Match(TokenType.ByRef); // Optional BYREF
                    }

                    var paramName = Consume(TokenType.Identifier, "Expected parameter name");

                    // Check for array parameter (e.g., BCoor())
                    bool isArray = false;
                    if (Match(TokenType.LeftParen))
                    {
                        Consume(TokenType.RightParen, "Expected ')' after array parameter");
                        isArray = true;
                    }

                    // Skip AS type (we don't enforce types)
                    if (Match(TokenType.As))
                    {
                        // Accept ANY or any identifier as type name
                        if (!Check(TokenType.Identifier))
                        {
                            // Skip unknown token (could be ANY which isn't a keyword)
                            Advance();
                        }
                        else
                        {
                            Advance(); // consume type name
                        }
                    }

                    parameters.Add(new ParameterDeclaration(paramName, isByRef, isArray));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");
        }

        return parameters;
    }

    private SubStatement ParseSubStatement()
    {
        // SUB name [(params)]
        // ... body ...
        // END SUB
        var name = Consume(TokenType.Identifier, "Expected SUB name");
        var parameters = ParseParameterList();

        // For line-numbered BASIC, SUB body spans multiple lines
        // We'll collect statements until we hit END SUB
        // This is a simplified approach - actual body execution is handled at runtime
        return new SubStatement(name, parameters, new List<IStatement>());
    }

    private FunctionStatement ParseFunctionStatement()
    {
        // FUNCTION name [(params)]
        // ... body ...
        // END FUNCTION
        var name = Consume(TokenType.Identifier, "Expected FUNCTION name");
        var parameters = ParseParameterList();

        // For line-numbered BASIC, FUNCTION body spans multiple lines
        return new FunctionStatement(name, parameters, new List<IStatement>());
    }

    private CallSubStatement ParseCallStatement()
    {
        // CALL subname [(args)]
        var name = Consume(TokenType.Identifier, "Expected SUB name after CALL");
        var arguments = new List<IExpression>();

        if (Match(TokenType.LeftParen))
        {
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after arguments");
        }

        return new CallSubStatement(name.Lexeme, arguments);
    }

    private TypeStatement ParseTypeStatement()
    {
        // TYPE typename
        //   fieldname AS typename
        //   ...
        // END TYPE
        var name = Consume(TokenType.Identifier, "Expected type name after TYPE");

        // For line-numbered BASIC, TYPE spans multiple lines
        // The fields will be parsed on subsequent lines
        // We return an empty TypeStatement; fields are handled at runtime
        return new TypeStatement(name, new List<TypeField>());
    }

    private TypeFieldDeclStatement ParseTypeFieldDeclaration()
    {
        // fieldname AS typename [* length]
        var fieldName = Consume(TokenType.Identifier, "Expected field name");
        Consume(TokenType.As, "Expected AS after field name");

        var typeName = Consume(TokenType.Identifier, "Expected type name after AS");

        // Check for STRING * length
        int? stringLength = null;
        if (Match(TokenType.Star))
        {
            var lengthToken = Consume(TokenType.Number, "Expected string length after *");
            stringLength = (int)(double)lengthToken.Literal!;
        }

        return new TypeFieldDeclStatement(fieldName, typeName.Lexeme, stringLength);
    }

    private DefTypeStatement ParseDefTypeStatement(BasicVarType varType)
    {
        // DEFINT A-Z or DEFINT A
        var start = Consume(TokenType.Identifier, "Expected letter range after DEF type");
        char startLetter = char.ToUpper(start.Lexeme[0]);
        char endLetter = startLetter;

        if (Match(TokenType.Minus))
        {
            var end = Consume(TokenType.Identifier, "Expected end letter in range");
            endLetter = char.ToUpper(end.Lexeme[0]);
        }

        return new DefTypeStatement(startLetter, endLetter, varType);
    }

    private PaletteStatement ParsePaletteStatement()
    {
        // PALETTE [attribute, color]
        if (Check(TokenType.Eol) || Check(TokenType.Colon) || Check(TokenType.Else) || IsAtEnd())
        {
            return new PaletteStatement(null, null);
        }

        var attribute = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' after palette attribute");
        var color = ParseExpression();

        return new PaletteStatement(attribute, color);
    }

    private IStatement ParseViewStatement()
    {
        // VIEW PRINT [top TO bottom]
        if (Match(TokenType.Print))
        {
            if (Check(TokenType.Eol) || Check(TokenType.Colon) || Check(TokenType.Else) || IsAtEnd())
            {
                return new ViewPrintStatement(null, null);
            }

            var top = ParseExpression();
            Consume(TokenType.To, "Expected TO in VIEW PRINT");
            var bottom = ParseExpression();

            return new ViewPrintStatement(top, bottom);
        }

        // VIEW [SCREEN] - graphics viewport, not implemented
        throw Error("VIEW without PRINT not supported");
    }

    private RedimStatement ParseRedimStatement()
    {
        // REDIM [PRESERVE] arrayname(dimensions) [, ...]
        bool preserve = Match(TokenType.Preserve);

        var declarations = new List<ArrayDeclaration>();
        do
        {
            var name = Consume(TokenType.Identifier, "Expected array name in REDIM");
            Consume(TokenType.LeftParen, "Expected '(' after array name");

            var dimensions = new List<IExpression>();
            do
            {
                dimensions.Add(ParseExpression());
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expected ')' after dimensions");
            declarations.Add(new ArrayDeclaration(name, dimensions));
        } while (Match(TokenType.Comma));

        return new RedimStatement(declarations, preserve);
    }

    private PokeStatement ParsePokeStatement()
    {
        // POKE address, value
        var address = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' after address in POKE");
        var value = ParseExpression();

        return new PokeStatement(address, value);
    }
}

public class ParserException(string message) : Exception(message);
