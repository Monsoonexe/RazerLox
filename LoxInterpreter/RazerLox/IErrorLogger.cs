
namespace LoxInterpreter.RazerLox
{
    public interface IErrorLogger
    {
        void Error(int line, string message);
        void Error(Token token, string message);
    }
}
