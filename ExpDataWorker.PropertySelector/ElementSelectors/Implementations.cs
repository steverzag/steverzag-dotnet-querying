using ExpDataWorker.PropertySelectors.SelectorImplementations;
using ExpDataWorker.PropertySelectors.ValueSelectors;
using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelectors.ElementSelectors
{
	internal static class Implementations
	{
		internal static MethodCallExpression CreateElementFirstOrDefaultFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var firstOrDefaultMethod = MethodInfos.FirstOrDefaultMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);
			var result = Expression.Call(firstOrDefaultMethod, target);

			return result;
		}

		internal static MethodCallExpression CreateElementLastOrDefaultFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var lastOrDefaultMethod = MethodInfos.LastOrDefaultMethodInfo
					.MakeGenericMethod(elementTypeFromEnumerable!);
			var result = Expression.Call(lastOrDefaultMethod, target);

			return result;
		}

		internal static MethodCallExpression CreateElementAtFunction(this Expression target, PropertySelector index)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			int? indexValue = null;

			if(index is IntegerValueSelector intIndexSelector)
			{
				indexValue = intIndexSelector.Value;
			}


			if (indexValue == null)
			{
				throw new InvalidOperationException("Index value is not provided or is not an integer.");
			}

			var skipMethod = MethodInfos.SkipMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var skipedTarget = Expression.Call(skipMethod, target, Expression.Constant(indexValue.Value));

			var firstOrDefaultMethod = MethodInfos.FirstOrDefaultMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);
			var result = Expression.Call(firstOrDefaultMethod, skipedTarget);

			return result;
		}
	}

}
