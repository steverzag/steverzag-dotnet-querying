using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.SelectorImplementations;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.PropertySelectors.StringSelectors
{
	internal static class Implementations
	{
		public static Expression CreateStringLengthAccess(this Expression? target)
		{
			var instanceExpr = target.CreateStringInstanceExpression(nameof(string.Length));
			return Expression.Property(instanceExpr, nameof(string.Length));
		}

		public static Expression CreateStringInstanceFunction(this Expression? target, MethodInfo methodInfo, params PropertySelector[] parameters)
		{
			var instanceExpr = target.CreateStringInstanceExpression(methodInfo.Name);
			var argumentRoot = SelectorExpressions.ArgumentRoot(instanceExpr);
			var arguments = argumentRoot.CreateStringFunctionArguments(methodInfo, parameters);

			return Expression.Call(instanceExpr, methodInfo, arguments);
		}

		public static Expression CreateStringStaticFunction(this Expression? target, MethodInfo methodInfo, params PropertySelector[] parameters)
		{
			var arguments = target.CreateStringFunctionArguments(methodInfo, parameters);

			return Expression.Call(methodInfo, arguments);
		}

		public static Expression CreateStringJoinFunction(this Expression? target, PropertySelector separatorParameter, params PropertySelector[] valueParameters)
		{
			var separatorExpr = SelectorExpressions.ResolveArgument(target, separatorParameter);

			if (separatorExpr.Type != typeof(string))
			{
				throw new InvalidOperationException($"The separator parameter must be of type String for {nameof(string.Join)} string function.");
			}

			var values = new Expression[valueParameters.Length];
			for (var i = 0; i < valueParameters.Length; i++)
			{
				var valueExpr = SelectorExpressions.ResolveArgument(target, valueParameters[i]);

				if (valueExpr.Type != typeof(string))
				{
					// Providers translate the string array overload only; non string values would
					// need a ToString() call that is not guaranteed to be translatable.
					throw new InvalidOperationException($"Parameter {i + 2} must be of type String for {nameof(string.Join)} string function.");
				}

				values[i] = valueExpr;
			}

			return Expression.Call(MethodInfos.StringJoinMethodInfo, separatorExpr, Expression.NewArrayInit(typeof(string), values));
		}

		private static Expression CreateStringInstanceExpression(this Expression? target, string functionName)
		{
			if (target is null)
			{
				throw new InvalidOperationException($"The {functionName} string function must be applied to a string target.");
			}

			if (target.Type != typeof(string))
			{
				throw new InvalidOperationException($"The target must be of string type for {functionName} string function.");
			}

			return target;
		}

		private static Expression[] CreateStringFunctionArguments(this Expression? target, MethodInfo methodInfo, PropertySelector[] parameters)
		{
			var methodParameters = methodInfo.GetParameters();

			if (methodParameters.Length != parameters.Length)
			{
				throw new InvalidOperationException($"Method {methodInfo.Name} must have exactly {methodParameters.Length} parameters.");
			}

			var arguments = new Expression[parameters.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				var argumentExpr = SelectorExpressions.ResolveArgument(target, parameters[i]);
				var expectedType = methodParameters[i].ParameterType;

				if (argumentExpr.Type != expectedType)
				{
					if (ArithmeticUtils.IsNumericType(argumentExpr.Type) && ArithmeticUtils.IsNumericType(expectedType))
					{
						argumentExpr = Expression.Convert(argumentExpr, expectedType);
					}
					else
					{
						throw new InvalidOperationException($"Parameter {i + 1} must be of type {expectedType.Name} for {methodInfo.Name} string function.");
					}
				}

				arguments[i] = argumentExpr;
			}

			return arguments;
		}
	}
}
