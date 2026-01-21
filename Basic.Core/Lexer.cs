namespace Basic.Core;

public sealed class Lexer(string source)
{
    private readonly string sourceText = source;
    private readonly List<Token> tokens = [];
    private int start;
    private int current;
    private int currentLine = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PRINT"] = TokenType.Print,
        ["LET"] = TokenType.Let,
        ["REM"] = TokenType.Rem,
        ["RUN"] = TokenType.Run,
        ["GOTO"] = TokenType.Goto,
        ["IF"] = TokenType.If,
        ["THEN"] = TokenType.Then,
        ["ELSE"] = TokenType.Else,
        ["ELSEIF"] = TokenType.ElseIf,
        ["FOR"] = TokenType.For,
        ["TO"] = TokenType.To,
        ["STEP"] = TokenType.Step,
        ["NEXT"] = TokenType.Next,
        ["WHILE"] = TokenType.While,
        ["WEND"] = TokenType.Wend,
        ["GOSUB"] = TokenType.Gosub,
        ["RETURN"] = TokenType.Return,
        ["ON"] = TokenType.On,
        ["END"] = TokenType.End,
        ["STOP"] = TokenType.Stop,
        ["DIM"] = TokenType.Dim,
        ["INPUT"] = TokenType.Input,
        ["DATA"] = TokenType.Data,
        ["READ"] = TokenType.Read,
        ["RESTORE"] = TokenType.Restore,
        ["DEF"] = TokenType.Def,
        ["FN"] = TokenType.Fn,
        ["CLS"] = TokenType.Cls,
        ["SWAP"] = TokenType.Swap,
        ["SCREEN"] = TokenType.Screen,
        ["PSET"] = TokenType.Pset,
        ["PRESET"] = TokenType.Preset,
        ["LINE"] = TokenType.Line,
        ["CIRCLE"] = TokenType.Circle,
        ["PAINT"] = TokenType.Paint,
        ["DRAW"] = TokenType.Draw,
        ["COLOR"] = TokenType.Color,
        ["LOCATE"] = TokenType.Locate,
        ["BEEP"] = TokenType.Beep,
        ["OPEN"] = TokenType.Open,
        ["CLOSE"] = TokenType.Close,
        ["OUTPUT"] = TokenType.Output,
        ["APPEND"] = TokenType.Append,
        ["AS"] = TokenType.As,
        ["WRITE"] = TokenType.Write,
        ["NAME"] = TokenType.Name,
        ["KILL"] = TokenType.Kill,
        ["FILES"] = TokenType.Files,
        ["GET"] = TokenType.Get,
        ["PUT"] = TokenType.Put,
        ["RANDOM"] = TokenType.Random,
        ["LOAD"] = TokenType.Load,
        ["SAVE"] = TokenType.Save,
        ["NEW"] = TokenType.New,
        ["LIST"] = TokenType.List,
        ["AND"] = TokenType.And,
        ["OR"] = TokenType.Or,
        ["NOT"] = TokenType.Not,
        ["XOR"] = TokenType.Xor,
        ["EQV"] = TokenType.Eqv,
        ["IMP"] = TokenType.Imp,
        ["MOD"] = TokenType.Mod,
        ["RANDOMIZE"] = TokenType.Randomize,
        ["USING"] = TokenType.Using,
        ["ERROR"] = TokenType.Error,
        ["RESUME"] = TokenType.Resume,
        ["SOUND"] = TokenType.Sound,
        ["PLAY"] = TokenType.Play,
        ["TRON"] = TokenType.Tron,
        ["TROFF"] = TokenType.Troff,
        ["WIDTH"] = TokenType.Width,
        ["FIELD"] = TokenType.Field,
        ["LSET"] = TokenType.Lset,
        ["RSET"] = TokenType.Rset,
        ["CONST"] = TokenType.Const,
        ["SLEEP"] = TokenType.Sleep,
        ["SELECT"] = TokenType.Select,
        ["CASE"] = TokenType.Case,
        ["DECLARE"] = TokenType.Declare,
        ["SUB"] = TokenType.Sub,
        ["FUNCTION"] = TokenType.Function,
        ["EXIT"] = TokenType.Exit,
        ["SHARED"] = TokenType.Shared,
        ["STATIC"] = TokenType.Static,
        ["BYVAL"] = TokenType.ByVal,
        ["BYREF"] = TokenType.ByRef,
        ["DO"] = TokenType.Do,
        ["LOOP"] = TokenType.Loop,
        ["UNTIL"] = TokenType.Until,
        ["CALL"] = TokenType.Call,
        ["TYPE"] = TokenType.Type,
        ["DEFINT"] = TokenType.Defint,
        ["DEFLNG"] = TokenType.Deflng,
        ["DEFSNG"] = TokenType.Defsng,
        ["DEFDBL"] = TokenType.Defdbl,
        ["DEFSTR"] = TokenType.Defstr,
        ["PALETTE"] = TokenType.Palette,
        ["VIEW"] = TokenType.View,
        ["REDIM"] = TokenType.Redim,
        ["SEG"] = TokenType.Seg,
        ["POKE"] = TokenType.Poke,
        ["PRESERVE"] = TokenType.Preserve
    };

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(TokenType.Eof, "", null, currentLine));
        return tokens;
    }

    private void ScanToken()
    {
        char c = Advance();

        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '+': AddToken(TokenType.Plus); break;
            case '-': AddToken(TokenType.Minus); break;
            case '*': AddToken(TokenType.Star); break;
            case '/': AddToken(TokenType.Slash); break;
            case '\\': AddToken(TokenType.Backslash); break;
            case '^': AddToken(TokenType.Caret); break;
            case '=': AddToken(TokenType.Equal); break;
            case ',': AddToken(TokenType.Comma); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case ':': AddToken(TokenType.Colon); break;
            case '#': AddToken(TokenType.Hash); break;
            case '&':
                // &H = hex, &O = octal, or just & for Long suffix (handled with identifiers)
                if (char.ToUpper(Peek()) == 'H')
                {
                    Advance(); // consume H
                    ScanHexNumber();
                }
                else if (char.ToUpper(Peek()) == 'O')
                {
                    Advance(); // consume O
                    ScanOctalNumber();
                }
                else
                {
                    // Just & - treat as part of identifier or ignore
                    // In QBasic, & alone after a number means Long type
                    AddToken(TokenType.Identifier, "&");
                }
                break;
            case '<':
                if (Match('=')) AddToken(TokenType.LessEqual);
                else if (Match('>')) AddToken(TokenType.NotEqual);
                else AddToken(TokenType.Less);
                break;
            case '>':
                if (Match('=')) AddToken(TokenType.GreaterEqual);
                else AddToken(TokenType.Greater);
                break;

            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace
                break;

            case '\n':
                AddToken(TokenType.Eol);
                currentLine++;
                break;

            case '"':
                ScanString();
                break;

            case '\'':
                // Apostrophe starts a comment (like REM)
                while (Peek() != '\n' && !IsAtEnd())
                {
                    Advance();
                }
                string comment = sourceText[start..current];
                AddToken(TokenType.Rem, comment);
                break;

            case '.':
                // Could be a number like .5 or field access like obj.field
                if (char.IsDigit(Peek()))
                {
                    ScanNumber();
                }
                else
                {
                    AddToken(TokenType.Dot);
                }
                break;

            default:
                if (char.IsDigit(c))
                {
                    ScanNumber();
                }
                else if (char.IsLetter(c))
                {
                    ScanIdentifier();
                }
                else
                {
                    throw new LexerException($"Unexpected character '{c}' at line {currentLine}");
                }
                break;
        }
    }

    private void ScanString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                throw new LexerException($"Unterminated string at line {currentLine}");
            }
            Advance();
        }

        if (IsAtEnd())
        {
            throw new LexerException($"Unterminated string at line {currentLine}");
        }

        // Consume closing "
        Advance();

        // Trim quotes
        string value = sourceText[(start + 1)..(current - 1)];
        AddToken(TokenType.String, value);
    }

    private void ScanNumber()
    {
        // Handle leading dot case
        if (sourceText[start] == '.')
        {
            while (char.IsDigit(Peek()))
            {
                Advance();
            }
        }
        else
        {
            while (char.IsDigit(Peek()))
            {
                Advance();
            }

            // Look for decimal part
            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                // Consume the dot
                Advance();

                while (char.IsDigit(Peek()))
                {
                    Advance();
                }
            }
        }

        // Scientific notation
        if (Peek() == 'E' || Peek() == 'e')
        {
            Advance();
            if (Peek() == '+' || Peek() == '-')
            {
                Advance();
            }
            while (char.IsDigit(Peek()))
            {
                Advance();
            }
        }

        // Type suffix: # (double), ! (single), % (integer), & (long)
        if (Peek() == '#' || Peek() == '!' || Peek() == '%' || Peek() == '&')
        {
            Advance();
        }

        string text = sourceText[start..current];
        // Remove type suffix for parsing
        if (text.EndsWith('#') || text.EndsWith('!') || text.EndsWith('%') || text.EndsWith('&'))
        {
            text = text[..^1];
        }
        AddToken(TokenType.Number, double.Parse(text, System.Globalization.CultureInfo.InvariantCulture));
    }

    private void ScanHexNumber()
    {
        // Scan hex digits after &H
        while (IsHexDigit(Peek()))
        {
            Advance();
        }
        string hexText = sourceText[(start + 2)..current]; // Skip &H
        if (string.IsNullOrEmpty(hexText))
        {
            AddToken(TokenType.Number, 0.0);
        }
        else
        {
            long value = Convert.ToInt64(hexText, 16);
            AddToken(TokenType.Number, (double)value);
        }
    }

    private void ScanOctalNumber()
    {
        // Scan octal digits after &O
        while (Peek() >= '0' && Peek() <= '7')
        {
            Advance();
        }
        string octText = sourceText[(start + 2)..current]; // Skip &O
        if (string.IsNullOrEmpty(octText))
        {
            AddToken(TokenType.Number, 0.0);
        }
        else
        {
            long value = Convert.ToInt64(octText, 8);
            AddToken(TokenType.Number, (double)value);
        }
    }

    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
    }

    private void ScanIdentifier()
    {
        // Type suffixes: $ (string), % (integer), & (long), ! (single), # (double)
        while (char.IsLetterOrDigit(Peek()) || Peek() == '$' || Peek() == '%' || Peek() == '&' || Peek() == '!' || Peek() == '#')
        {
            Advance();
            char lastChar = sourceText[current - 1];
            // Type suffix ends identifier
            if (lastChar == '$' || lastChar == '%' || lastChar == '&' || lastChar == '!' || lastChar == '#')
            {
                break;
            }
        }

        string text = sourceText[start..current];

        // Check if it's a keyword (without type suffix)
        if (Keywords.TryGetValue(text, out TokenType type))
        {
            // REM and DATA consume rest of line
            if (type == TokenType.Rem)
            {
                while (Peek() != '\n' && !IsAtEnd())
                {
                    Advance();
                }
                string comment = sourceText[start..current];
                AddToken(TokenType.Rem, comment);
            }
            else if (type == TokenType.Data)
            {
                // DATA values go to end of line
                int dataStart = current;
                while (Peek() != '\n' && !IsAtEnd())
                {
                    Advance();
                }
                string dataValues = sourceText[dataStart..current].Trim();
                AddToken(TokenType.Data, dataValues);
            }
            else
            {
                AddToken(type);
            }
        }
        else
        {
            AddToken(TokenType.Identifier);
        }
    }

    private bool IsAtEnd() => current >= sourceText.Length;

    private char Advance() => sourceText[current++];

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (sourceText[current] != expected) return false;
        current++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : sourceText[current];

    private char PeekNext() => current + 1 >= sourceText.Length ? '\0' : sourceText[current + 1];

    private void AddToken(TokenType type) => AddToken(type, null);

    private void AddToken(TokenType type, object? literal)
    {
        string text = sourceText[start..current];
        tokens.Add(new Token(type, text, literal, currentLine));
    }
}

public class LexerException(string message) : Exception(message);
