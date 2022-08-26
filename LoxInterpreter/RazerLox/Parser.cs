using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    public class Parser
    {
        private class ParseException : RuntimeException
        {
            public ParseException(Token token, string message)
                : base(token, message)
            {
                // exists
            }
        }

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

        public List<AStatement> Parse()
        {
            var statements = new List<AStatement>();

            try
            {
                while (!IsAtEnd())
                {
                    statements.Add(ParseDeclaration());
                }
                return statements;
            }
            catch (ParseException p)
            {
                Program.Error(p.Token, p.Message);
                return null;
            }
        }

        #region Parse Helpers

        private AStatement ParseDeclaration()
        {
            try
            {
                if (MatchesNext(TokenType.VAR))
                    return ParseVarDeclaration();
                else
                    return ParseStatement();
            }
            catch (ParseException ex)
            {
                Program.Error(ex.Token, ex.Message);
                Synchronize();
                return null;
            }
        }

        private AStatement ParseVarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expected variable name.");
            AExpression initializer = null;

            // get initial value (e.g. var x = 5;)
            if (MatchesNext(TokenType.EQUAL))
                initializer = ParseExpression();

            Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            return new VariableStatement(name, initializer);
        }

        private AStatement ParseStatement()
        {
            if (MatchesNext(TokenType.PRINT)) // or any other stmts
                return ParsePrintStatement();
            else if (MatchesNext(TokenType.LEFT_BRACE))
                return ParseBlockStatement();
            else
                return ParseExpressionStatement();
        }

        private AStatement ParseExpressionStatement()
        {
            AExpression expression = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
            return new ExpressionStatement(expression);
        }

        private AStatement ParsePrintStatement()
        {
            AExpression value = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after value.");
            return new PrintStatement(value);
        }

        private AStatement ParseBlockStatement()
        {
            return new BlockStatement(ParseBlockStatementBody());
        }

        private AExpression ParseExpression()
        {
            return ParseAssignment();
        }

        private AExpression ParseAssignment()
        {
            AExpression expression = ParseEquality();

            if (MatchesNext(TokenType.EQUAL))
            {
                Token equals = Previous();
                AExpression value = ParseAssignment();

                if (expression is VariableExpression variableExpression)
                {
                    return new AssignmentExpression(
                        variableExpression.identifier, value);
                }
                else // a + b = c; is an error
                {
                    Program.Error(equals, "Invalid assignment target.");
                }
            }

            // else recursive base case
            return expression;
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
            if (MatchesNext(TokenType.BANG, TokenType.MINUS))
            {
                Token _operator = Previous();
                AExpression right = ParseUnary(); // parse operand
                return new UnaryExpression(_operator, right);
            }

            return ParsePrimary();
        }

        private AExpression ParsePrimary()
        {
            // TODO - convert to switch-case
            // TokenType next = Check();
            // Advance() on match

            if (MatchesNext(TokenType.NUMBER) || MatchesNext(TokenType.STRING))
                return new LiteralExpression(Previous().literal);

            else if (MatchesNext(TokenType.FALSE))
                return new LiteralExpression(false);

            else if (MatchesNext(TokenType.TRUE))
                return new LiteralExpression(true);

            else if (MatchesNext(TokenType.NIL))
                return new LiteralExpression(null);

            else if (MatchesNext(TokenType.IDENTIFIER))
                return new VariableExpression(Previous());

            else if (MatchesNext(TokenType.LEFT_PAREN))
            {
                AExpression expr = ParseExpression();
                Consume(TokenType.RIGHT_PAREN, "Expected ')' after expression.");
                return new GroupingExpression(expr);
            }

            else if (MatchesNext(TokenType.VAR))
                return new VariableExpression(Previous());

            else if (MatchesNext(TokenType.EXIT))
                return new ExitExpression();

            else
                throw HandleError(Peek(), $"Expected expression, but saw {Peek()}.");
        }


        private AExpression ParseLeftAssociativeSeries(
            Func<AExpression> getOperand,
            params TokenType[] tokens)
        {
            AExpression expr = getOperand();

            while (MatchesNext(tokens))
            {
                Token _operator = Previous();
                AExpression right = getOperand();
                expr = new BinaryExpression(expr, _operator, right);
            }

            return expr;
        }

        private AExpression ParseRightAssociativeSeries(
            Func<AExpression> getOperand,
            params TokenType[] tokens)
        {
            AExpression expr = getOperand();

            while (MatchesNext(tokens))
            {
                Token _operator = Previous();
                AExpression left = getOperand();
                expr = new BinaryExpression(left, _operator, expr);
            }

            return expr;
        }

        private IList<AStatement> ParseBlockStatementBody()
        {
            var statements = new List<AStatement>();

            // while next token is not } or eof
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
                statements.Add(ParseDeclaration());

            // fail-safe
            Consume(TokenType.RIGHT_BRACE, "Expected '}' after block.");

            return statements;
        }

        #endregion Parse Helpers

        private bool MatchesNext(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool MatchesNext(params TokenType[] types)
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

        /// <summary>
        /// Eat tokens until <paramref name="type"/> is found; 
        /// otherwise <see cref="HandleError"/> with
        /// <paramref name="message"/>.
        /// </summary>
        /// <param name="type">Target token to eat tokens until found.</param>
        /// <param name="message">Error message to print.</param>
        /// <exception cref="ParseException"/>
        private Token Consume(TokenType type, string message)
        {
            if (Check(type))
                return Advance();
            else
                throw HandleError(Peek(), message);
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private void Synchronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().type == TokenType.SEMICOLON)
                    return;

                switch (Peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }
                Advance();
            }
        }

        /// <exception cref="ParseException"/>
        private static ParseException HandleError(Token token, string message)
        {
            return new ParseException(token, message);
        }
    }
}
