namespace Steverzag.QueryShaper.Exceptions
{
	/// <summary>
	/// A filter value can't be used with its field and condition: it doesn't convert to the field's type, it's out of
	/// range, it's missing where one is needed, or it isn't a defined enum value.
	/// </summary>
	/// <remarks>Through <see cref="IQueryShaper"/>, it is reported as the inner exception of a <see cref="QueryRequestException"/>.</remarks>
	/// <param name="message">What is wrong with the value.</param>
	public sealed class FilterConditionAndValueNotSupportedException(string message) : Exception(message)
	{
	}
}
