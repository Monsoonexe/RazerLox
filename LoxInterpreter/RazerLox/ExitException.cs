using System;

namespace LoxInterpreter.RazerLox
{
    internal class ExitException : Exception
    {
        public ExitException()
            : base()
        {

        }

        public ExitException(string message)
            : base(message)
        {

        }
    }
}
