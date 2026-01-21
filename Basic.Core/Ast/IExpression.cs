namespace Basic.Core.Ast;

public interface IExpression
{
    T Accept<T>(IExpressionVisitor<T> visitor);
}

public interface IExpressionVisitor<T>
{
    T VisitBinaryExpression(BinaryExpression expr);
    T VisitUnaryExpression(UnaryExpression expr);
    T VisitLiteralExpression(LiteralExpression expr);
    T VisitVariableExpression(VariableExpression expr);
    T VisitGroupingExpression(GroupingExpression expr);
    T VisitCallExpression(CallExpression expr);
    T VisitArrayAccessExpression(ArrayAccessExpression expr);
    T VisitFieldAccessExpression(FieldAccessExpression expr);
}
