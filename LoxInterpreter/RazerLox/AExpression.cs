/* This file is autogenerated by Generator.ps1.
*  Any changes made to it may be lost the next time it is run.
*/

namespace LoxInterpreter.RazerLox
{
    public abstract class AExpression
    {
        public interface IVisitor<T>
        {
            T VisitAssignmentExpression(AssignmentExpression expression);

            T VisitBinaryExpression(BinaryExpression expression);

            T VisitGroupingExpression(GroupingExpression expression);

            T VisitLiteralExpression(LiteralExpression expression);

            T VisitLogicalExpression(LogicalExpression expression);

            T VisitUnaryExpression(UnaryExpression expression);

            T VisitVariableExpression(VariableExpression expression);

            T VisitExitExpression(ExitExpression expression);

        }
        public abstract T Accept<T>(IVisitor<T> visitor);
    }
    public sealed class AssignmentExpression : AExpression
    {
        public readonly Token identifier;
        public readonly AExpression value;

        public AssignmentExpression(Token identifier, AExpression value)
        {
            this.identifier = identifier;
            this.value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAssignmentExpression(this);
        }
    }
    public sealed class BinaryExpression : AExpression
    {
        public readonly AExpression left;
        public readonly Token _operator;
        public readonly AExpression right;

        public BinaryExpression(AExpression left, Token _operator, AExpression right)
        {
            this.left = left;
            this._operator = _operator;
            this.right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
    }
    public sealed class GroupingExpression : AExpression
    {
        public readonly AExpression expression;

        public GroupingExpression(AExpression expression)
        {
            this.expression = expression;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitGroupingExpression(this);
        }
    }
    public sealed class LiteralExpression : AExpression
    {
        public readonly object value;

        public LiteralExpression(object value)
        {
            this.value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLiteralExpression(this);
        }
    }
    public sealed class LogicalExpression : AExpression
    {
        public readonly AExpression left;
        public readonly Token _operator;
        public readonly AExpression right;

        public LogicalExpression(AExpression left, Token _operator, AExpression right)
        {
            this.left = left;
            this._operator = _operator;
            this.right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLogicalExpression(this);
        }
    }
    public sealed class UnaryExpression : AExpression
    {
        public readonly Token _operator;
        public readonly AExpression right;

        public UnaryExpression(Token _operator, AExpression right)
        {
            this._operator = _operator;
            this.right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }
    }
    public sealed class VariableExpression : AExpression
    {
        public readonly Token identifier;

        public VariableExpression(Token identifier)
        {
            this.identifier = identifier;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVariableExpression(this);
        }
    }
    public sealed class ExitExpression : AExpression
    {

        public ExitExpression() { }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitExitExpression(this);
        }
    }
}
