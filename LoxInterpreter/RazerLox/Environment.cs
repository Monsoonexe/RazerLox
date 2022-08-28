using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    internal class Environment
    {
        public const int DefaultMemorySize = 128;

        private readonly Dictionary<string, object> values 
            = new Dictionary<string, object>(DefaultMemorySize);
        public readonly Environment enclosing;

        public int VariableDefinitionCount { get => values.Count; }

        #region Constructors

        public Environment()
            : this (null)
        {
            
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        #endregion Constructors

        public void Define(string identifier, object value)
        {
            // allow redefining variables at the global level
            values[identifier] = value;
        }

        /// <exception cref="RuntimeException"></exception>
        public object Get(Token identifier)
        {
            string name = identifier.lexeme;
            if (values.TryGetValue(name, out object value))
            {
                return value;
            }
            else if (enclosing != null)
            {
                return enclosing.Get(identifier); // ask parent recursively
            }
            else
            {
                throw new RuntimeException(identifier,
                    $"Undefined variable '{name}'.");
            }
        }

        internal object GetAt(int distance, string name)
        {
            return GetAncestor(distance).values[name];
        }

        private Environment GetAncestor(int distance)
        {
            Environment environment = this; // return variable

            // walk up chain
            for (int i = 0; i < distance; ++i)
                environment = environment.enclosing;

            return environment;
        }

        public void Set(Token identifier, object value)
        {
            string name = identifier.lexeme;
            if (values.ContainsKey(name)) // is defined
            {
                values[name] = value;
            }
            else if (enclosing != null)
            {
                enclosing.Set(identifier, value); // check parent recursively
            }
            else // undefined variable
            {
                throw new RuntimeException(identifier,
                    $"Undefined variable '{name}'.");
            }
        }
        
        public void SetAt(Token identifier, object value, int distance)
        {
            GetAncestor(distance).values[identifier.lexeme] = value;
        }
    }
}
