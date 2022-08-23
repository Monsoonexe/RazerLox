﻿using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    public class Interpreter : AExpression.IVisitor<object>,
                               AStatement.IVisitor<Void> // statements produce no values
    {
        private bool wishToExit;

        public void Interpret (AExpression expr)
        {
            try
            {
                object result = Evaluate(expr);
                Program.Print(Stringify(result));
            }
            catch (RuntimeException runEx)
            {
                Program.RuntimeError(runEx);
            }

            if (wishToExit)
                Program.ExitPrompt();
        }

        public void Interpret (IEnumerable<AStatement> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (RuntimeException runEx)
            {
                Program.RuntimeError(runEx);
            }
            //catch (ExitException) //placeholder
            //{
            //    // exit
            //    Program.ExitPrompt();
            //}

            if (wishToExit)
                Program.ExitPrompt();
        }

        private string Stringify(object value)
        {
            string str; // return value
            if (value is null)
            {
                str = "nil"; // TODO - const
            }
            else if (value is double d)
            {
                // convert to integer format if rounded.
                str = d.ToString();
                if (str.EndsWith(".0"))
                    str = str.Substring(0, str.Length - 2);
            }
            else
            {
                str = value.ToString();
            }
            return str;
        }

        #region Expression Visitors

        public object VisitBinaryExpression(BinaryExpression expression)
        {
            object left = Evaluate(expression.left);
            object right = Evaluate(expression.right);
            TokenType type = expression.op.type;
            (double A, double B) operands;

            switch (type)
            {
                case TokenType.BANG_EQUAL:
                    return IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.GREATER:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A > operands.B;
                case TokenType.GREATER_EQUAL:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A >= operands.B;
                case TokenType.LESS:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A < operands.B;
                case TokenType.LESS_EQUAL:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A <= operands.B;
                case TokenType.MINUS:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A - operands.B;
                case TokenType.SLASH:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    GuardAgainstDivideByZero(expression.op, operands.B);
                    return operands.A / operands.B;
                case TokenType.STAR:
                    operands = CheckNumberOperands(expression.op,
                        left, right);
                    return operands.A * operands.B;
                case TokenType.PLUS:
                {
                    if (left is double a && right is double b)
                    {
                        return a + b;
                    }
                    else if (left is string || right is string)
                    {
                        // support concatenation if either operand is a string
                        return left.ToString() + right.ToString();
                    }
                    throw new RuntimeException(expression.op, "Operands must both be numbers or strings.");
                }
                default:
                    throw GetImproperOperatorException(type, expression);
            }
        }

        public object VisitGroupingExpression(GroupingExpression expression)
        {
            return Evaluate(expression.expression);
        }

        public object VisitLiteralExpression(LiteralExpression expression)
        {
            return expression.value;
        }

        public object VisitUnaryExpression(UnaryExpression expression)
        {
            object right = Evaluate(expression.right);
            TokenType token = expression.op.type;

            switch (token)
            {
                case TokenType.BANG:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    return -CheckNumberOperand(expression.op, right);
                default:
                    throw GetImproperOperatorException(token, expression);
            }
        }

        public object VisitExitExpression(ExitExpression expression)
        {
            // the user wishes to exit the prompt
            //throw new ExitException();
            wishToExit = true;
            return null;
        }

        #endregion Expression Visitors

        #region Statement Visitors

        public Void VisitExpressionStatement(ExpressionStatement statement)
        {
            _ = Evaluate(statement.expression);
            return Void.Default;
        }

        public Void VisitPrintStatement(PrintStatement statement)
        {
            object result = Evaluate(statement.expression);
            Program.Print(Stringify(result));
            return Void.Default;
        }

        #endregion Statement Visitors

        #region Evaluation Helpers

        private object Evaluate(AExpression expression)
        {
            return expression.Accept(this);
        }

        private void Execute(AStatement statement)
        {
            statement.Accept(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns><paramref name="operand"/> cast to a <see cref="double"/>.</returns>
        /// <exception cref="RuntimeException"></exception>
        private static double CheckNumberOperand(Token _operator, object operand)
        {
            if (operand is double d)
                return d;
            throw new RuntimeException(_operator, "Operand must be a number.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns><paramref name="operand"/> cast to a <see cref="double"/>.</returns>
        /// <exception cref="RuntimeException"></exception>
        private static (double, double) CheckNumberOperands(
            Token _operator, object left, object right)
        {
            if (left is double a)
            {
                if (right is double b)
                {
                    return (a, b);
                }
                else
                {
                    throw new RuntimeException(_operator, "Right operand must be a number.");
                }
            }
            else
            {
                throw new RuntimeException(_operator, "Left operand must be a number.");
            }
        }

        /// <summary>
        /// Lox's notion of 'equality' is different than C#'s.
        /// </summary>
        private static bool IsEqual(object a, object b)
        {
            if (a is null && b is null)
                return true;
            else if (a is null)
                return false;
            else
                return a.Equals(b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Truthiness.</returns>
        private static bool IsTruthy(object obj)
        {
            if (obj is null) // nil
                return false;
            else if (obj is bool b)
                return b;
            else return true;                
        }

        #endregion Evaluation Helpers

        private static Exception GetImproperOperatorException(
            TokenType t, AExpression exp)
        {
            return new InvalidOperationException(
                $"{t} is not valid for {exp.GetType().Name}.");
        }

        /// <summary>
        /// Challenge accepted.
        /// </summary>
        /// <exception cref="RuntimeException"></exception>
        private static void GuardAgainstDivideByZero(Token token, double denominator)
        {
            if (denominator == 0)
                throw new RuntimeException(token, "Cannot divide by 0!");
        }
    }
}
