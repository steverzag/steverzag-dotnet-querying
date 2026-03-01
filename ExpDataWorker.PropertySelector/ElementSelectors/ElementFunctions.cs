using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.SelectorImplementations;
using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelector.ElementSelectors
{
	public static class ElementFunctions
	{
		public static MethodCallExpression CreateElementFirstOrDefaultFunction(this Expression target)
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

		public static MethodCallExpression CreateElementLastOrDefaultFunction(this Expression target)
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
