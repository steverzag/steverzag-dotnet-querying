using Steverzag.PropertySelectors.SelectorImplementations;
using System.IO;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.OrderingSelectors
{
	internal static class Implementations
	{
		internal static Type? OrderedType(Type? target) =>
			ResultTypes.ElementOf(target) is { } element ? typeof(IOrderedEnumerable<>).MakeGenericType(element) : null;

		internal static ArgumentConstraint OrderedCollection { get; } = ArgumentConstraint.Family(
			$"an ordered collection (sorted first with {SelectorDefaults.OrderAscending} or {SelectorDefaults.OrderDescending})",
			type => (type.IsInterface ? type.GetInterfaces().Append(type) : type.GetInterfaces())
				.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() is var d
					&& (d == typeof(IOrderedEnumerable<>) || d == typeof(IOrderedQueryable<>))));

		internal static MethodCallExpression CreateOrderByAscendingFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var orderByAscendingMehodInfo = MethodInfos.OrderByAscendingMethodInfo;
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

			var orderByAscendingMehodInfo = MethodInfos.OrderByAscendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			var innerAccess = ExpressionUtils.CreateMemberAccess(lambdaParam, orderByProperty);

			return Expression.Call(
				orderByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, innerAccess.Type!),
				target,
				Expression.Lambda(innerAccess, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateOrderByDescendingFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var orderByDescendingMehodInfo = MethodInfos.OrderByDescendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");

			return Expression.Call(
				orderByDescendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
				target,
				Expression.Lambda(lambdaParam, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateOrderByDescendingFunction(this Expression target, PropertySelector orderByProperty)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var orderByDescendingMehodInfo = MethodInfos.OrderByDescendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			var innerAccess = ExpressionUtils.CreateMemberAccess(lambdaParam, orderByProperty);

			return Expression.Call(
				orderByDescendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, innerAccess.Type!),
				target,
				Expression.Lambda(innerAccess, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateThenOrderByAscendingFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}
			var thenByAscendingMehodInfo = MethodInfos.ThenOrderByAscendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			return Expression.Call(
				thenByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
				target,
				Expression.Lambda(lambdaParam, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateThenOrderByAscendingFunction(this Expression target, PropertySelector orderByProperty)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}
			var thenByAscendingMehodInfo = MethodInfos.ThenOrderByAscendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			var innerAccess = ExpressionUtils.CreateMemberAccess(lambdaParam, orderByProperty);
			return Expression.Call(
				thenByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, innerAccess.Type!),
				target,
				Expression.Lambda(innerAccess, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateThenOrderByDescendingFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}
			var thenByDescendingMethodInfo = MethodInfos.ThenOrderByDescendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			return Expression.Call(
				thenByDescendingMethodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
				target,
				Expression.Lambda(lambdaParam, lambdaParam)
			);
		}

		internal static MethodCallExpression CreateThenOrderByDescendingFunction(this Expression target, PropertySelector orderByProperty)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable) && elementTypeFromEnumerable is not null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}
			var thenByDescendingMethodInfo = MethodInfos.ThenOrderByDescendingMethodInfo;
			var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
			var innerAccess = ExpressionUtils.CreateMemberAccess(lambdaParam, orderByProperty);
			return Expression.Call(
				thenByDescendingMethodInfo.MakeGenericMethod(elementTypeFromEnumerable!, innerAccess.Type!),
				target,
				Expression.Lambda(innerAccess, lambdaParam)
			);
		}
	}
}
