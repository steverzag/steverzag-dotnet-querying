namespace Steverzag.PropertySelectors.Extensions
{
	/// <summary>Extension methods for <see cref="PropertySelector"/>.</summary>
	public static class PropertySelectorExtensions
	{
		/// <summary>
		/// Converts a non-generic selector to the generic form by parsing its text again against <typeparamref name="T"/>,
		/// which resolves its property types.
		/// </summary>
		/// <typeparam name="T">The type the selector starts from.</typeparam>
		/// <param name="selector">The selector to convert.</param>
		/// <returns>The selector in the generic form.</returns>
		/// <exception cref="SelectorParseException">The selector isn't valid for <typeparamref name="T"/>, for example because a property doesn't exist on it.</exception>
		public static PropertySelector<T> ToGeneric<T>(this PropertySelector selector)
		{
			return (PropertySelector<T>)selector.ToString();
		}
	}
}
