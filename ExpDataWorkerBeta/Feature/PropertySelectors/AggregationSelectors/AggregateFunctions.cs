using System.Linq.Expressions;
using ExpDataWorkerBeta.Feature.PropertySelectors.SelectorImplementations;
using ExpDataWorkerBeta.Feature.Utils;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.AggregationSelectors
{
	public static class AggregateFunctions
	{

		public static MethodCallExpression CreateAggregateCountFunction(this Expression target)
		{ 
			if(!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var countMethod = MethodInfos.CountMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var result = Expression.Call(countMethod, target);
			return result;
		}

		public static MethodCallExpression CreateAggregateSumFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var sumMethod = MethodInfos.SumMethodInfo;
			var result = Expression.Call(sumMethod, target);

			return result;
		}

		public static MethodCallExpression CreateAggregateMinFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var minMethod = MethodInfos.MinMethodInfo;
			return Expression.Call(minMethod, target);
		}

		public static MethodCallExpression CreateAggregateMaxFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var maxMethod = MethodInfos.MaxMethodInfo;
			return Expression.Call(maxMethod, target);
		}

		public static MethodCallExpression CreateAggregateAvgFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var avgMethod = MethodInfos.AverageMethodInfo;
			return Expression.Call(avgMethod, target);
		}
	}
}
