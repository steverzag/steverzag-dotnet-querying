using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpDataWorkerBeta.Feature.Utils;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.AggregationSelectors
{
	public static class AggregateFunctions
	{

		private const string AggregateCountFunctionName = "Count";
		private const string AggregateSumFunctionName = "Sum";
		private const string AggregateMinFunctionName = "Min";
		private const string AggregateMaxFunctionName = "Max";
		private const string AggregateAverageFunctionName = "Average";

		public static MethodCallExpression CreateAggregateCountFunction(this Expression target)
		{ 
			if(!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var countMethod = typeof(Enumerable).GetMethods()
				.First(m => m.Name == AggregateCountFunctionName && m.GetParameters().Length == 1)
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

			var sumMethod = typeof(Enumerable).GetMethods()
				.First(m => m.Name == AggregateSumFunctionName && m.GetParameters().Length == 1);

			var result = Expression.Call(sumMethod, target);

			return result;
		}

		public static MethodCallExpression CreateAggregateMinFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var minMethod = typeof(Enumerable).GetMethods()
				.First(m => m.Name == AggregateMinFunctionName && m.GetParameters().Length == 1);

			return Expression.Call(minMethod, target);
		}

		public static MethodCallExpression CreateAggregateMaxFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var maxMethod = typeof(Enumerable).GetMethods()
				.First(m => m.Name == AggregateMaxFunctionName && m.GetParameters().Length == 1);

			return Expression.Call(maxMethod, target);
		}

		public static MethodCallExpression CreateAggregateAvgFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var avgMethod = typeof(Enumerable).GetMethods()
				.First(m => m.Name == AggregateAverageFunctionName && m.GetParameters().Length == 1);

			return Expression.Call(avgMethod, target);
		}
	}
}
