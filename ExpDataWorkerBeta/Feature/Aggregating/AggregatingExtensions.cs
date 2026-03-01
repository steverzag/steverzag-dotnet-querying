using ExpDataWorker.PropertySelector;
using ExpDataWorkerBeta.Feature.Utils;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public static class AggregatingExtensions
	{

		private const string GroupByKeyPrefix = "GroupKey";
		public static IQueryable<AggregateResult> ApplyAggregate<T>(this IQueryable<T> query, AggregateParameters aggregateParameters)
		{

			ParameterExpression param = Expression.Parameter(typeof(T), "x");

			var newGroupByKeysExp = Expression.New(typeof(GroupByKeys));
			var groupKeysBindings = new List<MemberBinding>();

			for (int i = 0; i < aggregateParameters.GroupBySelectors.Count; i++)
			{
				var groupBySelector = aggregateParameters.GroupBySelectors[i];
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

			var selectParam = Expression.Parameter(groupedQuery.ElementType, "x");

			var groupedByPropertiesListExp = Expression.New(typeof(List<GroupedByProperty>));
			var groupedByPropInits = new List<ElementInit>();
			var groupedByPropertyConstructor = typeof(GroupedByProperty).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;
			
			var groupedByAddMethod = typeof(List<GroupedByProperty>).GetMethod("Add")!;

			for (int i = 0; i < aggregateParameters.GroupBySelectors.Count; i++)
			{
				var keyGroupSelector = $"{nameof(IGrouping<T, GroupByKeys>.Key)}.{GroupByKeyPrefix}{i}";
				var keyGroupExp = ExpressionUtils.CreateMemberAccess(selectParam, keyGroupSelector);
			
				var newGroupedByPropertyExp = Expression.New(groupedByPropertyConstructor,
					Expression.Constant(aggregateParameters.GroupBySelectors[i].PlainValue),
						keyGroupExp);
			
				var elementInitExpression = Expression.ElementInit(
					groupedByAddMethod, newGroupedByPropertyExp
				);
			
				groupedByPropInits.Add(elementInitExpression);
			}

			var groupedByListInitEspression = Expression.ListInit(groupedByPropertiesListExp, groupedByPropInits);

			var aggregateValueListExpression = Expression.New(typeof(List<AggregateValue>));
			var aggregateValueInits = new List<ElementInit>();
			var aggregateValueConstructor = typeof(AggregateValue).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)])!;

			var aggregateValueAddMethod = typeof(List<AggregateValue>).GetMethod("Add")!;

			foreach (var aggregateSelector in aggregateParameters.AggregateSelectors)
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

			var aggregateResultConstructor = typeof(AggregateResult).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(List<GroupedByProperty>), typeof(List<AggregateValue>)]);
			var newAggregateResultExp = Expression.New(aggregateResultConstructor!,
				groupedByListInitEspression,
				aggregateValueListInitExpression);

			var selectLambda = Expression.Lambda<Func<IGrouping<GroupByKeys, T>, AggregateResult>>(
				newAggregateResultExp,
				selectParam);

			var selectedQuery = groupedQuery.Select(selectLambda);

			return selectedQuery;
		}
	}
}
