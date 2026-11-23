using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Ordering
{
	public static class OrderingExtensions
	{

		public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> query, PropertySelector selector, OrderDirection direction)
		{
			if (selector is null)
				return query;

			var paramExp = Expression.Parameter(typeof(T), name: "p");
			var propertyToOrderByExp = Expression.Convert(
				ExpressionUtils.CreateMemberAccess(paramExp, selector)
				, typeof(object));

			var orderByLambdaExp = Expression.Lambda<Func<T, object>>(propertyToOrderByExp, paramExp);

			query = direction == OrderDirection.Ascending
				? query.OrderBy(orderByLambdaExp)
				: query.OrderByDescending(orderByLambdaExp);

			return query;
		}

		public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> query, IEnumerable<OrderParameters> orderParameters)
		{
			if (orderParameters is null || !orderParameters.Any())
				return query;

			var isFirstIteration = true;

			foreach (var orderParam in orderParameters)
			{
				PropertySelector<T> selector = orderParam is OrderParameters<T> typed
					? typed.OrderBy
					: orderParam.OrderBy;

				var paramExp = Expression.Parameter(typeof(T), name: "p");
				var propertyToOrderByExp = Expression.Convert(
					ExpressionUtils.CreateMemberAccess(paramExp, selector)
					, typeof(object));

				var orderByLambdaExp = Expression.Lambda<Func<T, object>>(propertyToOrderByExp, paramExp);

				if (isFirstIteration)
				{
					query = orderParam.OrderDirection == OrderDirection.Ascending
						? query.OrderBy(orderByLambdaExp)
						: query.OrderByDescending(orderByLambdaExp);
					isFirstIteration = false;
				}
				else
				{
					var orderedQuery = query as IOrderedQueryable<T>;
					query = orderParam.OrderDirection == OrderDirection.Ascending
						? orderedQuery!.ThenBy(orderByLambdaExp)
						: orderedQuery!.ThenByDescending(orderByLambdaExp);
				}
			}

			return query;
		}
	}
}
