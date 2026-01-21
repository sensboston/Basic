namespace Basic.Core.Ast;

public sealed class PrintStatement(IReadOnlyList<IExpression> expressions) : IStatement
{
    public IReadOnlyList<IExpression> Expressions { get; } = expressions;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPrintStatement(this);
}

public sealed class LetStatement(Token name, IExpression value, IReadOnlyList<IExpression>? indices = null) : IStatement
{
    public Token Name { get; } = name;
    public IExpression Value { get; } = value;
    public IReadOnlyList<IExpression>? Indices { get; } = indices;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLetStatement(this);
}

public sealed class FieldAssignStatement(IExpression target, IExpression value) : IStatement
{
    public IExpression Target { get; } = target;
    public IExpression Value { get; } = value;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFieldAssignStatement(this);
}

public sealed class RemStatement(string comment) : IStatement
{
    public string Comment { get; } = comment;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitRemStatement(this);
}

public sealed class GotoStatement(int targetLine) : IStatement
{
    public int TargetLine { get; } = targetLine;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGotoStatement(this);
}

public sealed class IfStatement(IExpression condition, IStatement thenBranch, IStatement? elseBranch = null) : IStatement
{
    public IExpression Condition { get; } = condition;
    public IStatement ThenBranch { get; } = thenBranch;
    public IStatement? ElseBranch { get; } = elseBranch;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitIfStatement(this);
}

// Placeholder for block-IF (IF...THEN followed by newline, ended with END IF)
public sealed class BlockIfPlaceholder : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitBlockIfPlaceholder(this);
}

// END IF statement
public sealed class EndIfStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitEndIfStatement(this);
}

// ELSEIF statement
public sealed class ElseIfStatement(IExpression condition) : IStatement
{
    public IExpression Condition { get; } = condition;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitElseIfStatement(this);
}

// Standalone ELSE statement (for block-IF)
public sealed class ElseStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitElseStatement(this);
}

public sealed class ForStatement(Token variable, IExpression start, IExpression end, IExpression? step) : IStatement
{
    public Token Variable { get; } = variable;
    public IExpression Start { get; } = start;
    public IExpression End { get; } = end;
    public IExpression? Step { get; } = step;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitForStatement(this);
}

public sealed class NextStatement(Token? variable) : IStatement
{
    public Token? Variable { get; } = variable;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitNextStatement(this);
}

public sealed class WhileStatement(IExpression condition) : IStatement
{
    public IExpression Condition { get; } = condition;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitWhileStatement(this);
}

public sealed class WendStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitWendStatement(this);
}

public sealed class GosubStatement(int targetLine) : IStatement
{
    public int TargetLine { get; } = targetLine;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGosubStatement(this);
}

public sealed class ReturnStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitReturnStatement(this);
}

public sealed class EndStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitEndStatement(this);
}

public sealed class InputStatement(string? prompt, IReadOnlyList<Token> variables) : IStatement
{
    public string? Prompt { get; } = prompt;
    public IReadOnlyList<Token> Variables { get; } = variables;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitInputStatement(this);
}

public sealed class DimStatement(IReadOnlyList<ArrayDeclaration> declarations, bool isShared = false) : IStatement
{
    public IReadOnlyList<ArrayDeclaration> Declarations { get; } = declarations;
    public bool IsShared { get; } = isShared;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDimStatement(this);
}

public sealed class ArrayDeclaration(Token name, IReadOnlyList<IExpression> dimensions, string? asType = null)
{
    public Token Name { get; } = name;
    public IReadOnlyList<IExpression> Dimensions { get; } = dimensions;
    public string? AsType { get; } = asType;
}

public sealed class DataStatement(string rawData) : IStatement
{
    public string RawData { get; } = rawData;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDataStatement(this);
}

public sealed class ReadTarget(Token name, IReadOnlyList<IExpression>? indices = null)
{
    public Token Name { get; } = name;
    public IReadOnlyList<IExpression>? Indices { get; } = indices;
    public bool IsArray => Indices != null && Indices.Count > 0;
}

public sealed class ReadStatement(IReadOnlyList<ReadTarget> targets) : IStatement
{
    public IReadOnlyList<ReadTarget> Targets { get; } = targets;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitReadStatement(this);
}

public sealed class RestoreStatement(int? targetLine = null, string? targetLabel = null) : IStatement
{
    public int? TargetLine { get; } = targetLine;
    public string? TargetLabel { get; } = targetLabel;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitRestoreStatement(this);
}

