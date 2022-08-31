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
        private readonly Dictionary<string, LoxFunction> methods;

        /// <summary>
        /// Initializer's arity, otherwise 0.
        /// </summary>
        public int Arity => GetMethod("init")?.Arity ?? 0;

        public LoxClass(string identifier,
            Dictionary<string, LoxFunction> methods)
        {
            this.identifier = identifier;
            this.methods = methods;
        }

        public object Call(Interpreter interpreter,
            IList<object> arguments)
        {
            var instance = new LoxInstance(this);
            LoxFunction initializer = GetMethod("init");
            if (initializer != null)
            {
                initializer.Bind(instance)
                    .Call(interpreter, arguments);
            }
                
            return instance;
        }

        internal LoxFunction GetMethod(string lexeme)
        {
            if (methods.TryGetValue(lexeme, out LoxFunction method))
                return method;
            else
                return null;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}
