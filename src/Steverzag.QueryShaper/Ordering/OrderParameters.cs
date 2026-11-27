using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Ordering
{
	/// <summary>An order on a selector already parsed against <typeparamref name="T"/>, for orders built in code.</summary>
	/// <typeparam name="T">The type the selector starts from.</typeparam>
	public sealed record OrderParameters<T> : OrderParameters
	{
		/// <summary>The selector to order by.</summary>
		public new PropertySelector<T> Field { get; init; }

		/// <summary>Creates an order on a parsed selector.</summary>
		/// <param name="field">The selector to order by.</param>
		/// <param name="direction">The direction. Ascending by default.</param>
		public OrderParameters(PropertySelector<T> field, OrderDirection direction = OrderDirection.Ascending)
			: base(field?.ToString()!, direction)
		{
			Field = field!;
		}
	}

	/// <summary>
	/// One order of a request: a selector and a direction. Several orders apply in sequence, the first as the primary sort.
	/// </summary>
	[JsonConverter(typeof(OrderParametersJsonConverter))]
	public record OrderParameters
	{
		/// <summary>The selector text to order by, such as <c>CreatedAt</c> or <c>Reviews.Stars|$FN_AGGR_AVG</c>.</summary>
		public string Field { get; init; }

		/// <summary>The direction. Ascending by default.</summary>
		public OrderDirection Direction { get; init; }

		/// <summary>Creates an order.</summary>
		/// <param name="field">The selector text to order by.</param>
		/// <param name="direction">The direction. Ascending by default.</param>
		/// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
		public OrderParameters(string field, OrderDirection direction = OrderDirection.Ascending)
		{
			ArgumentNullException.ThrowIfNull(field);
			Field = field;
			Direction = direction;
		}
	}
}
