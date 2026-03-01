using ExpDataWorkerBeta.Feature.PropertySelectors.SelectorImplementations;
using ExpDataWorkerBeta.Feature.Utils;
using System.Linq.Expressions;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors
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
