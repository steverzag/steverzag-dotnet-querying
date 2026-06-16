using ExpDataWorker.PropertySelectors;
using ExpDataWorkerBeta.Feature.Configuration;
using ExpDataWorkerBeta.Feature.Configuration.Mapping;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public static class QueryableExtensions
	{

		private const string GroupByKeyPrefix = "GroupKey";

		public static IQueryable<IGrouping<GroupByKeys, T>> ApplyGrouping<T>(this IQueryable<T> query, List<PropertySelector<T>> groupByParameters)
		{
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

			List<PropertySelector<T>> propertySelectors = [];

			foreach (var aggregateParameter in aggregateSelectors)
			{
				propertySelectors.Add(aggregateParameter);
			}

			foreach (var propertySelector in propertySelectors)
			{
				var aggregateExp = ExpressionUtils.CreateMemberAccess(paramExp, propertySelector);//ExpressionUtils.CreateGroupingMemberAccess(selectParam, aggregateSelector);

				var newAggregateValueExp = Expression.New(selectValueConstructor,
					Expression.Constant(propertySelector.PlainValue),
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

			List<PropertySelector<T>> propertySelectors = [];

			foreach (var selector in selectors)
			{
				propertySelectors.Add(selector);
			}

			foreach (var propertySelector in propertySelectors)
			{
				var selectorExp = ExpressionUtils.CreateMemberAccess(paramExp, propertySelector);
				var selectValueNewExp = Expression.New(selectValueConstructor!,
					Expression.Constant(propertySelector.PlainValue),
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

		public static IQueryable<TOut> ApplySelect<TIn, TOut>(this IQueryable<TIn> query, IMapper? mapper = null)
		{
			mapper ??= DataWorkerServices.GetServiceOrNull<IMapper>();

			if (mapper is null)
			{
				throw new InvalidOperationException(
					$"No {nameof(IMapper)} was provided and none has been configured for types {typeof(TIn).Name} and {typeof(TOut).Name}" +
					$"Pass one explicitly or register it via AddDataWorker.");
			}

			return mapper.Map<TIn, TOut>(query);
		}
	}
}
