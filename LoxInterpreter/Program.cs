using CommandLine;
using System.IO;
using System;
using LoxInterpreter.RazerLox;

namespace LoxInterpreter
{
    internal class Program
    {
        public static LaunchArguments LaunchArguments { get; private set; }

        private static bool hadError;
        private static bool hadRuntimeError;

        private static int Main(string[] args)
        {
            var parsedArgs = ParseCommandLineArguments(args);
            if (LaunchArguments == null)
            {
                // print usage
                Console.WriteLine(GetHelpText(parsedArgs));
                return -1;
            }
            else if (LaunchArguments.FilePath != null)
            {
                RunFile(LaunchArguments.FilePath);
            }
            else
            {
                RunPrompt();
            }

            return GetErrorCode();
        }

        private static int GetErrorCode()
        {
            return (hadError) ? -1 : 0;
        }

        private static ParserResult<LaunchArguments> ParseCommandLineArguments(string[] args)
        {
            var result = Parser.Default.ParseArguments<LaunchArguments>(args);
            LaunchArguments = result.Value;
            return result;
        }

        private static string GetHelpText(ParserResult<LaunchArguments> res)
        {
            return CommandLine.Text.HelpText.AutoBuild(res);
        }

        /// <summary>
        /// Core function
        /// </summary>
        private static void Run(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();

            // just print them for now
            tokens.ForEach((t) => Console.WriteLine(t));
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
            using (var inputStream = Console.OpenStandardInput())
            using (var buffStream = new BufferedStream(inputStream))
            using (var textStream = new StreamReader(buffStream))
            {
                while (true)
                {
                    Console.Write("> ");
                    string line = textStream.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    else
                    {
                        Run(line);
                        hadError = false; // reset flag
                    }
                }
            }
        }

        public static void Error(int line, string message)
        {

        }

        //< lox-error
        //> Parsing Expressions token-error
        private static void Error(Token token, string message)
        {
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
        private static void RuntimeError(RuntimeException error)
        {
            using (var stderr = Console.OpenStandardError())
            using (var stream = new StreamWriter(stderr))
            {
                stream.WriteLine(error.Message + "\n[line " + error.token.line + "]");
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
            hadError = true;
        }
    }
}
