using LoxInterpreter.RazerLox.Callables;
using System;
using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    public class Interpreter : AExpression.IVisitor<object>,
                               AStatement.IVisitor<Void> // statements produce no values
    {
        internal readonly Environment globalEnv;
        private readonly Dictionary<AExpression, int> locals
            = new Dictionary<AExpression, int>();

        /// <summary>
        /// Inner-most executing scope.
        /// </summary>
        private Environment environment;

        #region Constructors

        public Interpreter()
        {
            globalEnv = new Environment();
            environment = globalEnv;
            InitializeNativeFunctions();
        }

        #endregion Constructors

        #region Initialization

        private void InitializeNativeFunctions()
        {
            globalEnv.Define("clock", new ClockNativeFunction());
            globalEnv.Define("exit", new InlinedNativeFunction(1,
                (interp, args) =>
                {
                    int exitCode = GetIntegerArg(args[0]) ?? -101;
                    throw new ExitException(exitCode);
                }));
        }

        #endregion Initialization

        public void Interpret (AExpression expr)
        {
            try
            {
                object result = Evaluate(expr);
                Program.Print(Stringify(result));
            }
            catch (RuntimeException runEx)
            {
                Program.RuntimeError(runEx);
            }
            catch (ExitException exit)
            {
                // exit program
                Program.ExitPrompt(exit.ExitCode);
            }

        }

        public void Interpret (IEnumerable<AStatement> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (RuntimeException runEx)
            {
                Program.RuntimeError(runEx);
            }
            catch (ExitException exit)
            {
                // exit program
                Program.ExitPrompt(exit.ExitCode);
            }
        }

        private string Stringify(object value)
        {
            string str; // return value
            if (value is null)
            {
                str = "nil"; // TODO - const
            }
            else if (value is double d)
            {
                // convert to integer format if rounded.
                str = d.ToString();
                if (str.EndsWith(".0"))
                    str = str.Substring(0, str.Length - 2);
            }
            else
            {
                str = value.ToString();
            }
            return str;
        }

        #region Expression Visitors

        public object VisitAssignmentExpression(AssignmentExpression expression)
        {
            object value = Evaluate(expression.value);

            if (locals.TryGetValue(expression, out int distance))
                environment.SetAt(expression.identifier, value, distance);
            else
                globalEnv.Set(expression.identifier, value);
            return value;
        }

        /// <exception cref="RuntimeException"></exception>
        public object VisitBinaryExpression(BinaryExpression expression)
        {
            object left = Evaluate(expression.left);
            object right = Evaluate(expression.right);
            TokenType type = expression._operator.type;
            (double A, double B) operands;
            (int A, int B) integerOperands;

            switch (type)
            {
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                case TokenType.GREATER:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A > operands.B;
                case TokenType.GREATER_EQUAL:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A >= operands.B;
                case TokenType.LESS:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A < operands.B;
                case TokenType.LESS_EQUAL:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A <= operands.B;
                case TokenType.MINUS:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A - operands.B;
                case TokenType.SLASH:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    GuardAgainstDivideByZero(expression._operator, operands.B);
                    return operands.A / operands.B;
                case TokenType.STAR:
                    operands = CheckNumberOperands(expression._operator,
                        left, right);
                    return operands.A * operands.B;
                case TokenType.PLUS:
                {
                    if (left is double a && right is double b)
                    {
                        return a + b;
                    }
                    else if (left is string || right is string)
                    {
                        // support concatenation if either operand is a string
                        return left.ToString() + right.ToString();
                    }
                    throw new RuntimeException(expression._operator, "Operands must both be numbers or strings.");
                }
                case TokenType.AMPERSAND: // bitwise AND
                    integerOperands = CheckIntegerOperands(expression._operator,
                        left, right);
                    return (double)(integerOperands.A & integerOperands.B);
                case TokenType.PIPE: // bitwise OR
                    integerOperands = CheckIntegerOperands(expression._operator,
                        left, right);
                    return(double)(integerOperands.A | integerOperands.B);
                default:
                    throw GetImproperOperatorException(type, expression);
            }
        }

        /// <exception cref="RuntimeException"></exception>
        public object VisitCallExpression(CallExpression expression)
        {
            object callee = Evaluate(expression.callee);

            // handle args
            var args = new List<object>();
            foreach (var arg in expression.args)
                args.Add(Evaluate(arg));

            // validate callable
            if (!(callee is ILoxCallable function))
            {
                // panic!
                throw new RuntimeException(expression.paren,
                    "Can only call functions and classes.");
            }
            else if (args.Count != function.Arity) // arity
            {
                throw new RuntimeException(expression.paren,
                    $"Expected {function.Arity} arguments but got {args.Count}.");
            }

            return function.Call(this, args);
        }

        /// <exception cref="RuntimeException"></exception>
        public object VisitGetExpression(GetExpression expression)
        {
            object _object = Evaluate(expression.instance);
            if (_object is LoxInstance instance)
                return instance.Get(expression.identifier);

            throw new RuntimeException(expression.identifier,
                "Only instances have properties.");
        }

        public object VisitGroupingExpression(GroupingExpression expression)
        {
            return Evaluate(expression.expression);
        }

        public object VisitLiteralExpression(LiteralExpression expression)
        {
            return expression.value;
        }

        public object VisitLogicalExpression(LogicalExpression expression)
        {
            object left = Evaluate(expression.left);

            // includes short-circuit logic
            if (expression._operator.type == TokenType.OR)
            {
                if (IsTruthy(left))
                    return left;
            }
            else // implicitly TokenType.AND
            {
                if (!IsTruthy(left))
                    return left;
            }

            return Evaluate(expression.right);
        }

        /// <exception cref="RuntimeException"></exception>
        public object VisitSetExpression(SetExpression expression)
        {
            object _object = Evaluate(expression.instance);

            if (_object is LoxInstance instance)
            {
                object value = Evaluate(expression.value);
                instance.Set(expression.identifier, value);
                return value;
            }
            else
            {
                throw new RuntimeException(expression.identifier,
                    "Only instances have fields.");
            }
        }

        /// <exception cref="RuntimeException"></exception>
        public object VisitSuperExpression(SuperExpression expression)
        {
            int distance = locals[expression];
            var superclass = (LoxClass)environment.GetAt(
                distance, "super");

            // this is always bound inside super environment
            var instance = (LoxInstance)environment.GetAt(
                distance - 1, "this");

            // bind method to 'this' where method was declared (or throw)
            LoxFunction method = superclass.GetMethod(expression.method.lexeme)
                ?? throw new RuntimeException(expression.method,
                    $"Undefined property '{expression.method.lexeme}'.");
            return method.Bind(instance);
        }

        public object VisitThisExpression(ThisExpression expression)
        {
            return LookUpVariable(expression.keyword, expression);
        }

        /// <exception cref="Exception"/>
        public object VisitUnaryExpression(UnaryExpression expression)
        {
            object right = Evaluate(expression.right);
            TokenType token = expression._operator.type;

            switch (token)
            {
                case TokenType.BANG:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    return -CheckNumberOperand(expression._operator, right);
                default:
                    throw GetImproperOperatorException(token, expression);
            }
        }

        public object VisitVariableExpression(VariableExpression expression)
        {
            return LookUpVariable(expression.identifier, expression);
        }

        #endregion Expression Visitors

        #region Statement Visitors

        public Void VisitBlockStatement(BlockStatement statement)
        {
            ExecuteBlock(statement.statements, new Environment(environment));
            return Void.Default;
        }

        /// <exception cref="BreakStatementException"></exception>
        public Void VisitBreakStatement(BreakStatement statement)
        {
            throw new BreakStatementException(statement.token);
        }

        /// <exception cref="RuntimeException"></exception>
        public Void VisitClassDeclaration(ClassDeclaration statement)
        {
            // inheritance
            object superclass = null;
            LoxClass loxSuperclass = null; // will be assigned in type check
            if (statement.superclass != null)
            {
                superclass = Evaluate(statement.superclass);

                // can only inherit from classes
                if (superclass is LoxClass s)
                {   // resolve cast now
                    loxSuperclass = s;
                }
                else
                {
                    throw new RuntimeException(statement.superclass.identifier,
                        "Superclass must be a class.");
                }
            }

            // define
            string name = statement.identifier.lexeme;
            environment.Define(name, value: null);

            // handle superclass scope
            if (loxSuperclass != null)
            {
                // push scope to stack
                environment = new Environment(environment);
                environment.Define("super", superclass);
            }

            // define methods
            var methods = new Dictionary<string, LoxFunction>();
            foreach (var method in statement.methods)
            {
                bool isInitializer = method.identifier.lexeme.Equals("init");
                var function = new LoxFunction(
                    method, environment, isInitializer);
                methods.Add(method.identifier.lexeme, function);
            }

            // convert AST node into runtime object
            var _class = new LoxClass(name, loxSuperclass, methods);

            // pop superclass scope
            if (loxSuperclass != null)
                environment = environment.enclosing;

            environment.Set(statement.identifier, _class);
            return Void.Default;
        }

        public Void VisitExpressionStatement(ExpressionStatement statement)
        {
            _ = Evaluate(statement.expression);
            return Void.Default;
        }

        public Void VisitFunctionDeclaration(FunctionDeclaration statement)
        {
            // if you're not having fun, you're not doing it right
            var fun = new LoxFunction(statement, environment);
            // bind
            environment.Define(statement.identifier.lexeme, fun);
            return Void.Default;
        }

        public Void VisitIfStatement(IfStatement statement)
        {
            object conditional = Evaluate(statement.condition);
            if (IsTruthy(conditional))
                Execute(statement.thenBranch);
            else if (statement.elseBranch != null)
                Execute(statement.elseBranch);

            return Void.Default;
        }

        public Void VisitPrintStatement(PrintStatement statement)
        {
            object result = Evaluate(statement.expression);
            Program.Print(Stringify(result));
            return Void.Default;
        }

        /// <exception cref="ReturnStatementException"></exception>
        public Void VisitReturnStatement(ReturnStatement statement)
        {
            object value = null;
            if (statement.value != null)
                value = Evaluate(statement.value);

            throw new ReturnStatementException(value);
        }

        public Void VisitVariableDeclaration(VariableDeclaration statement)
        {
            object value = null; // Lox defaults to 'nil'

            // var x = 1;
            if (statement.initializer != null)
            {
                // how to prevent this:
                // var a = a + 1
                value = Evaluate(statement.initializer);
            }

            environment.Define(statement.identifier.lexeme, value);
            return Void.Default;
        }

        public Void VisitWhileStatement(WhileStatement statement)
        {
            try
            {
                while (IsTruthy(Evaluate(statement.condition)))
                {
                    Execute(statement.body);
                }
            }
            catch (BreakStatementException)
            {
                // ignore
            }

            return Void.Default;
        }

        #endregion Statement Visitors

        #region Evaluation Helpers

        public object Evaluate(AExpression expression)
        {
            return expression.Accept(this);
        }

        public void Execute(AStatement statement)
       {
            statement.Accept(this);
        }

        internal void ExecuteBlock(IEnumerable<AStatement> statements,
            Environment environment)
        {
            var previousEnvironment = this.environment; // push scope
            this.environment = environment;

            try
            {
                foreach (var stmt in statements)
                    Execute(stmt); // how to pass environment down?
            }
            finally // fail-safe
            {
                this.environment = previousEnvironment; // pop scope
            }
        }

        public void Resolve(AExpression expression, int scopeDepth)
        {
            locals.Add(expression, scopeDepth);
        }

        private object LookUpVariable(Token identifier, AExpression expression)
        {
            if (locals.TryGetValue(expression, out int distance))
                return environment.GetAt(distance, identifier.lexeme);
            else
                return globalEnv.Get(identifier);
        }

        /// <returns><paramref name="operand"/> cast to a <see cref="double"/>.</returns>
        /// <exception cref="RuntimeException"></exception>
        private static double CheckNumberOperand(Token _operator,
            object operand)
        {
            if (operand is double d)
                return d;
            throw new RuntimeException(_operator, "Operand must be a number.");
        }

        /// <returns><paramref name="operand"/> cast to a <see cref="double"/>.</returns>
        /// <exception cref="RuntimeException"></exception>
        private static (double, double) CheckNumberOperands(
            Token _operator, object left, object right)
        {
            if (!(left is double a))
            {
                throw new RuntimeException(_operator,
                    $"Left operand must be a number but is {left.GetType()}.");
                
            }
            else if(!(right is double b))
            {
                throw new RuntimeException(_operator,
                    $"Right operand must be a number but is {right.GetType()}.");
            }
            else
            {
                return (a, b);
            }
        }

        /// <exception cref="RuntimeException"></exception>
        private static (int, int) CheckIntegerOperands(
            Token _operator, object left, object right)
        {
            int x, y;
            if (!(left is double a))
            {
                throw new RuntimeException(_operator,
                    $"Left operand must be an integral number but is {left.GetType()}.");
            }
            else if (!(right is double b))
            {
                throw new RuntimeException(_operator,
                    $"Right operand must be an integral number but is {left.GetType()}.");
            }
            else if (a - (x = (int)a) != 0)
            {
                throw new RuntimeException(_operator,
                    $"Left operand must be an integral number but is {a}.");
            }
            else if (b - (y = (int)b) != 0)
            {
                throw new RuntimeException(_operator,
                    $"Right operand must be an integral number but is {right}.");
            }
            else
            {
                return (x, y);
            }
        }

        private static int? GetIntegerArg(object arg)
        {
            int? _integer;
            if (!(arg is double d))
            {
                _integer = null;
            }
            else if (d != (_integer = (int)d))
            {
                _integer = null;
            }
            return _integer;
        }

        /// <returns><see langword="true"/> if <paramref name="a"/> is a
        /// <see langword="double"/> with 0 in mantissa.</returns>
        private static bool IsInteger(object a)
        {
            return (a is double d) && (d == (int)d);
        }

        /// <summary>
        /// Lox's notion of 'equality' is different than C#'s.
        /// </summary>
        private static bool IsEqual(object a, object b)
        {
            if (a is null && b is null)
                return true;
            else if (a is null)
                return false;
            else
                return a.Equals(b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Truthiness.</returns>
        private static bool IsTruthy(object obj)
        {
            if (obj is null) // nil
                return false;
            else if (obj is bool b)
                return b;
            else return true;                
        }

        #endregion Evaluation Helpers

        private static Exception GetImproperOperatorException(
            TokenType t, AExpression exp)
        {
            return new InvalidOperationException(
                $"{t} is not valid for {exp.GetType().Name}.");
        }

        /// <summary>
        /// Challenge accepted.
        /// </summary>
        /// <exception cref="RuntimeException"></exception>
        private static void GuardAgainstDivideByZero(Token token, double denominator)
        {
            if (denominator == 0)
                throw new RuntimeException(token, "Cannot divide by 0!");
        }

    }
}
