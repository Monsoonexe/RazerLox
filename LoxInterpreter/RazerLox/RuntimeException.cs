using System;

namespace LoxInterpreter.RazerLox
{
    public class RuntimeException : Exception
    {
        public readonly Token token;

        public RuntimeException(Token token, string message)
            :base (message)
        {
            this.token = token;
        }
    }
}
