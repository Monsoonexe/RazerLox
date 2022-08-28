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
            if (fields.TryGetValue(identifier.lexeme, out object value))
                return value;

            throw new RuntimeException(identifier,
                $"Undefined property '{identifier.lexeme}'.");
        }

        internal void Set(Token identifier, object value)
        {
            fields[identifier.lexeme] = value;
        }
    }
}