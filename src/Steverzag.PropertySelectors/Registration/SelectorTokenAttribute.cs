namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>
	/// Marks a selector record as the handler for a function token (e.g. <see cref="SelectorDefaults.OrderAscending"/>).
	/// Decorated types are discovered by <see cref="SelectorRegistry.RegisterAssembly"/>: open generic
	/// definitions fill the generic slot of the token entry, non-generic types fill the non-generic slot.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class SelectorTokenAttribute : Attribute
	{
		public string Token { get; }

		public PipedForm Piped { get; set; }

		public string? PipedHint { get; set; }

		public ArgumentScope Arguments { get; set; }

		public FunctionCategory Category { get; set; }

		public SelectorTokenAttribute(string token)
		{
			Token = token;
		}
	}
}
