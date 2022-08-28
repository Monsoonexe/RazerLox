using LoxInterpreter.RazerLox.Callables;
using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    internal class LoxInstance
    {
        private LoxClass loxClass;
        private readonly Dictionary<string, object> fields;

        public LoxInstance(LoxClass loxClass)
        {
            this.loxClass = loxClass;
            fields = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            return loxClass.identifier + " instance";
        }

        internal object Get(Token identifier)
        {
            // first check fields
            if (fields.TryGetValue(identifier.lexeme, out object value))
                return value;

            // then check methods
            LoxFunction method = loxClass.GetMethod(identifier.lexeme);
            if (method != null)
                return method;

            // unknown identifier
            throw new RuntimeException(identifier,
                $"Undefined property '{identifier.lexeme}'.");
        }

        internal void Set(Token identifier, object value)
        {
            fields[identifier.lexeme] = value;
        }
    }
}