using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Ordering
{
	public sealed record OrderParameters<T> : OrderParameters
	{
		public new PropertySelector<T> Field { get; init; }

		public OrderParameters(PropertySelector<T> field, OrderDirection direction = OrderDirection.Ascending)
			: base(field?.ToString()!, direction)
		{
			Field = field!;
		}
	}

	[JsonConverter(typeof(OrderParametersJsonConverter))]
	public record OrderParameters
	{
		public string Field { get; init; }

		public OrderDirection Direction { get; init; }

		public OrderParameters(string field, OrderDirection direction = OrderDirection.Ascending)
		{
			ArgumentNullException.ThrowIfNull(field);
			Field = field;
			Direction = direction;
		}
	}
}
