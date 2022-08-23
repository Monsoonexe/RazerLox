using System;

namespace LoxInterpreter.RazerLox
{
    public class RuntimeException : Exception
    {
        public readonly Token Token;

        public RuntimeException(Token token, string message)
            :base (message)
        {
            this.Token = token;
        }
    }
}
