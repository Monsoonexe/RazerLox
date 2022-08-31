using System;

namespace LoxInterpreter.RazerLox
{
    internal class ExitException : Exception
    {
        public readonly int ExitCode;

        public ExitException() : this(0) { }

        public ExitException(int exitCode)
            : base("The user code has requested to exit the program.")
        {
            this.ExitCode = exitCode;
        }
    }
}
