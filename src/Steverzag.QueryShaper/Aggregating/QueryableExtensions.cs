using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration.Mapping;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.QueryShaper.Aggregating
{
	public static class QueryableExtensions
	{

		private const string GroupByKeyPrefix = "GroupKey";

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
				var groupByExp = ExpressionUtils.CreateMemberAccess(param, groupBySelector);

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
				var aggregateExp = ExpressionUtils.CreateMemberAccess(paramExp, propertySelector);

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
				var selectorExp = ExpressionUtils.CreateMemberAccess(paramExp, propertySelector);

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

		public static IQueryable<TOut> ApplySelect<TIn, TOut>(this IQueryable<TIn> query, IMapper mapper)
		{
			ArgumentNullException.ThrowIfNull(mapper);

			return mapper.Map<TIn, TOut>(query);
		}
	}
}
