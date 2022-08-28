
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
        private FunctionType currentFunction = FunctionType.None;
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

        private void ResolveFunction(FunctionDeclaration function, FunctionType type)
        {
            FunctionType enclosingFuntion = currentFunction; // push
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

        public Void VisitExitExpression(ExitExpression expression)
        {
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
            Declare(statement.identifier);
            Define(statement.identifier);
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

            ResolveFunction(statement, FunctionType.Function);
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
            if (currentFunction == FunctionType.None)
                Program.Error(statement.keyword,
                    "Can't return from top-level code.");

            // resolve
            if (statement.value != null)
                Resolve(statement.value);

            return Void.Default;
        }

        public Void VisitUnaryExpression(UnaryExpression expression)
        {
            Resolve(expression.right);
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
