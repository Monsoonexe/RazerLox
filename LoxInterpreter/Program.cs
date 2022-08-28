using CommandLine;
using System.IO;
using System;
using LoxInterpreter.RazerLox;

namespace LoxInterpreter
{
    internal class Program
    {
        public static LaunchArguments LaunchArguments { get; private set; }
        private static readonly Interpreter interpreter = new Interpreter();
        private static bool hadError;
        private static bool hadRuntimeError;
        private static bool active = true;

        private static int Main(string[] args)
        {
            var parsedArgs = ParseCommandLineArguments(args);
            if (LaunchArguments == null)
            {
                // print usage
                PrintHelpText(parsedArgs);
                PressKeyToExitProgram();
                return -1;
            }
            else if (LaunchArguments.FilePath != null)
            {
                RunFile(LaunchArguments.FilePath);

                // exit prompt
                if (LaunchArguments.NoExit)
                {
                    PressKeyToExitProgram();
                }
            }
            else
            {
                RunPrompt();
            }

            return GetErrorCode();
        }

        private static int GetErrorCode()
        {
            if (hadError)
                return 65;
            else if (hadRuntimeError)
                return 70;
            else 
                return 0;
        }

        private static ParserResult<LaunchArguments> ParseCommandLineArguments(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<LaunchArguments>(args);
            LaunchArguments = result.Value;
            return result;
        }

        private static void PrintHelpText(ParserResult<LaunchArguments> parsedArgs)
        {
            Print(GetHelpText(parsedArgs));
        }

        private static string GetHelpText(
            ParserResult<LaunchArguments> res)
        {
            return CommandLine.Text.HelpText.AutoBuild(res);
        }

        private static void PressKeyToExitProgram()
        {
            Console.Write("\r\nPress any key to exit...\r\n>> ");
            Console.ReadKey();
        }

        /// <summary>
        /// Core function
        /// </summary>
        private static void Run(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new RazerLox.Parser(tokens);
            var program = parser.Parse();

            if (hadError) return; // syntax error

            var resolver = new Resolver(interpreter);
            resolver.Resolve(program);

            if (hadError) return; // analysis error

            interpreter.Interpret(program);

            // just print them for now
            // tokens.ForEach((t) => Console.WriteLine(t));

            // print tree
            //var printer = new AstPrinter();
            //Console.WriteLine(printer.GetParenthesizedString(expression));
        }

        /// <summary>
        /// From file.
        /// </summary>
        /// <param name="path"></param>
        private static void RunFile(string path)
        {
            try
            {
                Run(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Interactive
        /// </summary>
        private static void RunPrompt()
        {
            active = true;
            while (active)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
                else
                {
                    // at the prompt, turn everything into a print statement
                    if (!line.StartsWith("print ") && !line.StartsWith("var ")
                        && !line.StartsWith("while ") && !line.StartsWith("for "))
                        line = "print " + line;

                    // append missing statement-terminator ';'
                    if (!line.EndsWith(";"))
                        line += ";";

                    try
                    {
                        Run(line);
                    }
                    catch (Exception ex)
                    {
                        Error(-1, $"RazerLox itself threw an exception! " +
                            $"\r\n{ex}");
                    }

                    // reset state
                    hadError = false;
                    hadRuntimeError = false;
                }
            }
        }

        /// <summary>
        /// User wishes to exit the interactive prompt session.
        /// </summary>
        public static void ExitPrompt()
        {
            active = false;
        }

        public static void Print(string value)
            => Console.WriteLine(value);

        public static void PrintFormat(string format, string value)
            => Print(string.Format(format, value));

        public static void Error(int line, string message)
        {
            hadError = true;
            Report(line, "", message);
        }

        //< lox-error
        //> Parsing Expressions token-error
        public static void Error(Token token, string message)
        {
            hadError = true;
            if (token.type == TokenType.EOF)
            {
                Report(token.line, " at end", message);
            }
            else
            {
                Report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        //< Parsing Expressions token-error
        //> Evaluating Expressions runtime-error-method
        public static void RuntimeError(RuntimeException error)
        {
            using (var stderr = Console.OpenStandardError())
            using (var stream = new StreamWriter(stderr))
            {
                stream.WriteLine(error.Message + "\n[line " + error.Token.line + "]");
            }
            hadRuntimeError = true;
        }

        private static void Report(int line, string where, string message)
        {
            using (var stderr = Console.OpenStandardError())
            using (var stream = new StreamWriter(stderr))
            {
                stream.WriteLine($"[line {line}] Error{where}: {message}");
            }
        }
    }
}
