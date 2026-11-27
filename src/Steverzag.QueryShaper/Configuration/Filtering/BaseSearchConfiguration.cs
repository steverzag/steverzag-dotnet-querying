using Steverzag.QueryShaper.Filtering;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Configuration.Filtering
{
	/// <summary>
	/// The base search configured for each type. <c>AddQueryShaper</c> registers one, filled from
	/// <c>ConfigureSearch&lt;T&gt;</c>, as <see cref="IBaseSearchConfiguration"/>.
	/// </summary>
	public sealed class BaseSearchConfiguration : IBaseSearchConfiguration
	{
		private readonly Dictionary<Type, LambdaExpression> _expressions = [];

		/// <summary>Configures the search for <typeparamref name="T"/> as an expression of the row and the search text.</summary>
		/// <remarks>The expression isn't checked: what it matches is up to its author. It replaces any search configured for <typeparamref name="T"/>.</remarks>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="globalSearchExpression">The search, such as <c>(p, s) => p.Name.Contains(s) || p.Email.StartsWith(s)</c>.</param>
		/// <returns>This configuration, to chain more calls.</returns>
		public BaseSearchConfiguration Configure<T>(Expression<Func<T, string, bool>> globalSearchExpression)
		{
			_expressions[typeof(T)] = globalSearchExpression;
			return this;
		}

		/// <summary>
		/// Configures the search for <typeparamref name="T"/>: a row matches when any lookup contains the search text,
		/// ignoring case unless <paramref name="caseSensitive"/> is set. It replaces any search configured for <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// Ignoring case, a lookup matches when it contains the text as written, or when its lowercase form contains the
		/// text's. The database lowercases the column, so which letters fold is the provider's rule; SQLite's
		/// <c>lower()</c> folds ASCII letters only.
		/// </remarks>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchLookups">Selectors whose type is <see cref="string"/>, such as <c>Name</c> or <c>Description|$FN_COALESCE("")</c>.</param>
		/// <param name="caseSensitive"><see langword="true"/> to match only the text as written.</param>
		/// <returns>This configuration, to chain more calls.</returns>
		/// <exception cref="ArgumentException">A lookup isn't a string, or isn't a valid selector for <typeparamref name="T"/>.</exception>
		public BaseSearchConfiguration Configure<T>(IEnumerable<string> searchLookups, bool caseSensitive = false)
		{
			var entityParameter = Expression.Parameter(typeof(T), "x");
			var searchParameter = Expression.Parameter(typeof(string), "_search_");
			var body = FilteringExtensions.CreateBaseSearchBody(entityParameter, searchLookups, searchParameter, caseSensitive);

			_expressions[typeof(T)] = Expression.Lambda<Func<T, string, bool>>(body, entityParameter, searchParameter);
			return this;
		}

		/// <inheritdoc/>
		public bool HasConfiguration<T>() => _expressions.ContainsKey(typeof(T));

		/// <inheritdoc/>
		public Expression<Func<T, bool>> GetSearchPredicate<T>(string searchValue)
		{
			var expression = GetSearchExpression<T>();

			var entityParameter = expression.Parameters[0];
			var searchParameter = expression.Parameters[1];

			var body = new ParameterReplacer(searchParameter, ParameterHolder.Parameter(searchValue, typeof(string))).Visit(expression.Body);

			return Expression.Lambda<Func<T, bool>>(body, entityParameter);
		}

		/// <inheritdoc/>
		public bool TryGetSearchPredicate<T>(string searchValue, out Expression<Func<T, bool>>? predicate)
		{
			if (!HasConfiguration<T>())
			{
				predicate = null;
				return false;
			}

			predicate = GetSearchPredicate<T>(searchValue);
			return true;
		}

		private Expression<Func<T, string, bool>> GetSearchExpression<T>()
		{
			if (!_expressions.TryGetValue(typeof(T), out var expression))
				throw new InvalidOperationException(
					$"No base search registered for '{typeof(T).Name}'.");

			return (Expression<Func<T, string, bool>>)expression;
		}

		private sealed class ParameterReplacer(ParameterExpression target, Expression replacement) : ExpressionVisitor
		{
			protected override Expression VisitParameter(ParameterExpression node)
				=> node == target ? replacement : base.VisitParameter(node);
		}
	}
}
