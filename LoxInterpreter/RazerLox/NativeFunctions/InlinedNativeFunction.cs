using System.Collections.Generic;
using LoxInterpreter.RazerLox.Callables;

namespace LoxInterpreter.RazerLox
{
    internal sealed class InlinedNativeFunction : ANativeFunction
    {
        private readonly int arity;
        public override int Arity { get => arity; }

        private readonly Callable function;

        public InlinedNativeFunction(int arity, Callable function)
        {
            this.arity = arity;
            this.function = function;
        }

        public override object Call(Interpreter interpreter, IList<object> arguments)
        {
            return function(interpreter, arguments);
        }
    }
}
