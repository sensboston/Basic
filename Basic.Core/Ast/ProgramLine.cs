namespace Basic.Core.Ast;

public sealed class ProgramLine(int lineNumber, IStatement statement)
{
    public int LineNumber { get; } = lineNumber;
    public IStatement Statement { get; } = statement;
}

public sealed class BasicProgram(IReadOnlyList<ProgramLine> lines)
{
    public IReadOnlyList<ProgramLine> Lines { get; } = lines;
}
