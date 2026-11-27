using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Filtering
{
	[JsonConverter(typeof(FilterNodeJsonConverter))]
	public abstract record FilterNode
	{
		private protected FilterNode() { }

		public LogicalConnector Connector { get; init; }
	}

	public sealed record ConditionNode : FilterNode
	{
		private readonly PropertySelector _fieldSelector = null!;

		[JsonConverter(typeof(PropertySelectorJsonConverter))]
		public required PropertySelector Field
		{
			get => _fieldSelector;
			init => _fieldSelector = value ?? throw new ArgumentNullException(nameof(Field));
		}

		public FilterCondition Condition { get; init; }

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
