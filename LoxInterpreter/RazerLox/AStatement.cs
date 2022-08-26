/* This file is autogenerated by Generator.ps1.
*  Any changes made to it may be lost the next time it is run.
*/

using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    public abstract class AStatement
    {
        public interface IVisitor<T>
        {
            T VisitBreakStatement(BreakStatement statement);

            T VisitBlockStatement(BlockStatement statement);

            T VisitExpressionStatement(ExpressionStatement statement);

            T VisitIfStatement(IfStatement statement);

            T VisitPrintStatement(PrintStatement statement);

            T VisitVariableStatement(VariableStatement statement);

            T VisitWhileStatement(WhileStatement statement);

        }
        public abstract T Accept<T>(IVisitor<T> visitor);
    }
    public sealed class BreakStatement : AStatement
    {
        public readonly Token token;

        public BreakStatement(Token token)
        {
            this.token = token;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBreakStatement(this);
        }
    }
    public sealed class BlockStatement : AStatement
    {
        public readonly IList<AStatement> statements;

        public BlockStatement(IList<AStatement> statements)
        {
            this.statements = statements;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBlockStatement(this);
        }
    }
    public sealed class ExpressionStatement : AStatement
    {
        public readonly AExpression expression;

        public ExpressionStatement(AExpression expression)
        {
            this.expression = expression;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }
    public sealed class IfStatement : AStatement
    {
        public readonly AExpression condition;
        public readonly AStatement thenBranch;
        public readonly AStatement elseBranch;

        public IfStatement(AExpression condition, AStatement thenBranch, AStatement elseBranch)
        {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitIfStatement(this);
        }
    }
    public sealed class PrintStatement : AStatement
    {
        public readonly AExpression expression;

        public PrintStatement(AExpression expression)
        {
            this.expression = expression;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPrintStatement(this);
        }
    }
    public sealed class VariableStatement : AStatement
    {
        public readonly Token identifier;
        public readonly AExpression initializer;

        public VariableStatement(Token identifier, AExpression initializer)
        {
            this.identifier = identifier;
            this.initializer = initializer;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVariableStatement(this);
        }
    }
    public sealed class WhileStatement : AStatement
    {
        public readonly AExpression condition;
        public readonly AStatement body;

        public WhileStatement(AExpression condition, AStatement body)
        {
            this.condition = condition;
            this.body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitWhileStatement(this);
        }
    }
}
