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
        private readonly bool isInitializer;

        public int Arity => declaration.parameters.Count;

        #region Constructors

        /// <param name="closure">Lexical scope.</param>
        public LoxFunction(FunctionDeclaration declaration,
            Environment closure)
            : this(declaration, closure, false)
        {
            // exists
        }

        public LoxFunction(FunctionDeclaration declaration,
            Environment closure, bool isInitializer)
        {
            this.isInitializer = isInitializer;
            this.declaration = declaration;
            this.closure = closure;
        }

        #endregion Constructors

        /// <summary>
        /// Create a new <see cref="LoxFunction"/> with its 
        /// own class-local scope.
        /// </summary>
        internal LoxFunction Bind(LoxInstance instance)
        {
            var environment = new Environment(closure);
            environment.Define("this", instance);
            return new LoxFunction(declaration, environment,
                isInitializer);
        }

        public object Call(Interpreter interpreter,
            IList<object> arguments)
        {
            object returnValue = null; // 'nil' by default

            // init local scope
            var environment = new Environment(closure);
            for (int i = 0; i < arguments.Count; i++)
            {
                environment.Define(declaration.parameters[i].lexeme,
                    arguments[i]);
            }

            try // Execute!
            {
                interpreter.ExecuteBlock(declaration.body, environment);
            }
            catch (ReturnStatementException returnStatement)
            {
                returnValue = returnStatement.value;
            }

            // forcce initializer to always return 'this'
            if (isInitializer)
                returnValue = closure.GetAt(0, "this");

            return returnValue;
        }

        public override string ToString()
        {
            return $"<fn {declaration.identifier.lexeme}>";
        }

    }
}
