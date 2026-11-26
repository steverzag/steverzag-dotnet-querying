using Steverzag.PropertySelectors.ArithmeticSelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.DateSelectors
{
	internal static class Implementations
	{
		private static readonly Type[] SupportedDateTypes = [typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly)];

		public static Expression CreateDateComponentAccess(this Expression? target, string componentName, PropertySelector parameter)
		{
			var instanceExpr = target.CreateDateInstanceExpression(parameter, componentName);

			var property = instanceExpr.Type.GetProperty(componentName)
				?? throw new InvalidOperationException($"{instanceExpr.Type.Name} does not expose the {componentName} component.");

			return Expression.Property(instanceExpr, property);
		}

		public static Expression CreateDateAddFunction(this Expression? target, string methodName, PropertySelector instanceParameter, PropertySelector amountParameter)
		{
			var instanceExpr = target.CreateDateInstanceExpression(instanceParameter, methodName);

			var method = FindAddMethod(instanceExpr.Type, methodName)
				?? throw new InvalidOperationException($"{instanceExpr.Type.Name} does not support the {methodName} function.");

			var amountExpr = ExpressionUtils.ResolveArgument(target, amountParameter);

			if (!ArithmeticUtils.IsNumericType(amountExpr.Type))
			{
				throw new InvalidOperationException($"The amount parameter must be of numeric type for {methodName} function.");
			}

			var expectedType = method.GetParameters()[0].ParameterType;
			if (amountExpr.Type != expectedType)
			{
				amountExpr = Expression.Convert(amountExpr, expectedType);
			}

			return Expression.Call(instanceExpr, method, amountExpr);
		}

		internal static ArgumentConstraint DateWith(string methodName)
		{
			var types = SupportedDateTypes.Where(type => FindAddMethod(type, methodName) is not null).ToArray();
			var names = types.Select(type => type.Name).ToArray();
			var description = names.Length == 1 ? $"a {names[0]}" : $"a {string.Join(", ", names[..^1])} or {names[^1]}";
			return ArgumentConstraint.Family(description, types.Contains);
		}

		private static System.Reflection.MethodInfo? FindAddMethod(Type type, string methodName) =>
			type.GetMethods().FirstOrDefault(m => m.Name == methodName
				&& m.GetParameters().Length == 1
				&& ArithmeticUtils.IsNumericType(m.GetParameters()[0].ParameterType));

		public static Type? DateAddResultType(Type? target, IReadOnlyList<Type?> arguments) =>
			arguments[0] is { } subject ? Nullable.GetUnderlyingType(subject) ?? subject : null;

		public static Expression CreateDateTimeStaticAccess(this Expression? target, string propertyName)
		{
			var property = typeof(DateTime).GetProperty(propertyName)!;
			return Expression.Property(null, property);
		}

		private static Expression CreateDateInstanceExpression(this Expression? target, PropertySelector parameter, string functionName)
		{
			var instanceExpr = ExpressionUtils.ResolveArgument(target, parameter);

			if (Nullable.GetUnderlyingType(instanceExpr.Type) is not null)
			{
				instanceExpr = Expression.Property(instanceExpr, "Value");
			}

			if (!SupportedDateTypes.Contains(instanceExpr.Type))
			{
				throw new InvalidOperationException($"The first parameter must be a date or time type for {functionName} function.");
			}

			return instanceExpr;
		}
	}
}
