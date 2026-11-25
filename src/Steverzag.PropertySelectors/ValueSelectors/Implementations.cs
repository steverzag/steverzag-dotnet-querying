using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal static class Implementations
	{
		internal static Expression CreateConstantAccessor<TValue>(TValue value)
		{
			return Expression.Constant(value, typeof(TValue));
		}
	}
}
