using LoxInterpreter.RazerLox.Callables;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// Runtime representation of a class.
    /// </summary>
    internal class LoxClass : ILoxCallable
    {
        public readonly string identifier;

        public int Arity => 0;

        public LoxClass(string identifier)
        {
            this.identifier = identifier;
        }

        public object Call(Interpreter interpreter, IList<object> arguments)
        {
            var instance = new LoxInstance(this);
            return instance;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}