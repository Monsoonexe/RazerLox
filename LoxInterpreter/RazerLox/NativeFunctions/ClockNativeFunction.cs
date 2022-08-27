using System.Collections.Generic;
using System.Diagnostics;

namespace LoxInterpreter.RazerLox
{
    internal sealed class ClockNativeFunction : ANativeFunction
    {
        public override int Arity { get => 0; }
        private readonly Stopwatch stopwatch;

        public ClockNativeFunction()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public override object Call(Interpreter interpreter, IList<object> arguments)
        {
            return (double)stopwatch.ElapsedMilliseconds;
        }

        public override string ToString()
        {
            return "<native fn:clock>";
        }
    }
}
