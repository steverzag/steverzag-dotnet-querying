using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.AggregationSelectors
{
	internal static class Implementations
	{

		internal static MethodCallExpression CreateAggregateCountFunction(this Expression target)
		{ 
			if(!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var countMethod = MethodInfos.CountMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var result = Expression.Call(countMethod, target);
			return result;
		}

		internal static MethodCallExpression CreateAggregateLongCountFunction(this Expression target)
		{ 
			if(!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var countMethod = MethodInfos.LongCountMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var result = Expression.Call(countMethod, target);
			return result;
		}

		internal static MethodCallExpression CreateAggregateCountDistinctFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType) || elementType is null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			if (!elementType.IsValueType || Nullable.GetUnderlyingType(elementType) is not null)
			{
				var item = Expression.Parameter(elementType, "x");
				var notNull = Expression.Lambda(Expression.NotEqual(item, Expression.Constant(null, elementType)), item);
				target = Expression.Call(MethodInfos.WhereMethodInfo.MakeGenericMethod(elementType), target, notNull);
			}

			var distinct = Expression.Call(MethodInfos.DistinctMethodInfo.MakeGenericMethod(elementType), target);
			return Expression.Call(MethodInfos.CountMethodInfo.MakeGenericMethod(elementType), distinct);
		}

		internal static MethodCallExpression CreateAggregateSumFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Sum));

		internal static MethodCallExpression CreateAggregateMinFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Min), liftToNullable: true);

		internal static MethodCallExpression CreateAggregateMaxFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Max), liftToNullable: true);

		internal static MethodCallExpression CreateAggregateAvgFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Average), liftToNullable: true);

		private static MethodCallExpression CallAggregate(Expression target, string methodName, bool liftToNullable = false)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType) || elementType is null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var lifted = liftToNullable ? LiftedElementType(elementType) : elementType;
			if (lifted != elementType)
			{
				var item = Expression.Parameter(elementType, "x");
				var lift = Expression.Lambda(Expression.Convert(item, lifted), item);
				target = Expression.Call(MethodInfos.SelectMethodInfo.MakeGenericMethod(elementType, lifted), target, lift);
			}

			return Expression.Call(GetAggregateMethod(methodName, lifted), target);
		}

		internal static Type LiftedElementType(Type elementType)
			=> elementType.IsValueType && Nullable.GetUnderlyingType(elementType) is null
				? typeof(Nullable<>).MakeGenericType(elementType)
				: elementType;

		internal static Type? LiftedAggregateType(string methodName, Type? elementType)
			=> elementType is null ? null : GetAggregateMethod(methodName, LiftedElementType(elementType)).ReturnType;

		private static readonly ConcurrentDictionary<(string Name, Type ElementType), MethodInfo> _aggregateMethods = new();

		internal static MethodInfo GetAggregateMethod(string methodName, Type elementType)
			=> _aggregateMethods.GetOrAdd((methodName, elementType), static key =>
			{
				var (name, element) = key;
				var sequenceType = typeof(IEnumerable<>).MakeGenericType(element);
				var methods = typeof(Enumerable).GetMethods().Where(m => m.Name == name && m.GetParameters().Length == 1).ToList();

				var exact = methods.FirstOrDefault(m => !m.IsGenericMethodDefinition && m.GetParameters()[0].ParameterType == sequenceType);
				if (exact is not null)
				{
					return exact;
				}

				if (name is nameof(Enumerable.Min) or nameof(Enumerable.Max))
				{
					var generic = methods.FirstOrDefault(m => m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1);
					if (generic is not null)
					{
						return generic.MakeGenericMethod(element);
					}
				}

				throw new InvalidOperationException($"{name} is not supported for elements of type {element.Name}.");
			});
	}
}