public sealed class OnGotoStatement(IExpression selector, IReadOnlyList<int> targets, bool isGosub) : IStatement
{
    public IExpression Selector { get; } = selector;
    public IReadOnlyList<int> Targets { get; } = targets;
    public bool IsGosub { get; } = isGosub;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitOnGotoStatement(this);
}

public sealed class SwapStatement(Token first, Token second) : IStatement
{
    public Token First { get; } = first;
    public Token Second { get; } = second;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitSwapStatement(this);
}

public sealed class ClsStatement(IExpression? mode = null) : IStatement
{
    public IExpression? Mode { get; } = mode;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitClsStatement(this);
}

// Graphics statements

public sealed class ScreenStatement(
    IExpression mode,
    IExpression? colorSwitch = null,
    IExpression? activePage = null,
    IExpression? visualPage = null) : IStatement
{
    public IExpression Mode { get; } = mode;
    public IExpression? ColorSwitch { get; } = colorSwitch;
    public IExpression? ActivePage { get; } = activePage;
    public IExpression? VisualPage { get; } = visualPage;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitScreenStatement(this);
}

public sealed class PsetStatement(IExpression x, IExpression y, IExpression? color, bool isPreset) : IStatement
{
    public IExpression X { get; } = x;
    public IExpression Y { get; } = y;
    public IExpression? Color { get; } = color;
    public bool IsPreset { get; } = isPreset;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPsetStatement(this);
}

public sealed class LineStatement(
    IExpression? x1, IExpression? y1,
    IExpression x2, IExpression y2,
    IExpression? color, bool isBox, bool isFilled) : IStatement
{
    public IExpression? X1 { get; } = x1;
    public IExpression? Y1 { get; } = y1;
    public IExpression X2 { get; } = x2;
    public IExpression Y2 { get; } = y2;
    public IExpression? Color { get; } = color;
    public bool IsBox { get; } = isBox;
    public bool IsFilled { get; } = isFilled;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLineStatement(this);
}

public sealed class CircleStatement(
    IExpression cx, IExpression cy, IExpression radius,
    IExpression? color, IExpression? start, IExpression? end, IExpression? aspect) : IStatement
{
    public IExpression CX { get; } = cx;
    public IExpression CY { get; } = cy;
    public IExpression Radius { get; } = radius;
    public IExpression? Color { get; } = color;
    public IExpression? Start { get; } = start;
    public IExpression? End { get; } = end;
    public IExpression? Aspect { get; } = aspect;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitCircleStatement(this);
}

public sealed class PaintStatement(IExpression x, IExpression y, IExpression? fillColor, IExpression? borderColor) : IStatement
{
    public IExpression X { get; } = x;
    public IExpression Y { get; } = y;
    public IExpression? FillColor { get; } = fillColor;
    public IExpression? BorderColor { get; } = borderColor;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPaintStatement(this);
}

public sealed class DrawStatement(IExpression commands) : IStatement
{
    public IExpression Commands { get; } = commands;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDrawStatement(this);
}

public sealed class ColorStatement(IExpression? foreground, IExpression? background, IExpression? border) : IStatement
{
    public IExpression? Foreground { get; } = foreground;
    public IExpression? Background { get; } = background;
    public IExpression? Border { get; } = border;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitColorStatement(this);
}

public sealed class LocateStatement(IExpression? row, IExpression? col, IExpression? cursor) : IStatement
{
    public IExpression? Row { get; } = row;
    public IExpression? Col { get; } = col;
    public IExpression? Cursor { get; } = cursor;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLocateStatement(this);
}

public sealed class BeepStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitBeepStatement(this);
}

// File I/O statements

public enum FileMode { Input, Output, Append, Random }

public sealed class OpenStatement(IExpression fileName, FileMode mode, IExpression fileNumber, IExpression? recordLength = null) : IStatement
{
    public IExpression FileName { get; } = fileName;
    public FileMode Mode { get; } = mode;
    public IExpression FileNumber { get; } = fileNumber;
    public IExpression? RecordLength { get; } = recordLength;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitOpenStatement(this);
}

public sealed class CloseStatement(IReadOnlyList<IExpression>? fileNumbers) : IStatement
{
    public IReadOnlyList<IExpression>? FileNumbers { get; } = fileNumbers;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitCloseStatement(this);
}

