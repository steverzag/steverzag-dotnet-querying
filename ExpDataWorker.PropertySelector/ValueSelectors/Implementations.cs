using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelectors.ValueSelectors
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
	}
}
