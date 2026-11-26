using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Filtering
{
	public abstract record FilterNode
	{
		private protected FilterNode() { }

		public LogicalConnector FilterLogicOption { get; init; }
	}

	public sealed record ConditionNode : FilterNode
	{
		private readonly PropertySelector _fieldSelector = null!;

		public required PropertySelector FieldSelector
		{
			get => _fieldSelector;
			init => _fieldSelector = value ?? throw new ArgumentNullException(nameof(FieldSelector));
		}

		public FilterCondition FilterCondition { get; init; }

		public FilterValue? Value { get; init; }
	}

	public sealed record GroupNode : FilterNode
	{
		private readonly IReadOnlyList<FilterNode> _filters = [];

		public required IReadOnlyList<FilterNode> Filters
		{
			get => _filters;
			init
			{
				ArgumentNullException.ThrowIfNull(value, nameof(Filters));
				if (value.Count == 0)
				{
					throw new ArgumentException("A group must contain at least one filter node.", nameof(Filters));
				}
				if (value.Any(node => node is null))
				{
					throw new ArgumentException("A group cannot contain null filter nodes.", nameof(Filters));
				}
				_filters = [.. value];
			}
		}
	}
}
