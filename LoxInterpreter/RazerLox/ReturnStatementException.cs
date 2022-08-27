using System;

namespace LoxInterpreter.RazerLox
{
    [Serializable]
    internal class ReturnStatementException : RuntimeException
    {
        public readonly object value;

        public ReturnStatementException(object value)
            : base (null, null)
        {
            this.value = value;
        }
    }
}