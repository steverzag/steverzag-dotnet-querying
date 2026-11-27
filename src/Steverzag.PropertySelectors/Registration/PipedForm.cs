namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>What a value piped into a function with <c>|</c> means for it.</summary>
	public enum PipedForm
	{
		/// <summary>The piped value is the target the function works on, as for <c>STR_TRIM</c> or the aggregates.</summary>
		Target,
		/// <summary>
		/// The piped value is argument 0, so <c>Price|$FN_MATH_ROUND(2)</c> means <c>$FN_MATH_ROUND(Price, 2)</c>. Resolve
		/// every argument with <see cref="SelectorExpressions.ResolveArgument"/>.
		/// </summary>
		FirstArgument,
		/// <summary>The function has no subject, such as <c>DT_NOW</c>, and piping a value into it is an error.</summary>
		Rejected,
	}
}
