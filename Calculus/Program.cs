using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Calculus
{
    class Program
    {
        #region Aliases [Long]
        private static readonly Dictionary<Type, string> Aliases =
            new Dictionary<Type, string>()
            {
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
                { typeof(object), "object" },
                { typeof(bool), "bool" },
                { typeof(char), "char" },
                { typeof(string), "string" },
                { typeof(void), "void" }
            };
        #endregion
        
        static void ExpressionEvalV1EntryPoint()
        {
            var parser = new LambdaExpressionParser();
            var example = "x1:int, x2:double => x1 * x2";
            var lambdaExample = parser.GetExpression(example);

            Console.WriteLine("Lambda Parser\nExample: {0} \nCompiles to: {1}".F(example, lambdaExample.ToString()));
            var lambdaString = ConsoleHelpers.GetConsoleInput("Lambda");

            var lambda = parser.GetExpression(lambdaString);

            Console.WriteLine("Derivative: {0}", lambda.Derive());
            Console.WriteLine("Compiled: {0}".F(lambda));
            var arguments = new List<object>();
            foreach (var para in ParameterExpressionWrapper.Parameters)
            {
                var type = para.Type;
                var str = ConsoleHelpers.GetConsoleInput("{1} =".F(type.ToString(), para.Name));
                arguments.Add(Convert.ChangeType(str, type));
            }
            Console.WriteLine($"Result: {lambda.Compile().DynamicInvoke(arguments.ToArray())}");
            Console.ReadLine();
        }

        static void Main()
        {
            ExpressionEvalV1EntryPoint();
        }
    }

    public abstract class WrapperBase
    {
    }

    public enum Parentheses
    {
        Left,
        Right
    }

    public class ParenthesesWrapper : WrapperBase
    {
        public Parentheses Parentheses;

        public ParenthesesWrapper(Parentheses parentheses)
        {
            Parentheses = parentheses;
        }

        public static string GetRegex()
        {
            return @"^()$";
        }

        public override string ToString()
        {
            return Parentheses == Parentheses.Left ? "(" : ")";
        }

        public static implicit operator ParenthesesWrapper(string input)
        {
            return input == "(" ? new ParenthesesWrapper(Parentheses.Left) : new ParenthesesWrapper(Parentheses.Right);
        }
    }

    public class ExpressionWrapper : WrapperBase
    {
        public Expression Expression;

        public ExpressionWrapper(Expression expression)
        {
            Expression = expression;
        }

        public static bool IsImplicitNumericConversion(Type source, Type destination)
        {
            TypeCode typeCode = Type.GetTypeCode(source);
            TypeCode code2 = Type.GetTypeCode(destination);
            switch (typeCode)
            {
                case TypeCode.Char:
                    switch (code2)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.SByte:
                    switch (code2)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;

                case TypeCode.Byte:
                    switch (code2)
                    {
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.Int16:
                    switch (code2)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.UInt16:
                    switch (code2)
                    {
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.Int32:
                    switch (code2)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.UInt32:
                    switch (code2)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    switch (code2)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    return false;

                case TypeCode.Single:
                    return (code2 == TypeCode.Double);

                default:
                    return false;
            }
            return false;
        }

        public static implicit operator ExpressionWrapper(string input)
        {
            throw new NotImplementedException();
        }

        public static implicit operator Expression(ExpressionWrapper expressionWrapper)
        {
            return expressionWrapper.Expression;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }

    public class ConstantExpressionWrapper : ExpressionWrapper
    {
        private static readonly List<Tuple<char, Type>> TypeInference = new List<Tuple<char, Type>>{
                new Tuple<char, Type>('d', typeof(double)),
                new Tuple<char, Type>('m', typeof(decimal)),
                new Tuple<char, Type>('f', typeof(float)),
                new Tuple<char, Type>('.', typeof(double)),
            };

        public ConstantExpressionWrapper(Expression expression) : base(expression)
        {
        }

        public static implicit operator ConstantExpressionWrapper(string input)
        {
            // Current weak implementation of basic implicit data types using suffixes
            var type = TypeInference.FirstOrDefault(t => input.ToLower().Contains(t.Item1));
            var dataType = typeof(int);
            var value = input as object;
            if (type != null)
            {
                dataType = type.Item2;
                value = input.TrimEnd(type.Item1);
            }
            value = Convert.ChangeType(value, dataType);
            var constant = Expression.Constant(value, dataType);
            return new ConstantExpressionWrapper(constant);
        }

        public static string GetRegex()
        {
            return @"^[0-9]([0-9.dfmDFM]+)?$";
        }
    }

    public class UnaryExpressionWrapper : WrapperBase
    {
        
    }

    public enum Associativity
    {
        Left,
        Right
    }

    public class OperatorExpressionWrapper : WrapperBase
    {
        private static readonly Dictionary<string, OperatorExpressionWrapper> Operators = new Dictionary<string, OperatorExpressionWrapper>() {
                { "!", new OperatorExpressionWrapper(ExpressionType.Not, Associativity.Left, 1, 1) },
                { "&", new OperatorExpressionWrapper(ExpressionType.And, Associativity.Left, 1) },
                { "|", new OperatorExpressionWrapper(ExpressionType.Or, Associativity.Left, 1) },
                { "^^", new OperatorExpressionWrapper(ExpressionType.ExclusiveOr, Associativity.Left, 1) },
                { "!=", new OperatorExpressionWrapper(ExpressionType.NotEqual, Associativity.Left, 1) },
                { ">", new OperatorExpressionWrapper(ExpressionType.GreaterThan, Associativity.Left, 1) },
                { ">=", new OperatorExpressionWrapper(ExpressionType.GreaterThanOrEqual, Associativity.Left, 1) },
                { "<", new OperatorExpressionWrapper(ExpressionType.LessThan, Associativity.Left, 1) },
                { "<=", new OperatorExpressionWrapper(ExpressionType.LessThanOrEqual, Associativity.Left, 1) },
                { "==", new OperatorExpressionWrapper(ExpressionType.Equal, Associativity.Left, 1) },
                { "+", new OperatorExpressionWrapper(ExpressionType.Add, Associativity.Left, 2) },
                { "-", new OperatorExpressionWrapper(ExpressionType.Subtract, Associativity.Left, 2) },
                { "*", new OperatorExpressionWrapper(ExpressionType.Multiply, Associativity.Left, 3) },
                { "/", new OperatorExpressionWrapper(ExpressionType.Divide, Associativity.Left, 3) },
                { "^", new OperatorExpressionWrapper(ExpressionType.Power, Associativity.Right, 4) },
            };
        public ExpressionType OperatorType;
        public Associativity Associativity;
        public int Precedence;
        public int Degree;

        public OperatorExpressionWrapper(ExpressionType operatorType, Associativity associative, int precedence, int degree = 2)
        {
            OperatorType = operatorType;
            Precedence = precedence;
            Associativity = associative;
            Degree = degree;
        }

        public static string GetRegex()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var op in Operators)
                sb.Append(@"\{0}".F(op.Key));
            return @"^[{0}]$".F(sb.ToString());
        }

        public override string ToString()
        {
            return OperatorType.ToString();
        }

        public static implicit operator OperatorExpressionWrapper(string input)
        {
            return Operators[input];
        }
    }

    public class ParameterExpressionWrapper : ExpressionWrapper
    {
        public ParameterExpressionWrapper(Expression expression) : base(expression)
        {
        }

        private static readonly Dictionary<Type, string> aliases =
    new Dictionary<Type, string>()
                {
                    { typeof(byte), "byte" },
                    { typeof(sbyte), "sbyte" },
                    { typeof(short), "short" },
                    { typeof(ushort), "ushort" },
                    { typeof(int), "int" },
                    { typeof(uint), "uint" },
                    { typeof(long), "long" },
                    { typeof(ulong), "ulong" },
                    { typeof(float), "float" },
                    { typeof(double), "double" },
                    { typeof(decimal), "decimal" },
                    { typeof(object), "object" },
                    { typeof(bool), "bool" },
                    { typeof(char), "char" },
                    { typeof(string), "string" },
                    { typeof(void), "void" }
                };

        public static List<ParameterExpression> Parameters = new List<ParameterExpression>();

        public static implicit operator ParameterExpressionWrapper(string input)
        {
            if (IsDeclaration(input))
                return AddParameter(input);
            var parameter = Parameters.Find(p => p.Name == input);
            if (parameter == null)
                throw new Exception("Parameter input incorrect.");
            return new ParameterExpressionWrapper(parameter);
        }

        public static string GetRegex()
        {
            return @"^[a-zA-Z]+([a-zA-Z0-9]+)?(:[a-zA-Z]+)?$";
        }

        public static bool IsDeclaration(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-Z]+([a-zA-Z0-9]+)?:[a-zA-Z.]+$");
        }

        public static ParameterExpressionWrapper AddParameter(string input)
        {
            var split = input.Split(':');
            Type type = null;
            if (aliases.ContainsValue(split[1]))
                type = aliases.FirstOrDefault(a => a.Value == split[1]).Key ?? Type.GetType(split[1]);
            var parameter = Expression.Parameter(type, split[0]);
            Parameters.Add(parameter);
            return new ParameterExpressionWrapper(parameter);
        }
    }

    public class LambdaExpressionParser
    {
        public List<char> Delimiters { get; set; }

        public LambdaExpressionParser(string defaultDelimiters = @"()-+/*^|&!")
        {
            Delimiters = new List<char>(defaultDelimiters.ToCharArray());
        }

        private static string[] Tokenize(string lambda, char[] delimiters)
        {
            var removeWhiteSpace = lambda.Replace(" ", string.Empty);
            var declarationSplit = removeWhiteSpace.Split("=>".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (declarationSplit.Length < 1 || declarationSplit.Length > 2)
                throw new Exception("Incorrect lambda format.");
            var parameterExtract = declarationSplit.First().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (declarationSplit.Length > 1)
            {
                foreach (var param in parameterExtract)
                {
                    ParameterExpressionWrapper parameter = param;
                    if (parameter == null)
                        throw new Exception("Parameter creation failed. Parameter string: {0}.\nLambda string: {1}".F(param, lambda));
                }
            }
            return declarationSplit.Last().Split(delimiters, true);
        }

        private static Queue<WrapperBase> Parse(string[] tokenized)
        {
            var operatorStack = new Stack<WrapperBase>();
            var outputQueue = new Queue<WrapperBase>();

            foreach (var token in tokenized)
            {
                if (Regex.IsMatch(token, ConstantExpressionWrapper.GetRegex()))
                {
                    ConstantExpressionWrapper constant = token;
                    outputQueue.Enqueue(constant);
                }
                else if (Regex.IsMatch(token, ParameterExpressionWrapper.GetRegex()))
                {
                    ParameterExpressionWrapper parameter = token;
                    outputQueue.Enqueue(parameter);
                }
                else if (Regex.IsMatch(token, OperatorExpressionWrapper.GetRegex()))
                {

                    OperatorExpressionWrapper o1 = token;
                    while (operatorStack.Count > 0)
                    {
                        var next = operatorStack.Peek();
                        var wrapper = next as OperatorExpressionWrapper;
                        if (wrapper != null)
                        {
                            var o2 = wrapper;
                            if ((o1.Associativity == Associativity.Left && o1.Precedence == o2.Precedence) || o1.Precedence < o2.Precedence)
                                outputQueue.Enqueue(operatorStack.Pop());
                            else
                                break;
                        }
                        else
                            break;
                    }
                    operatorStack.Push(o1);
                }
                else if (Regex.IsMatch(token, ParenthesesWrapper.GetRegex()))
                {
                    ParenthesesWrapper op = token;
                    if (op.Parentheses == Parentheses.Left)
                        operatorStack.Push(new ParenthesesWrapper(Parentheses.Left));
                    else
                    {
                        while (operatorStack.Count > 0)
                        {
                            var current = operatorStack.Peek();
                            var wrapper = current as ParenthesesWrapper;
                            var parentheses = wrapper;
                            if (parentheses?.Parentheses == Parentheses.Left)
                            {
                                operatorStack.Pop();
                                break;
                            }
                            outputQueue.Enqueue(operatorStack.Pop());
                        }
                    }
                }
            }

            while (operatorStack.Count > 0)
            {
                outputQueue.Enqueue(operatorStack.Pop());
            }

            return outputQueue;
        }

        public LambdaExpression CreateTree(Queue<WrapperBase> outputQueue)
        {
            var paramValueStack = new Stack<WrapperBase>();

            while (outputQueue.Count > 0)
            {
                var current = outputQueue.Dequeue();
                if (current is ExpressionWrapper)
                {
                    paramValueStack.Push(current);
                }
                else if (current is OperatorExpressionWrapper)
                {
                    var op = (OperatorExpressionWrapper)current;
                    var arg1 = ((ExpressionWrapper)paramValueStack.Pop()).Expression;
                    Expression expression;

                    if (op.Degree == 1)
                    {
                        expression = Expression.MakeUnary(op.OperatorType, arg1, arg1.Type);
                    }
                    else
                    {
                        var arg2 = ((ExpressionWrapper)paramValueStack.Pop()).Expression;
                        if (arg1.Type != arg2.Type)
                        {
                            if (ExpressionWrapper.IsImplicitNumericConversion(arg2.Type, arg1.Type))
                            {
                                arg2 = Expression.Convert(arg2, arg1.Type);
                            }
                            else if (ExpressionWrapper.IsImplicitNumericConversion(arg1.Type, arg2.Type))
                            {
                                arg1 = Expression.Convert(arg1, arg2.Type);
                            }
                        }
                        expression = Expression.MakeBinary(op.OperatorType, arg2, arg1);
                    }
                    paramValueStack.Push(new ExpressionWrapper(expression));
                }
            }
            return Expression.Lambda(((ExpressionWrapper)paramValueStack.Pop()).Expression, ParameterExpressionWrapper.Parameters);
        }

        public LambdaExpression GetExpression(string lambda)
        {
            ParameterExpressionWrapper.Parameters.Clear();
            var tokenized = Tokenize(lambda, Delimiters.ToArray());
            var outputQueue = Parse(tokenized);
            return CreateTree(outputQueue);
        }
    }

    public static class LambdaExtensions
    {
        public static LambdaExpression Derive(this LambdaExpression e)
        {
            // check not null expression
            if (e == null)
                throw new ExpressionExtensionsException("Expression must be non-null");
            // check just one param (variable)
                if (e.Parameters.Count != 1)
                throw new ExpressionExtensionsException("Incorrect number of parameters");
            // check right node type (maybe not necessary)
            if (e.NodeType != ExpressionType.Lambda)
                throw new ExpressionExtensionsException("Functionality not supported");
            // calc derivative
            return Expression.Lambda(e.Body.Derive(e.Parameters.First().Name), e.Parameters);
        }

        private static Expression Derive(this Expression e, string paramName)
        {
            switch (e.NodeType)
            {
                case ExpressionType.Convert:
                    var convert = ((UnaryExpression)e);
                    return Expression.Convert(convert.Operand, convert.Type);
                // constant rule
                case ExpressionType.Constant:
                    return Expression.Constant(0.0);
                // parameter
                case ExpressionType.Parameter:
                    return Expression.Constant(((ParameterExpression)e).Name == paramName ? 1.0 : 0.0);
                // sign change
                case ExpressionType.Negate:
                    Expression op = ((UnaryExpression)e).Operand;
                    return Expression.Negate(op.Derive(paramName));
                // sum rule
                case ExpressionType.Add:
                    {
                        Expression dleft =
                           ((BinaryExpression)e).Left.Derive(paramName);
                        Expression dright =
                           ((BinaryExpression)e).Right.Derive(paramName);
                        return Expression.Add(dleft, dright);
                    }
                // product rule
                case ExpressionType.Multiply:
                    {
                        Expression left = ((BinaryExpression)e).Left;
                        Expression right = ((BinaryExpression)e).Right;
                        Expression dleft = left.Derive(paramName);
                        Expression dright = right.Derive(paramName);
                        return Expression.Add(
                                Expression.Multiply(left, dright),
                                Expression.Multiply(dleft, right));
                    }
                // *** other node types here ***
                case ExpressionType.Power:
                    {
                        var one = Expression.Constant(1);
                        var left = ((BinaryExpression)e).Left;
                        var right = ((BinaryExpression)e).Right;
                        var leftMul = Expression.Multiply(right, left);
                        var rightPow = Expression.Subtract(right, Expression.Convert(one, right.Type));
                        return Expression.Power(leftMul, rightPow);
                    }
                default:
                    throw new ExpressionExtensionsException(
                        "Not implemented expression type: " + e.NodeType.ToString());
            }
        }

        public class ExpressionExtensionsException : Exception
        {
            public ExpressionExtensionsException(string msg) : base(msg, null) { }
            public ExpressionExtensionsException(string msg, Exception innerException) :
                    base(msg, innerException)
            { }
        }
    }

    public static class ConsoleHelpers
    {
        public static T GetConsoleInput<T>(string output) where T : struct, IConvertible
        {
            try
            {
                Console.Write(output + ": ");
                var type = typeof(T);
                var input = Console.ReadLine();
                return (T)Convert.ChangeType(input, type);
            }
            catch
            {
                Console.WriteLine("Could not evaluate input. Try again.");
                return GetConsoleInput<T>(output);
            }
        }
        public static string GetConsoleInput(string output)
        {
            bool first = true;
            string input = null;
            while (input == null)
            {
                if (!first)
                {
                    Console.WriteLine("Input cannot be empty.");
                }
                Console.Write(output + ": ");
                input = Console.ReadLine();
                first = false;
            }
            return input;
        }
    }

    public static class Extensions
    {
        public static string[] Split(this string value, char[] delimiters, bool keepValues)
        {
            if (!keepValues) return value.Split(delimiters);
            var split = new List<string>();
            var current = string.Empty;
            foreach (var t in value)
            {
                if (delimiters.Contains(t))
                {
                    if (!string.IsNullOrEmpty(current))
                        split.Add(current);
                    current = string.Empty;
                    split.Add(t.ToString());
                    continue;
                }
                current += t;
            }
            if (current.Length > 0)
                split.Add(current);
            return split.ToArray();
        }

        public static string F(this string format, params object[] args)
        {
            return string.Format(format, args);
        }
    }
}
