using System.Collections.Generic;

namespace LoxInterpreter.RazerLox.Callables
{
    /// <summary>
    /// Wraps compile-time function definition syntax node 
    /// in its runtime representation.
    /// </summary>
    internal class LoxFunction : ILoxCallable
    {
        private readonly FunctionDeclaration declaration;
        private readonly Environment closure;

        public int Arity => declaration.parameters.Count;

        /// <param name="closure">Lexical scope.</param>
        public LoxFunction(FunctionDeclaration declaration,
            Environment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        /// <summary>
        /// Create a new <see cref="LoxFunction"/> with its 
        /// own class-local scope.
        /// </summary>
        internal LoxFunction Bind(LoxInstance instance)
        {
            var environment = new Environment(closure);
            environment.Define("this", instance);
            return new LoxFunction(declaration, environment);
        }

        public object Call(Interpreter interpreter,
            IList<object> arguments)
        {
            // init local scope
            var environment = new Environment(closure);
            for (int i = 0; i < arguments.Count; i++)
                environment.Define(declaration.parameters[i].lexeme,
                    arguments[i]);

            try
            {
                interpreter.ExecuteBlock(declaration.body, environment);
            }
            catch (ReturnStatementException returnStatement)
            {
                return returnStatement.value;
            }

            return null; // TODO - return values
        }

        public override string ToString()
        {
            return $"<fn {declaration.identifier.lexeme}>";
        }

    }
}
