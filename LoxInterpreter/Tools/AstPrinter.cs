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
                sb
                    .Append(" ")
                    .Append(expr.Accept(this));
            }
            sb.Append(")"); // close
            return sb.ToString();
        }

        public string VisitBinaryExpression(BinaryExpression binaryexpression)
        {
            return Parenthesize(binaryexpression.op.lexeme,
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
            return Parenthesize(unaryexpression.op.lexeme, unaryexpression.right);
        }
    }
}
