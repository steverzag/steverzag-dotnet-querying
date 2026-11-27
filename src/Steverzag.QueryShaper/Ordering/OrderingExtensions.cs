using Steverzag.PropertySelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Ordering
{
	/// <summary>Orders a query by selectors.</summary>
	/// <remarks>
	/// For trusted input built in code: these methods have no options and no guard rails. Pass client requests
	/// through <see cref="IQueryShaper"/>, which validates them first.
	/// </remarks>
	public static class OrderingExtensions
	{
		/// <summary>Orders a query by one selector, replacing any earlier order.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="selector">The selector to order by, or <see langword="null"/> to leave the query unchanged.</param>
		/// <param name="direction">The direction.</param>
		/// <returns>The ordered query.</returns>
		public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> query, PropertySelector selector, OrderDirection direction)
		{
			if (selector is null)
				return query;

			var paramExp = Expression.Parameter(typeof(T), name: "p");
			var propertyToOrderByExp = Expression.Convert(
				SelectorExpressions.CreateMemberAccess(paramExp, selector)
				, typeof(object));

			var orderByLambdaExp = Expression.Lambda<Func<T, object>>(propertyToOrderByExp, paramExp);

			query = direction == OrderDirection.Ascending
				? query.OrderBy(orderByLambdaExp)
				: query.OrderByDescending(orderByLambdaExp);

			return query;
		}

		/// <summary>Orders a query by several selectors: the first as the primary sort, each next one as a tie-breaker.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="orderParameters">The orders. <see langword="null"/> or an empty list leaves the query unchanged.</param>
		/// <returns>The ordered query.</returns>
		/// <exception cref="PropertySelectors.SelectorParseException">A field isn't a valid selector for <typeparamref name="T"/>.</exception>
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
					SelectorExpressions.CreateMemberAccess(paramExp, selector)
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
