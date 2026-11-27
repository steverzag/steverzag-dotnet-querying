using Steverzag.PropertySelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Ordering
{
	/// <remarks>
	/// For trusted input built in code: these methods have no options and no guard rails. Pass client requests
	/// through <see cref="IQueryShaper"/>, which validates them first.
	/// </remarks>
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
					? typed.Field
					: orderParam.Field;

				var paramExp = Expression.Parameter(typeof(T), name: "p");
				var propertyToOrderByExp = Expression.Convert(
					ExpressionUtils.CreateMemberAccess(paramExp, selector)
					, typeof(object));

				var orderByLambdaExp = Expression.Lambda<Func<T, object>>(propertyToOrderByExp, paramExp);

				if (isFirstIteration)
				{
					query = orderParam.Direction == OrderDirection.Ascending
						? query.OrderBy(orderByLambdaExp)
						: query.OrderByDescending(orderByLambdaExp);
					isFirstIteration = false;
				}
				else
				{
					var orderedQuery = query as IOrderedQueryable<T>;
					query = orderParam.Direction == OrderDirection.Ascending
						? orderedQuery!.ThenBy(orderByLambdaExp)
						: orderedQuery!.ThenByDescending(orderByLambdaExp);
				}
			}

			return query;
		}
	}
}
