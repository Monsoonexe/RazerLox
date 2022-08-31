﻿using LoxInterpreter.RazerLox.Callables;
using System;
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

        public int Arity => 0;

        public LoxClass(string identifier, Dictionary<string, LoxFunction> methods)
        {
            this.identifier = identifier;
            this.methods = methods;
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

        internal LoxFunction GetMethod(string lexeme)
        {
            if (methods.TryGetValue(lexeme, out LoxFunction method))
                return method;
            else
                return null;
        }
    }
}