public sealed class PrintFileStatement(IExpression fileNumber, IReadOnlyList<IExpression> expressions) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IReadOnlyList<IExpression> Expressions { get; } = expressions;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPrintFileStatement(this);
}

public sealed class InputFileStatement(IExpression fileNumber, IReadOnlyList<Token> variables) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IReadOnlyList<Token> Variables { get; } = variables;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitInputFileStatement(this);
}

public sealed class LineInputFileStatement(IExpression fileNumber, Token variable) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public Token Variable { get; } = variable;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLineInputFileStatement(this);
}

public sealed class WriteFileStatement(IExpression fileNumber, IReadOnlyList<IExpression> expressions) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IReadOnlyList<IExpression> Expressions { get; } = expressions;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitWriteFileStatement(this);
}

public sealed class KillStatement(IExpression fileName) : IStatement
{
    public IExpression FileName { get; } = fileName;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitKillStatement(this);
}

public sealed class NameStatement(IExpression oldName, IExpression newName) : IStatement
{
    public IExpression OldName { get; } = oldName;
    public IExpression NewName { get; } = newName;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitNameStatement(this);
}

public sealed class FilesStatement(IExpression? pattern) : IStatement
{
    public IExpression? Pattern { get; } = pattern;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFilesStatement(this);
}

// Additional statements

public sealed class RandomizeStatement(IExpression? seed) : IStatement
{
    public IExpression? Seed { get; } = seed;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitRandomizeStatement(this);
}

public sealed class LineInputStatement(string? prompt, Token variable) : IStatement
{
    public string? Prompt { get; } = prompt;
    public Token Variable { get; } = variable;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLineInputStatement(this);
}

public sealed class DefFnStatement(Token name, IReadOnlyList<Token> parameters, IExpression body) : IStatement
{
    public Token Name { get; } = name;
    public IReadOnlyList<Token> Parameters { get; } = parameters;
    public IExpression Body { get; } = body;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDefFnStatement(this);
}

public sealed class TronStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitTronStatement(this);
}

public sealed class TroffStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitTroffStatement(this);
}

public sealed class WidthStatement(IExpression? device, IExpression width) : IStatement
{
    public IExpression? Device { get; } = device;
    public IExpression Width { get; } = width;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitWidthStatement(this);
}

public sealed class SoundStatement(IExpression frequency, IExpression duration) : IStatement
{
    public IExpression Frequency { get; } = frequency;
    public IExpression Duration { get; } = duration;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitSoundStatement(this);
}

public sealed class PlayStatement(IExpression commands) : IStatement
{
    public IExpression Commands { get; } = commands;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPlayStatement(this);
}

public sealed class OnErrorStatement(int? targetLine, string? targetLabel = null) : IStatement
{
    public int? TargetLine { get; } = targetLine; // null means ON ERROR GOTO 0 (disable)
    public string? TargetLabel { get; } = targetLabel; // For label-based error handling

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitOnErrorStatement(this);
}

public sealed class ResumeStatement(ResumeType type, int? targetLine = null) : IStatement
{
    public ResumeType Type { get; } = type;
    public int? TargetLine { get; } = targetLine;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitResumeStatement(this);
}

public enum ResumeType { Resume, ResumeNext, ResumeLine }

public sealed class ErrorStatement(IExpression errorCode) : IStatement
{
    public IExpression ErrorCode { get; } = errorCode;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitErrorStatement(this);
}

public sealed class PrintUsingStatement(IExpression format, IReadOnlyList<IExpression> expressions, IExpression? fileNumber = null) : IStatement
{
    public IExpression Format { get; } = format;
    public IReadOnlyList<IExpression> Expressions { get; } = expressions;
    public IExpression? FileNumber { get; } = fileNumber;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPrintUsingStatement(this);
}

// Random access file statements

public sealed class FieldStatement(IExpression fileNumber, IReadOnlyList<FieldSpec> fields) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IReadOnlyList<FieldSpec> Fields { get; } = fields;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFieldStatement(this);
}

public sealed class FieldSpec(IExpression width, Token variable)
{
    public IExpression Width { get; } = width;
    public Token Variable { get; } = variable;
}

public sealed class GetRecordStatement(IExpression fileNumber, IExpression? recordNumber) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IExpression? RecordNumber { get; } = recordNumber;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGetRecordStatement(this);
}

public sealed class PutRecordStatement(IExpression fileNumber, IExpression? recordNumber) : IStatement
{
    public IExpression FileNumber { get; } = fileNumber;
    public IExpression? RecordNumber { get; } = recordNumber;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPutRecordStatement(this);
}

