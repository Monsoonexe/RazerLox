
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// Resolves variables in a 2nd pass through the tree.
    /// </summary>
    internal class Resolver : 
        AExpression.IVisitor<Void>,
        AStatement.IVisitor<Void>
    {
        private readonly Interpreter interpreter;
        private readonly Stack<Dictionary<string, bool>> scopes
            = new Stack<Dictionary<string, bool>>(16);

        /// <summary>
        /// The current type of function we are inside of.
        /// </summary>
        private EFunctionType currentFunction = EFunctionType.None;

        /// <summary>
        /// Flag to ensure we can't use 'this' outside of a member.
        /// </summary>
        private EClassType currentClassType = EClassType.None;
        private bool isInLoop = false;

        #region Constructors

        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        #endregion Constructors

        #region Helpers

        private void BeginScope()
        {
            scopes.Push(new Dictionary<string, bool>());
        }

        private void EndScope()
        {
            scopes.Pop();
        }

        private void Resolve(AExpression expression)
        {
            expression.Accept(this);
        }

        public void Resolve(AStatement statement)
        {
            statement.Accept(this);
        }

        public void Resolve(IList<AStatement> statements)
        {
            foreach (var s in statements)
                Resolve(s);
        }

        private void ResolveFunction(FunctionDeclaration function, EFunctionType type)
        {
            EFunctionType enclosingFuntion = currentFunction; // push
            currentFunction = type;

            // resolve
            BeginScope();
            foreach (Token param in function.parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(function.body);
            EndScope();

            currentFunction = enclosingFuntion; // pop
        }

        private void ResolveLocal(AExpression expression, Token identifier)
        {
            // starts at top of stack
            int scopeDepth = 0;
            foreach (var s in scopes)
            {
                if (s.ContainsKey(identifier.lexeme))
                {
                    interpreter.Resolve(expression, scopeDepth);
                    return;
                }
                else
                {
                    ++scopeDepth;
                }
            }
        }

        private void Declare(Token identifier)
        {
            if (scopes.Count == 0)
                return;

            var scope = scopes.Peek(); // peek
            if (scope.ContainsKey(identifier.lexeme))
                Program.Error(identifier,
                    $"A variable named {identifier.lexeme} already declared in this scope.");
            scope.Add(identifier.lexeme, false); // not init'd
        }

        private void Define(Token identifier)
        {
            if (scopes.Count == 0)
                return;

            scopes.Peek()[identifier.lexeme] = true; // it's alive!
        }

        #endregion Helpers

        #region Expression Visitors

        public Void VisitAssignmentExpression(AssignmentExpression expression)
        {
            Resolve(expression.value);
            ResolveLocal(expression, expression.identifier);
            return Void.Default;
        }

        public Void VisitBinaryExpression(BinaryExpression expression)
        {
            Resolve(expression.left);
            Resolve(expression.right);
            return Void.Default;
        }

        public Void VisitCallExpression(CallExpression expression)
        {
            Resolve(expression.callee);
            foreach (var arg in expression.args)
                Resolve(arg);
            return Void.Default;
        }

        public Void VisitGetExpression(GetExpression expression)
        {
            Resolve(expression.instance);
            return Void.Default;
        }

        public Void VisitGroupingExpression(GroupingExpression expression)
        {
            Resolve(expression.expression);
            return Void.Default;
        }

        public Void VisitLiteralExpression(LiteralExpression expression)
        {
            // nada
            return Void.Default;
        }

        public Void VisitLogicalExpression(LogicalExpression expression)
        {
            Resolve(expression.left);
            Resolve(expression.right);
            return Void.Default;
        }

        public Void VisitSetExpression(SetExpression expression)
        {
            Resolve(expression.value);
            Resolve(expression.instance);
            return Void.Default;
        }

        public Void VisitSuperExpression(SuperExpression expression)
        {
            ResolveLocal(expression, expression.keyword);
            return Void.Default;
        }

        public Void VisitThisExpression(ThisExpression expression)
        {
            // validate 'this' not used outside of method
            if (currentClassType == EClassType.None)
            {
                Program.Error(expression.keyword,
                    "Can't use 'this' outside of a class.");
            }
            else
            {
                // get 'this'
                ResolveLocal(expression, expression.keyword);
            }

            return Void.Default;
        }

        public Void VisitUnaryExpression(UnaryExpression expression)
        {
            Resolve(expression.right);
            return Void.Default;
        }

        public Void VisitVariableExpression(VariableExpression expression)
        {
            Token name = expression.identifier;

            // guard against using before defining
            if (scopes.Count != 0
                && scopes.Peek().TryGetValue(name.lexeme, out bool inited)
                && inited == false)
            {
                Program.Error(name,
                    "Can't read local variable in its own initializer.");
            }

            ResolveLocal(expression, name);
            return Void.Default;
        }

        #endregion Expression Visitors

        #region Statement Visitors

        public Void VisitBlockStatement(BlockStatement statement)
        {
            BeginScope();
            Resolve(statement.statements);
            EndScope();
            return Void.Default;
        }

        public Void VisitBreakStatement(BreakStatement statement)
        {
            if (!isInLoop)
                Program.Error(statement.token, "'break' is only allowed within loops.");
            return Void.Default;
        }

        public Void VisitClassDeclaration(ClassDeclaration statement)
        {
            // push state to stack
            EClassType enclosingClass = currentClassType;
            currentClassType = EClassType.Class; // set new state

            Declare(statement.identifier);
            Define(statement.identifier);

            // inheritance (could be local class)
            if (statement.superclass != null)
            {
                // prevent inheriting from self
                if (statement.identifier.lexeme.Equals(
                    statement.superclass.identifier.lexeme))
                {
                    Program.Error(statement.superclass.identifier,
                        $"Class cannot inherit from itself: {statement.superclass.identifier}.");
                }

                Resolve(statement.superclass);

                // resolve superclass scope
                BeginScope();
                scopes.Peek().Add("super", true);
            }

            // 'this'
            BeginScope();
            scopes.Peek().Add("this", true);

            // methods
            foreach (var method in statement.methods)
            {
                var declaration = method.identifier.lexeme.Equals("init")
                    ? EFunctionType.Initializer
                    : EFunctionType.Method;
                ResolveFunction(method, declaration);
            }

            EndScope(); // end 'this' scope

            if (statement.superclass != null)
                EndScope(); // superclass scope

            currentClassType = enclosingClass; // pop state
            return Void.Default;
        }

        public Void VisitExpressionStatement(ExpressionStatement statement)
        {
            Resolve(statement.expression);
            return Void.Default;
        }

        public Void VisitFunctionDeclaration(FunctionDeclaration statement)
        {
            Declare(statement.identifier);
            Define(statement.identifier);

            ResolveFunction(statement, EFunctionType.Function);
            return Void.Default;
        }

        public Void VisitIfStatement(IfStatement statement)
        {
            Resolve(statement.condition);
            Resolve(statement.thenBranch);
            if (statement.elseBranch != null)
                Resolve(statement.elseBranch);
            return Void.Default;
        }

        public Void VisitPrintStatement(PrintStatement statement)
        {
            Resolve(statement.expression);                
            return Void.Default;
        }

        public Void VisitReturnStatement(ReturnStatement statement)
        {
            // validate scope
            if (currentFunction == EFunctionType.None)
                Program.Error(statement.keyword,
                    "Can't return from top-level code.");

            // resolve
            if (statement.value != null)
            {
                if (currentFunction == EFunctionType.Initializer)
                {
                    Program.Error(statement.keyword,
                        "Can't return a value from an initializer.");
                }
                else
                {
                    Resolve(statement.value);
                }
            }

            return Void.Default;
        }

        public Void VisitVariableDeclaration(VariableDeclaration statement)
        {
            Declare(statement.identifier);
            Resolve(statement.initializer); // MUST be initialized
            Define(statement.identifier);
            return Void.Default;
        }

        public Void VisitWhileStatement(WhileStatement statement)
        {
            bool previouslyInLoop = isInLoop; // push
            isInLoop = true;
            Resolve(statement.condition);
            Resolve(statement.body);
            isInLoop = previouslyInLoop; // pop
            return Void.Default;
        }

        #endregion Statement Visitors
    }
}
