using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	public sealed record QueryRequest
	{
		private readonly IReadOnlyList<FilterNode> _filters = [];
		private readonly IReadOnlyList<OrderParameters> _order = [];

		public string? Search { get; init; }

		public IReadOnlyList<FilterNode> Filters
		{
			get => _filters;
			init => _filters = value ?? [];
		}

		public IReadOnlyList<OrderParameters> Order
		{
			get => _order;
			init => _order = value ?? [];
		}

		public PageRequest? Page { get; init; }
	}

	public sealed record PageRequest(int Index = 1, int? Size = null);
}