public sealed class LsetStatement(Token variable, IExpression value) : IStatement
{
    public Token Variable { get; } = variable;
    public IExpression Value { get; } = value;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLsetStatement(this);
}

public sealed class RsetStatement(Token variable, IExpression value) : IStatement
{
    public Token Variable { get; } = variable;
    public IExpression Value { get; } = value;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitRsetStatement(this);
}

public sealed class CompoundStatement(IReadOnlyList<IStatement> statements) : IStatement
{
    public IReadOnlyList<IStatement> Statements { get; } = statements;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitCompoundStatement(this);
}

// QBasic-style statements

public sealed class ConstStatement(IReadOnlyList<ConstDeclaration> declarations) : IStatement
{
    public IReadOnlyList<ConstDeclaration> Declarations { get; } = declarations;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitConstStatement(this);
}

public sealed class ConstDeclaration(Token name, IExpression value)
{
    public Token Name { get; } = name;
    public IExpression Value { get; } = value;
}

public sealed class SleepStatement(IExpression? seconds) : IStatement
{
    public IExpression? Seconds { get; } = seconds;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitSleepStatement(this);
}

public sealed class SelectCaseStatement(IExpression testExpression, IReadOnlyList<CaseClause> cases) : IStatement
{
    public IExpression TestExpression { get; } = testExpression;
    public IReadOnlyList<CaseClause> Cases { get; } = cases;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitSelectCaseStatement(this);
}

public sealed class CaseClause(IReadOnlyList<CaseMatch>? matches, IReadOnlyList<IStatement> statements)
{
    public IReadOnlyList<CaseMatch>? Matches { get; } = matches; // null = CASE ELSE
    public IReadOnlyList<IStatement> Statements { get; } = statements;
}

public abstract class CaseMatch { }

public sealed class CaseValueMatch(IExpression value) : CaseMatch
{
    public IExpression Value { get; } = value;
}

public sealed class CaseRangeMatch(IExpression from, IExpression to) : CaseMatch
{
    public IExpression From { get; } = from;
    public IExpression To { get; } = to;
}

public sealed class CaseComparisonMatch(TokenType comparison, IExpression value) : CaseMatch
{
    public TokenType Comparison { get; } = comparison; // <, >, <=, >=, =
    public IExpression Value { get; } = value;
}

public sealed class DoLoopStatement(IExpression? condition, bool isDoWhile, bool conditionAtTop, IReadOnlyList<IStatement> body) : IStatement
{
    public IExpression? Condition { get; } = condition;
    public bool IsDoWhile { get; } = isDoWhile; // true = WHILE, false = UNTIL
    public bool ConditionAtTop { get; } = conditionAtTop; // true = DO WHILE/UNTIL, false = LOOP WHILE/UNTIL
    public IReadOnlyList<IStatement> Body { get; } = body;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDoLoopStatement(this);
}

public sealed class ExitStatement(ExitType exitType) : IStatement
{
    public ExitType ExitType { get; } = exitType;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitExitStatement(this);
}

public enum ExitType { For, While, Do, Sub, Function }

public sealed class DeclareStatement(bool isFunction, string name, IReadOnlyList<ParameterDeclaration> parameters) : IStatement
{
    public bool IsFunction { get; } = isFunction;
    public string Name { get; } = name;
    public IReadOnlyList<ParameterDeclaration> Parameters { get; } = parameters;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDeclareStatement(this);
}

public sealed class ParameterDeclaration(Token name, bool isByRef = true, bool isArray = false)
{
    public Token Name { get; } = name;
    public bool IsByRef { get; } = isByRef;
    public bool IsArray { get; } = isArray;
}

public sealed class SubStatement(Token name, IReadOnlyList<ParameterDeclaration> parameters, IReadOnlyList<IStatement> body) : IStatement
{
    public Token Name { get; } = name;
    public IReadOnlyList<ParameterDeclaration> Parameters { get; } = parameters;
    public IReadOnlyList<IStatement> Body { get; } = body;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitSubStatement(this);
}

public sealed class FunctionStatement(Token name, IReadOnlyList<ParameterDeclaration> parameters, IReadOnlyList<IStatement> body) : IStatement
{
    public Token Name { get; } = name;
    public IReadOnlyList<ParameterDeclaration> Parameters { get; } = parameters;
    public IReadOnlyList<IStatement> Body { get; } = body;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFunctionStatement(this);
}

