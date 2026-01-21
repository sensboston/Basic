namespace Basic.Core;

public enum TokenType
{
    // Literals
    Number,
    String,

    // Identifiers
    Identifier,

    // Keywords
    Print,
    Let,
    Rem,
    Run,
    Goto,
    If,
    Then,
    Else,
    ElseIf,
    For,
    To,
    Step,
    Next,
    While,
    Wend,
    Gosub,
    Return,
    On,
    End,
    Stop,
    Dim,
    Input,
    Data,
    Read,
    Restore,
    Def,
    Fn,
    Cls,
    Swap,

    // Graphics keywords
    Screen,
    Pset,
    Preset,
    Line,
    Circle,
    Paint,
    Draw,
    Color,
    Locate,
    Beep,

    // File I/O keywords
    Open,
    Close,
    Output,
    Append,
    As,
    Write,
    Name,
    Kill,
    Files,
    Get,
    Put,
    Random,

    // Program management
    Load,
    Save,
    New,
    List,

    // Additional statements
    Randomize,
    Using,
    Error,
    Resume,
    Sound,
    Play,
    Tron,
    Troff,
    Width,
    Field,
    Lset,
    Rset,
    Const,
    Sleep,
    Select,
    Case,
    Declare,
    Sub,
    Function,
    Exit,
    Shared,
    Static,
    ByVal,
    ByRef,
    Do,
    Loop,
    Until,
    Wend2,  // Alternative to WEND for DO...LOOP
    Call,   // CALL statement for SUBs
    Type,   // TYPE ... END TYPE
    Defint,
    Deflng,
    Defsng,
    Defdbl,
    Defstr,
    Palette,
    View,
    Redim,
    Seg,    // DEF SEG
    Peek,
    Poke,
    Preserve,

    // Hash for file number
    Hash,

    // Logical operators
    And,
    Or,
    Not,
    Xor,
    Eqv,
    Imp,
    Mod,

    // Operators
    Plus,
    Minus,
    Star,
    Slash,
    Backslash,  // Integer division
    Caret,      // Power
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Delimiters
    LeftParen,
    RightParen,
    Comma,
    Semicolon,
    Colon,
    Dot,        // Field access for TYPE

    // Special
    Eol,
    Eof
}
