using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    class Scanner
    {
        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            {"and" ,   TokenType.AND },
            {"class",  TokenType.CLASS },
            {"else",   TokenType.ELSE },
            {"exit",   TokenType.EXIT },
            {"false",  TokenType.FALSE},
            {"for",    TokenType.FOR},
            {"fun",    TokenType.FUN},
            {"if",     TokenType.IF},
            {"nil",    TokenType.NIL},
            {"or",     TokenType.OR},
            {"print",  TokenType.PRINT},
            {"return", TokenType.RETURN},
            {"super",  TokenType.SUPER},
            {"this",   TokenType.THIS},
            {"true",   TokenType.TRUE},
            {"var",    TokenType.VAR},
            {"while",  TokenType.WHILE},
        };

        // components
        private readonly String source;
        private readonly List<Token> tokens = new List<Token>();

        // state tracking
        private int start = 0;
        private int current = 0;
        private int line = 1;
        private Token previousToken;

        public Scanner(String source)
        {
            this.source = source;
        }

        #region Tokenizing

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // We are at the beginning of the next lexeme.
                start = current;
                ScanToken();
            }

            tokens.Add(new Token(TokenType.EOF, String.Empty, null, line));
            return tokens;
        }

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(':
                    AddToken(TokenType.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenType.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenType.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenType.RIGHT_BRACE);
                    break;
                case '&':
                    AddToken(TokenType.AMPERSAND);
                    break;
                case '|':
                    AddToken(TokenType.PIPE);
                    break;
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case '-':
                    AddToken(TokenType.MINUS);
                    break;
                case '+':
                    AddToken(TokenType.PLUS);
                    break;
                case ';':
                    AddToken(TokenType.SEMICOLON);
                    break;
                case '*':
                    AddToken(TokenType.STAR);
                    break;
                case '!':
                    AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;
                case '/':
                    if (Match('/'))
                    {
                        // A comment goes until the end of the line.
                        while (Peek() != '\n' && !IsAtEnd())
                            Advance();
                    }
                    else if (Match('*'))
                    {
                        // multi-line comments
                        while (Peek() != '*' && PeekNext() != '/')
                        {
                            if (!IsAtEnd())
                            {
                                if (Advance() == '\n')
                                    line++;
                            }
                            else
                            {
                                LoxInterpreter.Program.Error(line, "Unterminated multi-line comment.");
                                goto exit;
                            }
                        }

                        // churn '*/'
                        Advance();
                        Advance();
                    }
                    else
                    {
                        AddToken(TokenType.SLASH);
                    }
                    exit:
                    break;

                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.
                    break;

                case '\n':
                    line++;
                    break;

                case '"':
                    LexString();
                    break;

                default:
                    if (IsDigit(c))
                    {
                        LexNumber();
                    }
                    else if (IsAlpha(c))
                    {
                        LexIdentifier();
                    }
                    else
                    {
                        LoxInterpreter.Program.Error(line, "Unexpected character.");
                    }

                    break;

            }
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, Object literal)
        {
            String text = source.Substring(start, current - start);
            previousToken = new Token(type, text, literal, line);
            tokens.Add(previousToken);
        }

        #endregion Tokenizing

        #region Lexing

        private bool IsAtEnd()
        {
            return current >= source.Length;
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                    c == '_';
        }

        private bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        private void LexIdentifier()
        {
            while (IsAlphaNumeric(Peek()))
                Advance();

            // See if the identifier is a reserved word.
            String text = source.Substring(start, current - start);

            TokenType type;
            if (!keywords.TryGetValue(text, out type))
            {
                type = TokenType.IDENTIFIER;
            }
            AddToken(type);
        }

        private void LexNumber()
        {
            while (IsDigit(Peek()))
                Advance();

            // Look for a fractional part.
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the "."
                Advance();

                while (IsDigit(Peek()))
                    Advance();
            }

            AddToken(TokenType.NUMBER, 
                Double.Parse(source.Substring(start, current - start)));
        }

        private void LexString()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                    line++;
                Advance();
            }

            // Unterminated string.
            if (IsAtEnd())
            {
                LoxInterpreter.Program.Error(line, "Unterminated string.");
                return;
            }

            // The closing ".
            Advance();

            // Trim the surrounding quotes.
            int next = start + 1;

            // unescaping escape sequences would go here
            String value = source.Substring(next, current - next - 1);
            AddToken(TokenType.STRING, value);
        }

        #endregion Lexing

        #region Parsing

        private char PeekNext()
        {
            int next = current + 1;
            if (next >= source.Length)
                return '\0';
            return source[next];
        }

        private char Peek()
        {
            if (IsAtEnd())
                return '\0';
            return source[current];
        }

        /// <summary>
        /// Conditionally match next char and <see cref="Advance"/>.
        /// </summary>
        private bool Match(char expected)
        {
            if (IsAtEnd() || source[current] != expected)
                return false;

            current++;
            return true;
        }

        private char Advance()
        {
            return source[current++];
        }

        #endregion Parsing
    }
}