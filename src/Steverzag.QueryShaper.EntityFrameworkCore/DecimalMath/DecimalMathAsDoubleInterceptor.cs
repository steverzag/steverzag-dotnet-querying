using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	internal sealed class DecimalMathAsDoubleInterceptor : IQueryExpressionInterceptor
	{
		public static DecimalMathAsDoubleInterceptor Instance { get; } = new();

		private DecimalMathAsDoubleInterceptor() { }

		public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
			=> Rewriter.Instance.Visit(queryExpression);

		private sealed class Rewriter : ExpressionVisitor
		{
			public static readonly Rewriter Instance = new();

			private static readonly HashSet<string> Rewritten =
			[
				nameof(Math.Round),
				nameof(Math.Ceiling),
				nameof(Math.Floor),
				nameof(Math.Truncate),
			];

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var visited = (MethodCallExpression)base.VisitMethodCall(node);

				if (visited.Method.DeclaringType != typeof(Math)
					|| !Rewritten.Contains(visited.Method.Name)
					|| visited.Method.ReturnType != typeof(decimal))
					return visited;

				var doubleOverload = DoubleOverload(visited.Method);
				if (doubleOverload is null)
					return visited;

				var arguments = visited.Arguments
					.Select(a => a.Type == typeof(decimal) ? Expression.Convert(a, typeof(double)) : a);

				return Expression.Convert(Expression.Call(doubleOverload, arguments), typeof(decimal));
			}

			private static MethodInfo? DoubleOverload(MethodInfo decimalOverload)
			{
				var parameterTypes = decimalOverload.GetParameters()
					.Select(p => p.ParameterType == typeof(decimal) ? typeof(double) : p.ParameterType)
					.ToArray();

				return typeof(Math).GetMethod(decimalOverload.Name, parameterTypes);
			}
		}
	}
}
