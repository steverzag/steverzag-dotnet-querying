using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration.Mapping;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.QueryShaper.Aggregating
{
	/// <summary>Groups, aggregates and projects a query.</summary>
	/// <remarks>
	/// For trusted input built in code: these methods have no options and no guard rails. Pass client requests
	/// through <see cref="IQueryShaper"/>, which validates them first.
	/// </remarks>
	public static class QueryableExtensions
	{
		private const string GroupByKeyPrefix = "GroupKey";

		/// <summary>Groups a query by up to 21 selectors. Follow it with <see cref="ApplySelect{T}(IQueryable{IGrouping{GroupByKeys, T}}, List{string})"/>.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="groupByParameters">The grouping selectors, in order: the first is <c>Key.GroupKey0</c>, the next <c>Key.GroupKey1</c>, and so on.</param>
		/// <returns>The grouped query.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="groupByParameters"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">There are no selectors, more than <see cref="GroupByKeys.MaxKeys"/>, or a <see langword="null"/> one.</exception>
		/// <example>
		/// <code>
		/// db.Products
		///     .ApplyGrouping(["Category.Name"])
		///     .ApplySelect(["Key.GroupKey0", "$FN_AGGR_COUNT", "Quantity|$FN_AGGR_SUM"]);
		/// </code>
		/// </example>
		public static IQueryable<IGrouping<GroupByKeys, T>> ApplyGrouping<T>(this IQueryable<T> query, List<PropertySelector<T>> groupByParameters)
		{
			ArgumentNullException.ThrowIfNull(groupByParameters);

			if (groupByParameters.Count == 0)
			{
				throw new ArgumentException("Grouping needs at least one key.", nameof(groupByParameters));
			}

			if (groupByParameters.Count > GroupByKeys.MaxKeys)
			{
				throw new ArgumentException(
					$"Grouping supports at most {GroupByKeys.MaxKeys} keys, but {groupByParameters.Count} were given.",
					nameof(groupByParameters));
			}

			if (groupByParameters.Any(key => key is null))
			{
				throw new ArgumentException("Grouping keys can't contain null.", nameof(groupByParameters));
			}

			ParameterExpression param = Expression.Parameter(typeof(T), "x");

			var newGroupByKeysExp = Expression.New(typeof(GroupByKeys));
			var groupKeysBindings = new List<MemberBinding>();

			for (int i = 0; i < groupByParameters.Count; i++)
			{
				var groupBySelector = groupByParameters[i];
				var groupByExp = SelectorExpressions.CreateMemberAccess(param, groupBySelector);

				var bindExpression = Expression.Bind(typeof(GroupByKeys).GetProperty(GroupByKeyPrefix + i)!, Expression.Convert(groupByExp, typeof(object)));
				groupKeysBindings.Add(bindExpression);
			}

			var groupByKeyInitExp = Expression.MemberInit(newGroupByKeysExp, groupKeysBindings);

			var groupByMethod = typeof(Enumerable).GetMethods()
				.Where(m => m.Name == "GroupBy" && m.GetParameters().Length == 2)
				.First()
				.MakeGenericMethod(typeof(T), typeof(GroupByKeys));

			var groupByLambda = Expression.Lambda<Func<T, GroupByKeys>>(
				groupByKeyInitExp,
				param);

			var groupedQuery = query
				.GroupBy(groupByLambda);

			return groupedQuery;
		}

		/// <summary>
		/// Returns one <see cref="SelectResult"/> per group. The selectors resolve against the group: <c>Key.GroupKey0</c>
		/// for a key, <c>$FN_AGGR_COUNT</c> for the number of rows, <c>Quantity|$FN_AGGR_SUM</c> for an aggregate over the
		/// group's rows.
		/// </summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="groupedQuery">The query from <see cref="ApplyGrouping"/>.</param>
		/// <param name="aggregateSelectors">The selectors, one value each, in order.</param>
		/// <returns>The projected query.</returns>
		/// <exception cref="PropertySelectors.SelectorParseException">A selector isn't valid against the group.</exception>
		public static IQueryable<SelectResult> ApplySelect<T>(this IQueryable<IGrouping<GroupByKeys, T>> groupedQuery, List<string> aggregateSelectors)
		{
			var paramExp = Expression.Parameter(typeof(IGrouping<GroupByKeys, T>), "x");
			var constructorInfo = typeof(SelectResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(List<SelectValue>)]);
			var selectValueListExpression = Expression.New(typeof(List<SelectValue>));
			Expression<Func<IGrouping<GroupByKeys, T>, SelectResult>> selectLambda = null!;

			if (aggregateSelectors.Count == 0)
			{
				var resultNewExp = Expression.New(constructorInfo!,
					selectValueListExpression);

				selectLambda = Expression.Lambda<Func<IGrouping<GroupByKeys, T>, SelectResult>>(
					resultNewExp,
					paramExp);
				return groupedQuery.Select(selectLambda);
			}

			var aggregateValueInits = new List<ElementInit>();
			var aggregateValueAddMethod = typeof(List<SelectValue>).GetMethod("Add")!;
			var selectValueConstructor = typeof(SelectValue).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;

			foreach (var selectorText in aggregateSelectors)
			{
				PropertySelector<IGrouping<GroupByKeys, T>> propertySelector = selectorText;
				var aggregateExp = SelectorExpressions.CreateMemberAccess(paramExp, propertySelector);

				var newAggregateValueExp = Expression.New(selectValueConstructor,
					Expression.Constant(selectorText),
					Expression.Convert(
						aggregateExp,
						typeof(object)));


				var elementInitExpression = Expression.ElementInit(aggregateValueAddMethod, newAggregateValueExp);
				aggregateValueInits.Add(elementInitExpression);
			}

			var aggregateValueListInitExpression = Expression.ListInit(selectValueListExpression, aggregateValueInits);
			var newAggregateResultExp = Expression.New(constructorInfo!,
				aggregateValueListInitExpression);

			selectLambda = Expression.Lambda<Func<IGrouping<GroupByKeys, T>, SelectResult>>(
				newAggregateResultExp,
				paramExp);

			return groupedQuery.Select(selectLambda);
		}

		/// <summary>Returns one <see cref="SelectResult"/> per row, with one value per selector.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="selectors">The selectors, one value each, in order.</param>
		/// <returns>The projected query.</returns>
		/// <exception cref="PropertySelectors.SelectorParseException">A selector isn't valid for <typeparamref name="T"/>.</exception>
		public static IQueryable<SelectResult> ApplySelect<T>(this IQueryable<T> query, List<string> selectors)
		{

			var paramExp = Expression.Parameter(typeof(T), "x");
			var constructorInfo = typeof(SelectResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(List<SelectValue>)]);
			var selectValueListExpression = Expression.New(typeof(List<SelectValue>));
			Expression<Func<T, SelectResult>> selectLambda = null!;

			if (selectors.Count == 0)
			{
				var resultNewExp = Expression.New(constructorInfo!, 
					selectValueListExpression);

				selectLambda = Expression.Lambda<Func<T, SelectResult>>(
					resultNewExp,
					paramExp);
				return query.Select(selectLambda);
			}

			var selectValueInits = new List<ElementInit>();
			var selectValueAddMethod = typeof(List<SelectValue>).GetMethod("Add")!;
			var selectValueConstructor = typeof(SelectValue).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;

			foreach (var selectorText in selectors)
			{
				PropertySelector<T> propertySelector = selectorText;
				var selectorExp = SelectorExpressions.CreateMemberAccess(paramExp, propertySelector);

				var selectValueNewExp = Expression.New(selectValueConstructor!,
					Expression.Constant(selectorText),
					Expression.Convert(
						selectorExp,
						typeof(object)));

				var elementInitExpression = Expression.ElementInit(selectValueAddMethod, selectValueNewExp);
				selectValueInits.Add(elementInitExpression);
			}

			var selectValueListInitExpression = Expression.ListInit(selectValueListExpression, selectValueInits);

			var selectResultNewExp = Expression.New(constructorInfo!,
				selectValueListInitExpression);

			selectLambda = Expression.Lambda<Func<T, SelectResult>>(
				selectResultNewExp,
				paramExp);

			return query.Select(selectLambda);
		}

		/// <summary>Projects a query with the map registered from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="mapper">The registered maps, such as the <see cref="IMapper"/> <c>AddQueryShaper</c> registers.</param>
		/// <returns>The projected query.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No map is registered for the pair.</exception>
		public static IQueryable<TOut> ApplySelect<TIn, TOut>(this IQueryable<TIn> query, IMapper mapper)
		{
			ArgumentNullException.ThrowIfNull(mapper);

			return mapper.Map<TIn, TOut>(query);
		}
	}
}
