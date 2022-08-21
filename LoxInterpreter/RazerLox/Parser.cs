using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    public class Parser
    {
        //private static readonly Array TokenTypeMembers = Enum.GetValues(typeof(TokenType));
        private readonly IList<Token> tokens;

        // state
        private int current = 0;

        #region Constructors

        public Parser(IList<Token> tokens)
        {
            this.tokens = tokens;
        }

        #endregion Constructors

        private AExpression ParseLeftAssociativeSeries(
            Func<AExpression> getOperand,
            params TokenType[] tokens)
        {
            AExpression expr = getOperand();

            while (Match(tokens))
            {
                Token _operator = Previous();
                AExpression right = getOperand();
                expr = new BinaryExpression(expr, _operator, right);
            }

            return expr;
        }

        private AExpression ParseExpression()
        {
            return ParseEquality();
        }

        private AExpression ParseEquality()
        {
            return ParseLeftAssociativeSeries(ParseComparison,
                TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL);
        }

        private AExpression ParseComparison()
        {
            return ParseLeftAssociativeSeries(ParseTerm, 
                TokenType.GREATER, TokenType.GREATER_EQUAL,
                TokenType.LESS, TokenType.LESS_EQUAL);
        }

        private AExpression ParseTerm()
        {
            return ParseLeftAssociativeSeries(
                ParseFactor, TokenType.MINUS, TokenType.PLUS);
        }

        private AExpression ParseFactor()
        {
            return ParseLeftAssociativeSeries(ParseUnary,
                TokenType.SLASH, TokenType.STAR);
        }

        private AExpression ParseUnary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                Token _operator = Previous();
                AExpression right = ParseUnary(); // parse operand
                return new UnaryExpression(_operator, right);
            }

            return ParsePrimary();
        }

        private AExpression ParsePrimary()
        {
            if (Match(TokenType.FALSE))
                return new LiteralExpression(false);

            if (Match(TokenType.TRUE))
                return new LiteralExpression(true);

            if (Match(TokenType.NIL))
                return new LiteralExpression(null);

            if (Match(TokenType.NUMBER) || Match(TokenType.STRING))
                return new LiteralExpression(Previous().literal);

            if (Match(TokenType.LEFT_PAREN))
            {
                AExpression expr = ParseExpression();
                Consume(TokenType.RIGHT_PAREN, "Expected ')' after expression.");
                return new GroupingExpression(expr);
            }

            // fail-safe
            throw new NotSupportedException(Previous().ToString());
        }

        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var token in types)
            {
                if (Check(token))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Advance()
        {
            if (!IsAtEnd())
                ++current;
            return Previous();
        }

        private bool Check(TokenType type)
        {
            return !IsAtEnd() && Peek().type == type;
        }

        private void Consume(TokenType type, string message)
        {
            if (Check(type))
                Advance();
            else
                throw HandleError(Peek(), message);
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.EOF;
        }

        private Exception HandleError(Token token, string message)
        {
            Program.Error(token, message);
            return new ParserException(token, message);
        }
    }
}
