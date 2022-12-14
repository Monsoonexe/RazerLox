using System;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// This class is just a demonstration of implementing the visitor
    /// pattern and is no longer needed.
    /// </summary>
    public class AstPrinter : AExpression.IVisitor<String>
    {
        public string GetParenthesizedString(AExpression expr)
        {
            return expr.Accept(this);
        }

        private string Parenthesize(string name, params AExpression[] expressions)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(").Append(name); // open
            foreach(var expr in expressions)
            {
                sb.Append(" ")
                    .Append(expr.Accept(this));
            }
            sb.Append(")"); // close
            return sb.ToString();
        }

        public string VisitBinaryExpression(BinaryExpression binaryexpression)
        {
            return Parenthesize(binaryexpression._operator.lexeme,
                binaryexpression.left, binaryexpression.right);
        }

        public string VisitGroupingExpression(GroupingExpression groupingexpression)
        {
            return Parenthesize("group", groupingexpression.expression);
        }

        public string VisitLiteralExpression(LiteralExpression literalexpression)
        {
            return literalexpression?.value.ToString() ?? "nil";
        }

        public string VisitUnaryExpression(UnaryExpression unaryexpression)
        {
            return Parenthesize(unaryexpression._operator.lexeme, unaryexpression.right);
        }

        public string VisitVariableExpression(VariableExpression expression)
        {
            return $"var {expression.identifier.lexeme}"; // 
        }

        public string VisitAssignmentExpression(AssignmentExpression expression)
        {
            string value = "I need to Visit the inner expression, but I don't know how!";
            return $"{expression.identifier.lexeme} = {value}";
        }

        public string VisitLogicalExpression(LogicalExpression expression)
        {
            return Parenthesize(expression._operator.lexeme,
                expression.left, expression.right);
        }

        public string VisitCallExpression(CallExpression expression)
        {
            throw new NotImplementedException();
        }

        public string VisitGetExpression(GetExpression expression)
        {
            throw new NotImplementedException();
        }

        public string VisitSetExpression(SetExpression expression)
        {
            throw new NotImplementedException();
        }

        public string VisitThisExpression(ThisExpression expression)
        {
            throw new NotImplementedException();
        }

        public string VisitSuperExpression(SuperExpression expression)
        {
            throw new NotImplementedException();
        }
    }
}
