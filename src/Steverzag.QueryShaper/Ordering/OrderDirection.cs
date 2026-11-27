using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Ordering
{
	[JsonConverter(typeof(NameOnlyEnumConverter<OrderDirection>))]
	public enum OrderDirection
	{
		Ascending,
		Descending
	}
}
