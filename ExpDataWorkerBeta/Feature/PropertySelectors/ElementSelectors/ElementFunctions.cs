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

		public static MethodCallExpression CreateElementLastOrDefaultFunction(this Expression target)
		{
			if (!ExpressionUtils.TryGetEnumerableElementType(target.Type, out var elementTypeFromEnumerable))
			{
				throw new InvalidOperationException("Target is not an enumerable type.");
			}

			var lastOrDefaultMethod = _lastOrDefaultMethodInfo
				.MakeGenericMethod(elementTypeFromEnumerable!);

			var result = Expression.Call(lastOrDefaultMethod, target);

			return result;
		}
	}

}
