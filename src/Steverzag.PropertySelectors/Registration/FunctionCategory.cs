namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>The group a function belongs to, so a UI can list functions by kind.</summary>
	public enum FunctionCategory
	{
		/// <summary>A function that sets no category, such as a custom one.</summary>
		Other,
		/// <summary>Counts, sums, averages and other values computed over a collection.</summary>
		Aggregation,
		/// <summary>One element of a collection: the first, the last, or one at an index.</summary>
		Element,
		/// <summary>Ordering a collection.</summary>
		Ordering,
		/// <summary>Rounding, powers, logarithms, trigonometry, and the larger or smaller of two values.</summary>
		Math,
		/// <summary>Functions on strings.</summary>
		String,
		/// <summary>Date and time literals, arithmetic and the current date.</summary>
		Date,
		/// <summary>Handling null values.</summary>
		Null,
	}
}
