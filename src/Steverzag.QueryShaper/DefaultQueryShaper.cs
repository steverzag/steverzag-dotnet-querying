using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.MetaData;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	internal sealed class DefaultQueryShaper : IQueryShaper
	{
		private readonly IMapper _mapper;
		private readonly IBaseSearchConfiguration _search;
		private readonly QueryShaperOptions _options;

		public DefaultQueryShaper(IMapper mapper, IBaseSearchConfiguration search, IOptions<QueryShaperOptions> options)
		{
			ArgumentNullException.ThrowIfNull(mapper);
			ArgumentNullException.ThrowIfNull(search);
			ArgumentNullException.ThrowIfNull(options);

			_mapper = mapper;
			_search = search;
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
			if (_options.MaxFilters < 1)
			{
				throw new InvalidOperationException($"{nameof(QueryShaperOptions)}: {nameof(QueryShaperOptions.MaxFilters)} must be at least 1.");
			}
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
			ValidateSelectors<T>(filters, "", _options.MemberFilter);
			return source.Where(FilteringExtensions.CreateFilterLambda<T>(filters, path: ""));
		}

		public IQueryable<T> ApplyOrder<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(order);
			if (order.Count == 0)
			{
				return source;
			}

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
					var type = ExpressionUtils.CreateMemberAccess(parameter, selector).Type;
					type = Nullable.GetUnderlyingType(type) ?? type;
					if (_options.UnsortableTypes.Contains(type))
					{
						throw new QueryRequestException($"[{i}].field", $"'{item.Field}' can't be ordered: the database can't sort {TypeDisplay.Name(type)} values.");
					}
				}
				catch (SelectorParseException ex)
				{
					throw new QueryRequestException($"[{i}].field", ex.Message, ex);
				}
			}

			return source.ApplyOrder(order);
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

			var query = ApplySearch(source, request.Search);
			query = Within("filters", () => ApplyFilters(query, request.Filters));
			return Within("order", () => ApplyOrder(query, request.Order));
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

		private static void ValidateSelectors<T>(IReadOnlyList<FilterNode> nodes, string path, MemberFilter filter)
		{
			for (var i = 0; i < nodes.Count; i++)
			{
				var nodePath = $"{path}[{i}]";
				switch (nodes[i])
				{
					case null:
						throw new QueryRequestException(nodePath, "A filter can't be null.");
					case GroupNode group:
						ValidateSelectors<T>(group.Filters, nodePath + ".filters", filter);
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
						break;
				}
			}
		}
	}
}
