namespace Steverzag.QueryShaper.Aggregating
{
	/// <summary>One row returned by <c>ApplySelect</c> with selectors: one value per selector, in the order they were given.</summary>
	/// <param name="Values">The values.</param>
	public sealed class SelectResult(List<SelectValue> Values)
	{
		/// <summary>The values, one per selector, in the order they were given.</summary>
		public List<SelectValue> Values { get; } = Values;
	}

	/// <summary>The value one selector produced for a row of a <see cref="SelectResult"/>.</summary>
	/// <param name="Selector">The selector text, exactly as it was passed in.</param>
	/// <param name="Value">The value.</param>
	public sealed class SelectValue(string Selector, object? Value)
	{
		/// <summary>The selector text, exactly as it was passed in.</summary>
		public string Selector { get; } = Selector;

		/// <summary>The value the selector produced for this row.</summary>
		public object? Value { get; } = Value;
	}
}
