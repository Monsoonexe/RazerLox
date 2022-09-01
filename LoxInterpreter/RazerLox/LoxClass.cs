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
        public readonly LoxClass superclass;
        private readonly Dictionary<string, LoxFunction> methods;

        /// <summary>
        /// Initializer's arity, otherwise 0.
        /// </summary>
        public int Arity => GetMethod("init")?.Arity ?? 0;

        public LoxClass(string identifier,
            LoxClass superclass,
            Dictionary<string, LoxFunction> methods)
        {
            this.identifier = identifier;
            this.superclass = superclass;
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

        internal LoxFunction GetMethod(string identifier)
        {
            if (methods.TryGetValue(identifier, out LoxFunction method))
                return method;
            else if (superclass != null) // check inheritance structure
                return superclass.GetMethod(identifier);
            else
                return null;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}
