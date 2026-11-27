using Steverzag.QueryShaper.Filtering;

namespace Steverzag.QueryShaper.Exceptions
{
	/// <summary>
	/// A field's type can't be filtered, or can't be filtered with a condition, such as <c>In</c> on an entity.
	/// </summary>
	/// <remarks>Through <see cref="IQueryShaper"/>, it is reported as the inner exception of a <see cref="QueryRequestException"/>.</remarks>
	public sealed class FilterNotSupportedException : Exception
	{
		/// <summary>Initializes the exception for a type that can't be filtered at all.</summary>
		/// <param name="notSupportedType">The field's type.</param>
		public FilterNotSupportedException(Type notSupportedType) : base($"Filtering is not supported for type '{notSupportedType.Name}'.") { }

		/// <summary>Initializes the exception for a condition a type doesn't support.</summary>
		/// <param name="notSupportedCondition">The condition.</param>
		/// <param name="notSupportedType">The field's type.</param>
		public FilterNotSupportedException(FilterCondition notSupportedCondition, Type notSupportedType) : base($"Filter condition '{notSupportedCondition}' is not supported for type '{notSupportedType.Name}'.") { }
	}
}
