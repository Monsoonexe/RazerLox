using LoxInterpreter.RazerLox.Callables;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// Runtime representation of a class.
    /// </summary>
    internal class LoxClass
    {
        public readonly string identifier;

        public LoxClass(string identifier)
        {
            this.identifier = identifier;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}