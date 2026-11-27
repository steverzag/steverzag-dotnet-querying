using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.MetaData;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Configuration.Ordering;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	internal sealed class DefaultQueryShaper : IQueryShaper
	{
		private readonly IMapper _mapper;
		private readonly IBaseSearchConfiguration _search;
		private readonly DefaultOrderConfiguration _defaultOrders;
		private readonly QueryShaperOptions _options;

		public DefaultQueryShaper(IMapper mapper, IBaseSearchConfiguration search, DefaultOrderConfiguration defaultOrders, IOptions<QueryShaperOptions> options)
		{
			ArgumentNullException.ThrowIfNull(mapper);
			ArgumentNullException.ThrowIfNull(search);
			ArgumentNullException.ThrowIfNull(defaultOrders);
			ArgumentNullException.ThrowIfNull(options);

			_mapper = mapper;
			_search = search;
			_defaultOrders = defaultOrders;
			_options = options.Value;

			if (_options.DefaultPageSize < 1 || _options.MaxPageSize < 1)
			{
				throw new InvalidOperationException(
					$"{nameof(QueryShaperOptions)}: {nameof(QueryShaperOptions.DefaultPageSize)} and {nameof(QueryShaperOptions.MaxPageSize)} must be at least 1.");
			}
			if (_options.DefaultPageSize > _options.MaxPageSize)
			{
				throw new InvalidOperationException(
					$"{nameof(QueryShaperOptions)}: {nameof(QueryShaperOptions.DefaultPageSize)} ({_options.DefaultPageSize}) can't be more than {nameof(QueryShaperOptions.MaxPageSize)} ({_options.MaxPageSize}).");
			}
			RequireAtLeastOne(_options.MaxFilters, nameof(QueryShaperOptions.MaxFilters));
			RequireAtLeastOne(_options.MaxListValues, nameof(QueryShaperOptions.MaxListValues));
			RequireAtLeastOne(_options.MaxOrders, nameof(QueryShaperOptions.MaxOrders));
			RequireAtLeastOne(_options.MaxSearchLength, nameof(QueryShaperOptions.MaxSearchLength));
			if (_options.MemberFilter is null)
			{
				throw new InvalidOperationException($"{nameof(QueryShaperOptions)}: {nameof(QueryShaperOptions.MemberFilter)} can't be null.");
			}
			if (_options.UnsortableTypes is null)
			{
				throw new InvalidOperationException($"{nameof(QueryShaperOptions)}: {nameof(QueryShaperOptions.UnsortableTypes)} can't be null.");
			}
		}

		public IQueryable<T> ApplySearch<T>(IQueryable<T> source, string? search)
		{
			ArgumentNullException.ThrowIfNull(source);
			if (search is not null && search.Length > _options.MaxSearchLength)
			{
				throw new QueryRequestException("", $"The search can be at most {_options.MaxSearchLength} characters long, not {search.Length}.");
			}
			return string.IsNullOrWhiteSpace(search) ? source : source.ApplyBaseFilter(search, _search);
		}

		public IQueryable<T> ApplyFilters<T>(IQueryable<T> source, IReadOnlyList<FilterNode> filters)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(filters);
			if (filters.Count == 0)
			{
				return source;
			}

			EnsureWithinFilterLimit(filters, _options.MaxFilters);
			ValidateNodes<T>(filters, "", _options.MemberFilter, _options.MaxListValues);
			return source.Where(FilteringExtensions.CreateFilterLambda<T>(filters, path: ""));
		}

		public IQueryable<T> ApplyOrder<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(order);
			return Order(source, order, _defaultOrders.For<T>());
		}

		private IQueryable<T> Order<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order, IReadOnlyList<OrderParameters>? tieBreaker)
		{
			ArgumentNullException.ThrowIfNull(order);
			if (order.Count == 0)
			{
				return tieBreaker is null || IsOrdered(source.Expression) ? source : source.ApplyOrder(tieBreaker);
			}
			if (order.Count > _options.MaxOrders)
			{
				throw new QueryRequestException("", $"A request can have at most {_options.MaxOrders} orders, not {order.Count}.");
			}

			var requested = new HashSet<string>(StringComparer.Ordinal);
			var parameter = Expression.Parameter(typeof(T), "p");
			for (var i = 0; i < order.Count; i++)
			{
				var item = order[i] ?? throw new QueryRequestException($"[{i}]", "An order can't be null.");
				try
				{
					var filter = _options.MemberFilter;
					PropertySelector<T> selector = item is OrderParameters<T> typed ? typed.Field : filter.Parse<T>(item.Field);
					if (item is OrderParameters<T> && !filter.IsAll)
					{
						filter.Parse<T>(selector.ToString());
					}
					var type = SelectorExpressions.CreateMemberAccess(parameter, selector).Type;
					type = Nullable.GetUnderlyingType(type) ?? type;
					if (_options.UnsortableTypes.Contains(type))
					{
						throw new QueryRequestException($"[{i}].field", $"'{item.Field}' can't be ordered: the database can't sort {TypeDisplay.Name(type)} values.");
					}
					requested.Add(selector.ToString());
				}
				catch (SelectorParseException ex)
				{
					throw new QueryRequestException($"[{i}].field", ex.Message, ex);
				}
			}

			if (tieBreaker is null)
			{
				return source.ApplyOrder(order);
			}

			var extra = tieBreaker.Where(t => !requested.Contains(SelectorText<T>(t)));
			return source.ApplyOrder([.. order, .. extra]);
		}

		private static string SelectorText<T>(OrderParameters order)
			=> order is OrderParameters<T> typed ? typed.Field.ToString() : ((PropertySelector<T>)order.Field).ToString();

		private static bool IsOrdered(Expression expression)
		{
			while (expression is MethodCallExpression call && call.Arguments.Count > 0 && typeof(IQueryable).IsAssignableFrom(call.Arguments[0].Type))
			{
				if (call.Method.DeclaringType == typeof(Queryable)
					&& call.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending))
				{
					return true;
				}
				expression = call.Arguments[0];
			}
			return false;
		}

		public IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source)
		{
			ArgumentNullException.ThrowIfNull(source);
			return source.ApplySelect<TIn, TOut>(_mapper);
		}

		public PageRequest ResolvePage(PageRequest? page)
		{
			if (page is null)
			{
				return new PageRequest(1, _options.DefaultPageSize);
			}
			if (page.Index < 1)
			{
				throw new QueryRequestException("index", $"The page index must be at least 1, not {page.Index}.");
			}
			if (page.Size is { } size && size < 1)
			{
				throw new QueryRequestException("size", $"The page size must be at least 1, not {size}.");
			}
			if (page.Size > _options.MaxPageSize)
			{
				throw new QueryRequestException("size", $"The page size can't be more than {_options.MaxPageSize}, not {page.Size}.");
			}

			return page with { Size = page.Size ?? _options.DefaultPageSize };
		}

		public IQueryable<T> Apply<T>(IQueryable<T> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(request);

			return SearchFilterAndOrder(source, request, _defaultOrders.For<T>());
		}

		public IQueryable<T> ApplyForPaging<T>(IQueryable<T> source, QueryRequest request, IReadOnlyList<OrderParameters>? fallbackOrder = null)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(request);

			var tieBreaker = _defaultOrders.For<T>() ?? (fallbackOrder is { Count: > 0 } ? fallbackOrder : null);
			var query = SearchFilterAndOrder(source, request, tieBreaker);
			if (!IsOrdered(query.Expression))
			{
				var name = typeof(T).Name;
				throw new InvalidOperationException(
					$"{name} can't be paged in a stable order: the request has no 'order', the source isn't ordered, and no default "
					+ $"order is configured. Configure one with ConfigureDefaultOrder<{name}>(…), or order the source.");
			}
			return query;
		}

		private IQueryable<T> SearchFilterAndOrder<T>(IQueryable<T> source, QueryRequest request, IReadOnlyList<OrderParameters>? tieBreaker)
		{
			var query = Within("search", () => ApplySearch(source, request.Search));
			query = Within("filters", () => ApplyFilters(query, request.Filters));
			return Within("order", () => Order(query, request.Order, tieBreaker));
		}

		public IQueryable<TOut> Apply<TIn, TOut>(IQueryable<TIn> source, QueryRequest request)
			=> Map<TIn, TOut>(Apply(source, request));

		internal static TResult Within<TResult>(string parent, Func<TResult> action)
		{
			try
			{
				return action();
			}
			catch (QueryRequestException ex)
			{
				throw ex.Within(parent);
			}
		}

		private static void EnsureWithinFilterLimit(IReadOnlyList<FilterNode> filters, int max)
		{
			var count = 0;
			var pending = new Stack<IReadOnlyList<FilterNode>>();
			pending.Push(filters);
			while (pending.TryPop(out var nodes))
			{
				foreach (var node in nodes)
				{
					if (++count > max)
					{
						throw new QueryRequestException("", $"A request can have at most {max} filters, groups included.");
					}
					if (node is GroupNode group)
					{
						pending.Push(group.Filters);
					}
				}
			}
		}

		private static void RequireAtLeastOne(int value, string name)
		{
			if (value < 1)
			{
				throw new InvalidOperationException($"{nameof(QueryShaperOptions)}: {name} must be at least 1.");
			}
		}

		private static void ValidateNodes<T>(IReadOnlyList<FilterNode> nodes, string path, MemberFilter filter, int maxListValues)
		{
			for (var i = 0; i < nodes.Count; i++)
			{
				var nodePath = $"{path}[{i}]";
				switch (nodes[i])
				{
					case null:
						throw new QueryRequestException(nodePath, "A filter can't be null.");
					case GroupNode group:
						ValidateNodes<T>(group.Filters, nodePath + ".filters", filter, maxListValues);
						break;
					case ConditionNode condition:
						try
						{
							_ = filter.Parse<T>(condition.Field.ToString());
						}
						catch (SelectorParseException ex)
						{
							throw new QueryRequestException(nodePath + ".field", ex.Message, ex);
						}
						if (condition.Value is { } value && value.GetKind() is FilterValueKind.Enumerable && ((List<object>)value).Count is var count && count > maxListValues)
						{
							throw new QueryRequestException(nodePath + ".value", $"A list can have at most {maxListValues} values, not {count}.");
						}
						break;
				}
			}
		}
	}
}
