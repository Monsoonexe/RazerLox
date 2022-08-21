
namespace LoxInterpreter.RazerLox
{
    internal class ParserException : RuntimeException
    {
        public ParserException(Token token, string message)
            : base(token, message)
        {
            // exists
        }
    }
}
