using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Filtering
{
	/// <summary>
	/// The test a <see cref="ConditionNode"/> applies to its field. In JSON it's written by name, such as
	/// <c>"GreaterThan"</c>, ignoring case; numbers are rejected.
	/// </summary>
	[JsonConverter(typeof(NameOnlyEnumConverter<FilterCondition>))]
	public enum FilterCondition
	{
		/// <summary>The field equals the value.</summary>
		Equals,
		/// <summary>The field doesn't equal the value.</summary>
		NotEquals,
		/// <summary>The field is greater than the value.</summary>
		GreaterThan,
		/// <summary>The field is greater than or equal to the value.</summary>
		GreaterThanOrEqual,
		/// <summary>The field is less than the value.</summary>
		LessThan,
		/// <summary>The field is less than or equal to the value.</summary>
		LessThanOrEqual,
		/// <summary>The string field starts with the text.</summary>
		StartsWith,
		/// <summary>The string field doesn't start with the text.</summary>
		DoesNotStartWith,
		/// <summary>The string field ends with the text.</summary>
		EndsWith,
		/// <summary>The string field doesn't end with the text.</summary>
		DoesNotEndWith,
		/// <summary>
		/// The field has no meaningful value: a null or empty string, a zero number, a null nullable value, or a collection
		/// with no elements. It takes no value.
		/// </summary>
		IsEmpty,
		/// <summary>The opposite of <see cref="IsEmpty"/>. It takes no value.</summary>
		IsNotEmpty,
		/// <summary>The field equals one of the values in a list.</summary>
		In,
		/// <summary>The field equals none of the values in a list. A null field counts as not in the list.</summary>
		NotIn,
		/// <summary>The string field contains the text, or a collection of strings has an element that contains it.</summary>
		Contains,
		/// <summary>The opposite of <see cref="Contains"/>.</summary>
		DoesNotContain,
		/// <summary>The collection field has at least one of the values in a list.</summary>
		ContainsAny,
		/// <summary>The collection field has none of the values in a list.</summary>
		DoesNotContainAny,
		/// <summary>The collection field has every value in a list.</summary>
		ContainsAll,
		/// <summary>The collection field is missing at least one of the values in a list.</summary>
		DoesNotContainAll
	}
}
