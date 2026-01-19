using ExpDataWorkerBeta.Feature.PropertySelectors.OrderingSelector;
using ExpDataWorkerBeta.Feature.Utils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors
{
	public static class ElementFunctions
	{
		private readonly static MethodInfo _firstOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1);
		private readonly static MethodInfo _lastOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "LastOrDefault" && m.GetParameters().Length == 1);
		private readonly static MethodInfo _orderByAscendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
		private readonly static MethodInfo _orderByDescendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);

		public static MethodCallExpression CreateElementFirstOrDefaultFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var firstOrDefaultMethod = _firstOrDefaultMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var result = Expression.Call(firstOrDefaultMethod, target);

			return result;
		}

		public static MethodCallExpression CreateElementLastOrDefaultFunction(this Expression target, LastElementSelector lastElementSelector)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var parameterTypes = new List<Type>();

			foreach (var p in lastElementSelector.Parameters)
			{
				if (p is OrderAscendingSpecifierSelector or OrderDescendingSpecifierSelector)
				{ 
					parameterTypes.Add(typeof(SpecifierTypes.OrderSpecifiers));
					continue;
				}

				parameterTypes.Add(typeof(object));
			}

			var availableOverloads = lastElementSelector.Overloads
				.Where(ov => ov.Parameters.Select(p => p.Value).SequenceEqual(parameterTypes) 
					|| (ov.Parameters.Count == 0 && lastElementSelector.Parameters.Count == 0))
				.FirstOrDefault();

			if (availableOverloads is null) 
			{
				throw new InvalidOperationException("No matching overload found for the provided parameters in LastElementSelector function.");
			}

			if (lastElementSelector.Parameters.Count == 0)
			{
				var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
				target = Expression.Call(
					_orderByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
					target,
					Expression.Lambda(lambdaParam, lambdaParam)
				);
			}
			else if (lastElementSelector.Parameters.Count == 1 && lastElementSelector.Parameters.FirstOrDefault() is OrderAscendingSpecifierSelector)
			{
				var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
				target = Expression.Call(
					_orderByAscendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
					target,
					Expression.Lambda(lambdaParam, lambdaParam)
				);
			}
			else if (lastElementSelector.Parameters.Count == 1 && lastElementSelector.Parameters.FirstOrDefault() is OrderDescendingSpecifierSelector)
			{
				var lambdaParam = Expression.Parameter(elementTypeFromEnumerable!, "x");
				target = Expression.Call(
					_orderByDescendingMehodInfo.MakeGenericMethod(elementTypeFromEnumerable!, elementTypeFromEnumerable!),
					target,
					Expression.Lambda(lambdaParam, lambdaParam)
				);
			}

			var lastOrDefaultMethod = _lastOrDefaultMethodInfo
					.MakeGenericMethod(elementTypeFromEnumerable!);
			var result = Expression.Call(lastOrDefaultMethod, target);

			return result;
		}
	}

}
