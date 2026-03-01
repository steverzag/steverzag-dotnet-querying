using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.SelectorImplementations;
using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelector.ElementSelectors
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
	}

}
