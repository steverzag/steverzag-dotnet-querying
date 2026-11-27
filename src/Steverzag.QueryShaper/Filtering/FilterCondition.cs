using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Filtering
{
	[JsonConverter(typeof(NameOnlyEnumConverter<FilterCondition>))]
	public enum FilterCondition
	{
		Equals,
		NotEquals,
		GreaterThan,
		GreaterThanOrEqual,
		LessThan,
		LessThanOrEqual,
		StartsWith,
		DoesNotStartWith,
		EndsWith,
		DoesNotEndWith,
		IsEmpty,
		IsNotEmpty,
		In,
		NotIn,
		Contains,
		DoesNotContain,
		ContainsAny,
		DoesNotContainAny,
		ContainsAll,
		DoesNotContainAll
	}
}
