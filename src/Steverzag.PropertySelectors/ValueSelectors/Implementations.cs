using System.Globalization;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal static class Implementations
	{

		internal static Expression CreateIntConstantAccesor(int value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}

		internal static Expression CreateDoubleConstantAccesor(double value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}

		internal static Expression CreateBoolConstantAccesor(bool value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}

		internal static Expression CreateStringConstantAccesor(string value)
		{
			var constantExpression = Expression.Constant(value, typeof(string));
			return constantExpression;
		}

		internal static string FormatStringLiteral(string value, string plainValue)
		{
			return value.Contains('"') ? plainValue : $"\"{value}\"";
		}

		internal static string FormatIntLiteral(int value, string plainValue)
		{
			return value >= 0 ? value.ToString(CultureInfo.InvariantCulture) : plainValue;
		}

		internal static string FormatBoolLiteral(bool value)
		{
			return value ? "true" : "false";
		}

		internal static Expression CreateDateTimeConstantAccesor(DateTime value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}

		internal static Expression CreateDateOnlyConstantAccesor(DateOnly value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}

		internal static Expression CreateTimeOnlyConstantAccesor(TimeOnly value)
		{
			var constantExpression = Expression.Constant(value);
			return constantExpression;
		}
	}
}
