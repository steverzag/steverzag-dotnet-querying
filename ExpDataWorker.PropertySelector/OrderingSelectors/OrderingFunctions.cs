using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.SelectorImplementations;
using System.IO;
using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelector.OrderingSelectors
{
	internal static class OrderingFunctions
	{
		internal static MethodCallExpression CreateOrderByAscendingFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var orderByAscendingMehodInfo = MethodInfos.OrderByAscendingMehodInfo;

			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			return Expression.Call(
				orderByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
				target,
				Expression.Lambda(lambdaParam, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateOrderByAscendingFunction(this Expression target, PropertySelector orderByProperty)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var orderByAscendingMehodInfo = MethodInfos.OrderByAscendingMehodInfo;

			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			var innerAccess = ExpressionUtils.CreateMemberAccess(lambdaParam, orderByProperty);
			return Expression.Call(
				orderByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, innerAccess.Type!),
				target,
				Expression.Lambda(innerAccess, lambdaParam)
			);
		}
	}
}
