using LoxInterpreter.RazerLox.Callables;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    internal abstract class ANativeFunction : ILoxCallable
    {
        public abstract int Arity { get; }

        public abstract object Call(Interpreter interpreter, IList<object> arguments);
    }
}
