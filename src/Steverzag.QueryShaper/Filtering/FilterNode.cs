using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Filtering
{
	/// <summary>
	/// One node of a filter list: a <see cref="ConditionNode"/>, which tests one field, or a <see cref="GroupNode"/>,
	/// which works as parentheses.
	/// </summary>
	/// <remarks>
	/// Nodes combine strictly left to right, with no precedence: <c>A</c>, <c>Or B</c>, <c>And C</c> means
	/// <c>(A or B) and C</c>. In JSON, a node with <c>filters</c> is a group and any other is a condition. See "Combining
	/// filters" in <c>docs/query-shaper.md</c>.
	/// </remarks>
	[JsonConverter(typeof(FilterNodeJsonConverter))]
	public abstract record FilterNode
	{
		private protected FilterNode() { }

		/// <summary>How this node joins everything before it in its list. The first node's connector is ignored. <see cref="LogicalConnector.And"/> by default.</summary>
		public LogicalConnector Connector { get; init; }
	}

	/// <summary>A filter node that tests one field against a value.</summary>
	/// <example>
	/// <code>new ConditionNode { Field = "Price", Condition = FilterCondition.GreaterThan, Value = 10 }</code>
	/// </example>
	public sealed record ConditionNode : FilterNode
	{
		private readonly PropertySelector _fieldSelector = null!;

		/// <summary>The selector for the field to test, such as <c>Price</c> or <c>Reviews.Stars|$FN_AGGR_AVG</c>.</summary>
		/// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
		[JsonConverter(typeof(PropertySelectorJsonConverter))]
		public required PropertySelector Field
		{
			get => _fieldSelector;
			init => _fieldSelector = value ?? throw new ArgumentNullException(nameof(Field));
		}

		/// <summary>The test to apply.</summary>
		public FilterCondition Condition { get; init; }

		/// <summary>
		/// The value to test against, converted to the field's type when the filter is built. <see langword="null"/> works
		/// only with <see cref="FilterCondition.IsEmpty"/> and <see cref="FilterCondition.IsNotEmpty"/>.
		/// </summary>
		public FilterValue? Value { get; init; }
	}

	/// <summary>
	/// A filter node that groups other nodes, like parentheses. Its children combine by the same rules, and its own
	/// <see cref="FilterNode.Connector"/> joins the whole group to what came before.
	/// </summary>
	/// <example>
	/// <code>
	/// // A or (B and C)
	/// [a, new GroupNode { Connector = LogicalConnector.Or, Filters = [b, c] }]
	/// </code>
	/// </example>
	public sealed record GroupNode : FilterNode
	{
		private readonly IReadOnlyList<FilterNode> _filters = [];

		/// <summary>The nodes in the group. The list is copied when it's set.</summary>
		/// <exception cref="ArgumentNullException">The list is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">The list is empty, or contains a <see langword="null"/> node.</exception>
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
