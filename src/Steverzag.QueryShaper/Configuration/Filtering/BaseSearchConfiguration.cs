using Steverzag.QueryShaper.Filtering;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Configuration.Filtering
{
	public class BaseSearchConfiguration : IBaseSearchConfiguration
	{
		private readonly Dictionary<Type, LambdaExpression> _expressions = [];

		public BaseSearchConfiguration Configure<T>(Expression<Func<T, string, bool>> globalSearchExpression)
		{
			_expressions[typeof(T)] = globalSearchExpression;
			return this;
		}

		/// <summary>
		/// Configures the base search for <typeparamref name="T"/> from a set of property lookups,
		/// building an <c>OR</c> of <c>Contains</c> checks across every lookup. The search value is
		/// kept as a parameter so it can be substituted per-search by <see cref="GetSearchPredicate{T}"/>.
		/// </summary>
		public BaseSearchConfiguration Configure<T>(IEnumerable<string> searchLookups)
		{
			var lambda = FilteringExtensions.CreateBaseSearchLambdaExpression<T>(searchLookups, string.Empty);

			var entityParameter = lambda.Parameters[0];
			var searchParameter = Expression.Parameter(typeof(string), "_search_");
			var body = new ConstantReplacer(string.Empty, searchParameter).Visit(lambda.Body);

			_expressions[typeof(T)] = Expression.Lambda<Func<T, string, bool>>(body, entityParameter, searchParameter);
			return this;
		}

		public bool HasConfiguration<T>() => _expressions.ContainsKey(typeof(T));

		/// <summary>
		/// Returns the preconfigured base search for <typeparamref name="T"/> as a single-parameter
		/// predicate with <paramref name="searchValue"/> baked in, ready to pass to <c>Where</c>.
		/// </summary>
		public Expression<Func<T, bool>> GetSearchPredicate<T>(string searchValue)
		{
			var expression = GetSearchExpression<T>();

			var entityParameter = expression.Parameters[0];
			var searchParameter = expression.Parameters[1];

			var searchConstant = Expression.Constant(searchValue, typeof(string));
			var body = new ParameterReplacer(searchParameter, searchConstant).Visit(expression.Body);

			return Expression.Lambda<Func<T, bool>>(body, entityParameter);
		}

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

		private sealed class ConstantReplacer(string target, Expression replacement) : ExpressionVisitor
		{
			protected override Expression VisitConstant(ConstantExpression node)
				=> node.Type == typeof(string) && Equals(node.Value, target) ? replacement : base.VisitConstant(node);
		}
	}
}
