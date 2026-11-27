using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper.Pagination
{
	internal static class PrimaryKeyOrder
	{
		public static IReadOnlyList<OrderParameters>? For<T>(IQueryable<T> source)
		{
			var finder = new RootFinder();
			finder.Visit(source.Expression);

			var key = finder.Root?.EntityType is { } entityType && entityType.ClrType == typeof(T)
				? entityType.FindPrimaryKey()
				: null;
			if (key is null || key.Properties.Any(p => p.PropertyInfo is null))
			{
				return null;
			}

			return [.. key.Properties.Select(p => new OrderParameters(p.Name))];
		}

		private sealed class RootFinder : ExpressionVisitor
		{
			public EntityQueryRootExpression? Root { get; private set; }

			protected override Expression VisitExtension(Expression node)
			{
				if (node is EntityQueryRootExpression root)
				{
					Root ??= root;
					return node;
				}
				return base.VisitExtension(node);
			}
		}
	}
}
