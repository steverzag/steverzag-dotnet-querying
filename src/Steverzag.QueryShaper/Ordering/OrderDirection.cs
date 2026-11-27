using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Ordering
{
	/// <summary>The direction of an order. In JSON it's written by name, ignoring case; numbers are rejected.</summary>
	[JsonConverter(typeof(NameOnlyEnumConverter<OrderDirection>))]
	public enum OrderDirection
	{
		/// <summary>Smallest first. The default.</summary>
		Ascending,
		/// <summary>Largest first.</summary>
		Descending
	}
}
