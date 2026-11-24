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

		internal static MethodCallExpression CreateAggregateSumFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Sum));

		internal static MethodCallExpression CreateAggregateMinFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Min));

		internal static MethodCallExpression CreateAggregateMaxFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Max));

		internal static MethodCallExpression CreateAggregateAvgFunction(this Expression target)
			=> CallAggregate(target, nameof(Enumerable.Average));

		private static MethodCallExpression CallAggregate(Expression target, string methodName)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementType) || elementType is null)
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			return Expression.Call(GetAggregateMethod(methodName, elementType), target);
		}

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
