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
                if (MatchesNext(TokenType.CLASS))
                    return ParseClassDeclaration();
                else if (MatchesNext(TokenType.FUN))
                    return ParseFunctionDeclaration("function");
                else if (MatchesNext(TokenType.VAR))
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

        private AStatement ParseClassDeclaration()
        {
            Token identifier = Consume(TokenType.IDENTIFIER,
                "Expected class identifier.");

            // {
            Consume(TokenType.LEFT_BRACE,
                "Expected '{' before class body.");

            // methods
            var methods = new List<FunctionDeclaration>();
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
                methods.Add((FunctionDeclaration)ParseFunctionDeclaration("method"));

            // }
            Consume(TokenType.RIGHT_BRACE,
                "Expected '}' after class body.");
            return new ClassDeclaration(identifier, methods);
        }

        private AStatement ParseFunctionDeclaration(string kind)
        {
            const int MAX_PARAMS = SyntaxRules.MaxFunctionParameters;
            Token identifier = Consume(TokenType.IDENTIFIER,
                $"Expected {kind} name.");
            Consume(TokenType.LEFT_PAREN,
                $"Expected '(' after {kind} identifier.");

            // parameter list
            var parameters = new List<Token>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count > MAX_PARAMS)
                    {
                        Program.Error(Peek(),
                            $"Can't have more than {MAX_PARAMS} parameters.");
                    }

                    parameters.Add(Consume(TokenType.IDENTIFIER,
                        "Expected parameter name."));
                } while (MatchesNext(TokenType.COMMA));
            }
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after parameter list.");

            // body (ParseBlockBody expects '{' already consumed)
            Consume(TokenType.LEFT_BRACE, "Expected { before " + kind + " body.");
            var body = ParseBlockStatementBody();
            return new FunctionDeclaration(identifier, parameters, body);
        }

        private AStatement ParseVarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expected variable name.");

            // get initial value (e.g. var x = 5;)
            Consume(TokenType.EQUAL, "Expected variable to be assigned a value with '='.");
            AExpression initializer = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            return new VariableDeclaration(name, initializer);
        }

        private AStatement ParseStatement()
        {
            if (MatchesNext(TokenType.BREAK))
                return ParseBreakStatement();
            else if (MatchesNext(TokenType.FOR))
                return ParseForStatement();
            else if (MatchesNext(TokenType.IF))
                return ParseIfStatement();
            else if (MatchesNext(TokenType.PRINT))
                return ParsePrintStatement();
            else if (MatchesNext(TokenType.RETURN))
                return ParseReturnStatement();
            else if (MatchesNext(TokenType.WHILE))
                return ParseWhileStatement();
            else if (MatchesNext(TokenType.LEFT_BRACE))
                return ParseBlockStatement();
            else
                return ParseExpressionStatement();
        }

        private AStatement ParseBreakStatement()
        {
            Consume(TokenType.SEMICOLON, "Expected ';' after statement.");
            return new BreakStatement(Previous());
        }

        private AStatement ParseExpressionStatement()
        {
            AExpression expression = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
            return new ExpressionStatement(expression);
        }

        private AStatement ParseForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'for'.");

            // initialize
            AStatement initializer;
            if (MatchesNext(TokenType.SEMICOLON))
                initializer = null; // none
            if (MatchesNext(TokenType.VAR))
                initializer = ParseVarDeclaration();
            else
                initializer = ParseExpressionStatement();

            // condition
            AExpression condition = null;
            if (!Check(TokenType.SEMICOLON))
                condition = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after loop condition.");

            // increment
            AExpression incrementer = null;
            if (!Check(TokenType.RIGHT_PAREN))
                incrementer = ParseExpression();
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after for-clause.");

            // caramelize for-loop into while-loop
            AStatement body = ParseStatement();

            if (incrementer != null)
            {
                // do the body, then do the incrementer after
                var block = new AStatement[]
                {
                    body,
                    new ExpressionStatement(incrementer)
                };
                body = new BlockStatement(block);
            }

            //infinite loop if no condition
            if (condition == null)
                condition = new LiteralExpression(true);
            body = new WhileStatement(condition, body);

            if (initializer != null)
            {
                // include in scope
                var block = new AStatement[]
                {
                    initializer, body
                };
                body = new BlockStatement(block);
            }

            return body;
        }

        private AStatement ParseIfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'if'.");
            AExpression condition = ParseExpression();
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after if condition.");
            AStatement thenBranch = ParseStatement();

            // else -- binds to nearest 'if'
            AStatement elseBranch = null;
            if (MatchesNext(TokenType.ELSE))
                elseBranch = ParseStatement();

            return new IfStatement(condition, thenBranch, elseBranch);
        }

        private AStatement ParsePrintStatement()
        {
            AExpression value = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after value.");
            return new PrintStatement(value);
        }

        private AStatement ParseReturnStatement()
        {
            Token keyword = Previous();
            AExpression value = null;

            if (!Check(TokenType.SEMICOLON))
                value = ParseExpression();

            Consume(TokenType.SEMICOLON, "Expected ';' after return value.");
            return new ReturnStatement(keyword, value);
        }

        private AStatement ParseWhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'while'.");
            AExpression condition = ParseExpression();
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after while statement.");
            AStatement body = ParseStatement();

            return new WhileStatement(condition, body);
        }

        private AStatement ParseBlockStatement()
        {
            return new BlockStatement(ParseBlockStatementBody());
        }

        private AExpression ParseExpression()
        {
            AExpression expression = ParseAssignment();

            // comma-separated expressions
            while (MatchesNext(TokenType.COMMA))
                expression = ParseAssignment();

            return expression;
        }

        private AExpression ParseAssignment()
        {
            AExpression expression = ParseLogicOr(); // may be an identifier

            if (MatchesNext(TokenType.EQUAL)) // parsed an identifier
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

        private AExpression ParseLogicOr()
        {
            AExpression expression = ParseLogicAnd();

            while (MatchesNext(TokenType.OR))
            {
                Token _operator = Previous();
                AExpression right = ParseLogicAnd();
                expression = new LogicalExpression(expression, _operator, right);
            }

            return expression;
        }

        private AExpression ParseLogicAnd()
        {
            AExpression expression = ParseEquality();

            while (MatchesNext(TokenType.AND))
            {
                Token _operator = Previous();
                AExpression right = ParseEquality();
                expression = new LogicalExpression(expression, _operator, right);
            }

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
                ParseFactor, TokenType.MINUS, TokenType.PLUS,
                TokenType.PIPE, TokenType.AMPERSAND);
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
            else
            {
                return ParseCallExpression();
            }
        }

        private AExpression ParseCallExpression()
        {
            AExpression expression = ParsePrimary();

            while (true)
            {
                // myFunction()()()() is valid syntax
                if (MatchesNext(TokenType.LEFT_PAREN))
                {
                    expression = FinishCallExpression(expression);
                }
                else
                {
                    // come back later
                    break;
                }
            }

            return expression;
        }

        private AExpression FinishCallExpression(AExpression callee)
        {
            const int MAX_ARGS = SyntaxRules.MaxFunctionParameters;
            var arguments = new List<AExpression>();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    // check arg count
                    if (arguments.Count >= MAX_ARGS)
                        Program.Error(Peek(), $"Can't have more than {MAX_ARGS} arguments.");

                    arguments.Add(ParseExpression());
                }
                while (MatchesNext(TokenType.COMMA));
            }

            // get stack trace marker
            Token closingParen = Consume(TokenType.RIGHT_PAREN,
                "Expected ')' after argument list.");

            return new CallExpression(callee, closingParen, arguments);
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

        private AExpression ParseLeftAssociativeSeries(
            Func<AExpression> getOperand,
            TokenType token)
        {
            AExpression expr = getOperand();

            while (MatchesNext(token))
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

        /// <summary>
        /// Safely peek and check if the next token 
        /// matches <paramref name="type"/>.
        /// </summary>
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
