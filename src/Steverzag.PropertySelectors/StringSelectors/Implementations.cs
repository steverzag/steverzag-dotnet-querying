using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.SelectorImplementations;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.PropertySelectors.StringSelectors
{
	internal static class Implementations
	{
		/// <summary>
		/// Instance string members read their subject from the chained target, mirroring how the
		/// function is written in C#: <c>Name|$FN_STR_LENGTH</c> builds <c>x.Name.Length</c>.
		/// </summary>
		public static Expression CreateStringLengthAccess(this Expression? target)
		{
			var instanceExpr = target.CreateStringInstanceExpression(nameof(string.Length));
			return Expression.Property(instanceExpr, nameof(string.Length));
		}

		/// <summary>
		/// Calls <paramref name="methodInfo"/> on the chained target, so
		/// <c>Name|$FN_STR_CONTAINS("ell")</c> builds <c>x.Name.Contains("ell")</c>.
		/// The selector parameters carry only the arguments; because the target is already a
		/// resolved string expression, property parameters are resolved against the scope root
		/// the target itself was built from (<c>x</c> in the example above).
		/// </summary>
		public static Expression CreateStringInstanceFunction(this Expression? target, MethodInfo methodInfo, params PropertySelector[] parameters)
		{
			var instanceExpr = target.CreateStringInstanceExpression(methodInfo.Name);
			var argumentRoot = ExpressionUtils.ArgumentRoot(instanceExpr);
			var arguments = argumentRoot.CreateStringFunctionArguments(methodInfo, parameters);

			return Expression.Call(instanceExpr, methodInfo, arguments);
		}

		/// <summary>
		/// Calls a static member of <see cref="string"/> (e.g. <see cref="string.Concat(string, string)"/>),
		/// which has no instance form in C#. The target stays the root every parameter is resolved
		/// against and all values are passed as arguments.
		/// </summary>
		public static Expression CreateStringStaticFunction(this Expression? target, MethodInfo methodInfo, params PropertySelector[] parameters)
		{
			var arguments = target.CreateStringFunctionArguments(methodInfo, parameters);

			return Expression.Call(methodInfo, arguments);
		}

		/// <summary>
		/// Builds <c>string.Join(separator, new[] { value0, value1, … })</c>. Unlike the other static
		/// members the argument count is variadic, so the values are packed into a string array —
		/// the shape relational providers recognise. Like every static string function the target
		/// stays the root the parameters are resolved against.
		/// </summary>
		public static Expression CreateStringJoinFunction(this Expression? target, PropertySelector separatorParameter, params PropertySelector[] valueParameters)
		{
			var separatorExpr = ExpressionUtils.ResolveArgument(target, separatorParameter);

			if (separatorExpr.Type != typeof(string))
			{
				throw new InvalidOperationException($"The separator parameter must be of type String for {nameof(string.Join)} string function.");
			}

			var values = new Expression[valueParameters.Length];
			for (var i = 0; i < valueParameters.Length; i++)
			{
				var valueExpr = ExpressionUtils.ResolveArgument(target, valueParameters[i]);

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
				var argumentExpr = ExpressionUtils.ResolveArgument(target, parameters[i]);
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
