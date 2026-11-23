namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>
	/// Registry entry for a function token. The two slots are filled independently because the generic
	/// and non-generic selector records are separate types (discovered/registered one at a time).
	/// </summary>
	internal sealed record SelectorRegistration(Type? OpenGenericSelectorType, Type? NonGenericSelectorType);
}
