using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox.Callables
{
    internal delegate object Callable(Interpreter interpreter, IList<object> arguments);

    internal interface ILoxCallable
    {
        /// <summary>
        /// Number of parameters.
        /// </summary>
        int Arity { get; }

        object Call(Interpreter interpreter, IList<object> arguments);
    }
}
