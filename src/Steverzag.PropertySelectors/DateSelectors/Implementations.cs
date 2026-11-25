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

			// Resolved by name against the actual source type: DateTime.AddDays takes a double
			// while DateOnly.AddDays takes an int, and e.g. TimeOnly has no AddSeconds at all.
			var method = instanceExpr.Type.GetMethods()
				.FirstOrDefault(m => m.Name == methodName
					&& m.GetParameters().Length == 1
					&& ArithmeticUtils.IsNumericType(m.GetParameters()[0].ParameterType))
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
