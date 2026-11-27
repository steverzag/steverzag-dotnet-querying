using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	/// <summary>
	/// A client's data request: search, filters, order and page. It binds from JSON with no setup, and
	/// <see cref="IQueryShaper"/> applies it.
	/// </summary>
	/// <remarks>
	/// Every part is optional, so <c>{}</c> is a valid request. Selectors name the source type's properties. See
	/// "Client requests" in <c>docs/query-shaper.md</c> for the JSON shape.
	/// </remarks>
	public sealed record QueryRequest
	{
		private readonly IReadOnlyList<FilterNode> _filters = [];
		private readonly IReadOnlyList<OrderParameters> _order = [];

		/// <summary>
		/// The text to search for, using the search configured for the type with <c>ConfigureSearch&lt;T&gt;</c>, or
		/// <see langword="null"/> for no search. A client can't choose which properties are searched.
		/// </summary>
		public string? Search { get; init; }

		/// <summary>The filters, combined left to right. Empty by default; <see langword="null"/> is read as empty.</summary>
		public IReadOnlyList<FilterNode> Filters
		{
			get => _filters;
			init => _filters = value ?? [];
		}

		/// <summary>The orders, the first as the primary sort. Empty by default; <see langword="null"/> is read as empty.</summary>
		public IReadOnlyList<OrderParameters> Order
		{
			get => _order;
			init => _order = value ?? [];
		}

		/// <summary>The page, or <see langword="null"/> for page 1 at the default size.</summary>
		public PageRequest? Page { get; init; }
	}

	/// <summary>The page a request asks for.</summary>
	/// <param name="Index">The 1-based page number. 1 by default.</param>
	/// <param name="Size">The number of rows per page, or <see langword="null"/> for the configured default.</param>
	public sealed record PageRequest(int Index = 1, int? Size = null);
}
