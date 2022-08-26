
namespace LoxInterpreter.RazerLox
{
    internal class BreakStatementException : RuntimeException // to affect environment state
    {
        public BreakStatementException(Token token)
            : base(token, "A break statement was issued with no surrounding loop construct.")
        {
            // exists
        }
    }
}
