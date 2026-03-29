using ExpDataWorker.PropertySelectors;
using ExpDataWorkerBeta.Feature.Utils;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public static class QueryableExtensions
	{

		private const string GroupByKeyPrefix = "GroupKey";

		public static IQueryable<IGrouping<GroupByKeys, T>> ApplyGrouping<T>(this IQueryable<T> query, List<PropertySelector> groupByParameters)
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

		public static IQueryable<SelectResult> ApplySelect<T>(this IQueryable<IGrouping<GroupByKeys, T>> groupedQuery, List<PropertySelector> aggregateParameters)
		{
			var selectParam = Expression.Parameter(typeof(IGrouping<GroupByKeys, T>), "x");

			var aggregateValueListExpression = Expression.New(typeof(List<SelectValue>));
			var aggregateValueInits = new List<ElementInit>();
			var aggregateValueConstructor = typeof(SelectValue).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;

			var aggregateValueAddMethod = typeof(List<SelectValue>).GetMethod("Add")!;

			foreach (var aggregateSelector in aggregateParameters)
			{
				var aggregateExp = ExpressionUtils.CreateMemberAccess(selectParam, aggregateSelector);//ExpressionUtils.CreateGroupingMemberAccess(selectParam, aggregateSelector);

				var newAggregateValueExp = Expression.New(aggregateValueConstructor,
					Expression.Constant(aggregateSelector.PlainValue),
					Expression.Convert(
						aggregateExp,
						typeof(object)));


				var elementInitExpression = Expression.ElementInit(aggregateValueAddMethod, newAggregateValueExp);
				aggregateValueInits.Add(elementInitExpression);
			}

			var aggregateValueListInitExpression = Expression.ListInit(aggregateValueListExpression, aggregateValueInits);

			var aggregateResultConstructor = typeof(SelectResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(List<SelectValue>)]);
			var newAggregateResultExp = Expression.New(aggregateResultConstructor!,
				aggregateValueListInitExpression);

			var selectLambda = Expression.Lambda<Func<IGrouping<GroupByKeys, T>, SelectResult>>(
				newAggregateResultExp,
				selectParam);

			var selectedQuery = groupedQuery.Select(selectLambda);

			return selectedQuery;
		}

		public static IQueryable<SelectResult> ApplySelect<T>(this IQueryable<T> query, List<PropertySelector> aggregateParameters)
		{
			var selectParam = Expression.Parameter(typeof(T), "x");
			var aggregateValueListExpression = Expression.New(typeof(List<SelectValue>));
			var aggregateValueInits = new List<ElementInit>();
			var aggregateValueConstructor = typeof(SelectValue).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;
			var aggregateValueAddMethod = typeof(List<SelectValue>).GetMethod("Add")!;
			foreach (var aggregateSelector in aggregateParameters)
			{
				var aggregateExp = ExpressionUtils.CreateMemberAccess(selectParam, aggregateSelector);
				var newAggregateValueExp = Expression.New(aggregateValueConstructor,
					Expression.Constant(aggregateSelector.PlainValue),
					Expression.Convert(
						aggregateExp,
						typeof(object)));

				var elementInitExpression = Expression.ElementInit(aggregateValueAddMethod, newAggregateValueExp);
				aggregateValueInits.Add(elementInitExpression);
			}

			var aggregateValueListInitExpression = Expression.ListInit(aggregateValueListExpression, aggregateValueInits);

			var aggregateResultConstructor = typeof(SelectResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(List<SelectValue>)]);
			var newAggregateResultExp = Expression.New(aggregateResultConstructor!,
				aggregateValueListInitExpression);

			var selectLambda = Expression.Lambda<Func<T, SelectResult>>(
				newAggregateResultExp,
				selectParam);

			var selectedQuery = query.Select(selectLambda);

			return selectedQuery;
		}
	}
}
