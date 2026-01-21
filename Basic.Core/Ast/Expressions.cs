namespace Basic.Core.Ast;

public sealed class BinaryExpression(IExpression left, Token op, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public Token Operator { get; } = op;
    public IExpression Right { get; } = right;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}

public sealed class UnaryExpression(Token op, IExpression right) : IExpression
{
    public Token Operator { get; } = op;
    public IExpression Right { get; } = right;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
}

public sealed class LiteralExpression(object? value) : IExpression
{
    public object? Value { get; } = value;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
}

public sealed class VariableExpression(Token name) : IExpression
{
    public Token Name { get; } = name;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitVariableExpression(this);
}

public sealed class GroupingExpression(IExpression expression) : IExpression
{
    public IExpression Expression { get; } = expression;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitGroupingExpression(this);
}

public sealed class CallExpression(string name, IReadOnlyList<IExpression> arguments) : IExpression
{
    public string Name { get; } = name;
    public IReadOnlyList<IExpression> Arguments { get; } = arguments;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitCallExpression(this);
}

public sealed class ArrayAccessExpression(Token name, IReadOnlyList<IExpression> indices) : IExpression
{
    public Token Name { get; } = name;
    public IReadOnlyList<IExpression> Indices { get; } = indices;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitArrayAccessExpression(this);
}

public sealed class FieldAccessExpression(IExpression obj, Token fieldName) : IExpression
{
    public IExpression Object { get; } = obj;
    public Token FieldName { get; } = fieldName;

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitFieldAccessExpression(this);
}
