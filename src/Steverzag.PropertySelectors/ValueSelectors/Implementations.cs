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
