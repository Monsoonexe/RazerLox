using System;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// Void represents nothing, very much like <see langword="void"/>.
    /// Void exists as an objectified void because C# can't do IVisitor{void} like Java can.
    /// </summary>
    public struct Void : IEquatable<Void>
    {
        public static readonly Void Default = default(Void);

        public static bool operator ==(Void left, Void right) => true;

        public static bool operator !=(Void left, Void right) => false;

        public override bool Equals(object obj)
        {
            return obj is Void;
        }

        public bool Equals(Void other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }
}
