namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>What a function's arguments are typed against in the generic form.</summary>
	public enum ArgumentScope
	{
		/// <summary>The scope root, which is what <see cref="SelectorExpressions.ResolveArgument"/> resolves against.</summary>
		Root,
		/// <summary>
		/// The element type of the function's target, for a function that builds a lambda over the elements, such as the
		/// ordering functions. If the target isn't a collection, the arguments stay untyped.
		/// </summary>
		TargetElement,
	}
}