public sealed class CallSubStatement(string name, IReadOnlyList<IExpression> arguments) : IStatement
{
    public string Name { get; } = name;
    public IReadOnlyList<IExpression> Arguments { get; } = arguments;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitCallSubStatement(this);
}

public sealed class CaseClauseStatement(IReadOnlyList<CaseMatch>? matches) : IStatement
{
    public IReadOnlyList<CaseMatch>? Matches { get; } = matches; // null = CASE ELSE

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitCaseClauseStatement(this);
}

public sealed class EndSelectStatement : IStatement
{
    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitEndSelectStatement(this);
}

public sealed class LoopStatement(IExpression? condition, bool isWhile) : IStatement
{
    public IExpression? Condition { get; } = condition;
    public bool IsWhile { get; } = isWhile; // true = LOOP WHILE, false = LOOP UNTIL

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLoopStatement(this);
}

public sealed class LabelStatement(string label) : IStatement
{
    public string Label { get; } = label;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitLabelStatement(this);
}

public sealed class GotoLabelStatement(string label) : IStatement
{
    public string Label { get; } = label;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGotoLabelStatement(this);
}

public sealed class GosubLabelStatement(string label) : IStatement
{
    public string Label { get; } = label;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGosubLabelStatement(this);
}

// TYPE ... END TYPE for user-defined types
public sealed class TypeStatement(Token name, IReadOnlyList<TypeField> fields) : IStatement
{
    public Token Name { get; } = name;
    public IReadOnlyList<TypeField> Fields { get; } = fields;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitTypeStatement(this);
}

public sealed class TypeField(Token name, string typeName, int? stringLength = null)
{
    public Token Name { get; } = name;
    public string TypeName { get; } = typeName;
    public int? StringLength { get; } = stringLength; // For STRING * n
}

public sealed class TypeFieldDeclStatement(Token fieldName, string typeName, int? stringLength = null) : IStatement
{
    public Token FieldName { get; } = fieldName;
    public string TypeName { get; } = typeName;
    public int? StringLength { get; } = stringLength;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitTypeFieldDeclStatement(this);
}

public sealed class DefTypeStatement(char startLetter, char endLetter, BasicVarType varType) : IStatement
{
    public char StartLetter { get; } = startLetter;
    public char EndLetter { get; } = endLetter;
    public BasicVarType VarType { get; } = varType;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDefTypeStatement(this);
}

public enum BasicVarType { Integer, Long, Single, Double, String }

public sealed class PaletteStatement(IExpression? attribute, IExpression? color) : IStatement
{
    public IExpression? Attribute { get; } = attribute;
    public IExpression? Color { get; } = color;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPaletteStatement(this);
}

public sealed class ViewPrintStatement(IExpression? topRow, IExpression? bottomRow) : IStatement
{
    public IExpression? TopRow { get; } = topRow;
    public IExpression? BottomRow { get; } = bottomRow;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitViewPrintStatement(this);
}

public sealed class RedimStatement(IReadOnlyList<ArrayDeclaration> declarations, bool preserve) : IStatement
{
    public IReadOnlyList<ArrayDeclaration> Declarations { get; } = declarations;
    public bool Preserve { get; } = preserve;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitRedimStatement(this);
}

public sealed class DefSegStatement(IExpression? segment) : IStatement
{
    public IExpression? Segment { get; } = segment;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitDefSegStatement(this);
}

public sealed class PokeStatement(IExpression address, IExpression value) : IStatement
{
    public IExpression Address { get; } = address;
    public IExpression Value { get; } = value;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPokeStatement(this);
}

public sealed class PutGraphicsStatement(IExpression x, IExpression y, Token arrayName, string? action) : IStatement
{
    public IExpression X { get; } = x;
    public IExpression Y { get; } = y;
    public Token ArrayName { get; } = arrayName;
    public string? Action { get; } = action; // PSET, PRESET, AND, OR, XOR

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPutGraphicsStatement(this);
}

public sealed class GetGraphicsStatement(IExpression x1, IExpression y1, IExpression x2, IExpression y2, Token arrayName) : IStatement
{
    public IExpression X1 { get; } = x1;
    public IExpression Y1 { get; } = y1;
    public IExpression X2 { get; } = x2;
    public IExpression Y2 { get; } = y2;
    public Token ArrayName { get; } = arrayName;

    public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitGetGraphicsStatement(this);
}
