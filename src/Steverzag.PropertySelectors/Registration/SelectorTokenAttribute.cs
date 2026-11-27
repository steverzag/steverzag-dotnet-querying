namespace Steverzag.PropertySelectors.Registration
{
	/// <summary>
	/// Marks a function selector with the token it implements, such as <c>$FN_ACME_CLAMP</c>, so
	/// <see cref="SelectorRegistry.RegisterAssembly"/> finds it. Put it on both the generic and the non-generic form.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class SelectorTokenAttribute : Attribute
	{
		/// <summary>The function's token.</summary>
		public string Token { get; }

		/// <summary>What a value piped into the function means for it. <see cref="PipedForm.Target"/> by default.</summary>
		public PipedForm Piped { get; set; }

		/// <summary>
		/// An extra sentence for the error when <see cref="Piped"/> is <see cref="PipedForm.Rejected"/> and a value is
		/// piped into the function anyway.
		/// </summary>
		public string? PipedHint { get; set; }

		/// <summary>
		/// What the arguments are typed against in the generic form. It must match where the function resolves them.
		/// <see cref="ArgumentScope.Root"/> by default.
		/// </summary>
		public ArgumentScope Arguments { get; set; }

		/// <summary>The group the function belongs to, for UIs. <see cref="FunctionCategory.Other"/> by default.</summary>
		public FunctionCategory Category { get; set; }

		/// <summary>Marks a function selector with its token.</summary>
		/// <param name="token">The token. It must start with <see cref="SelectorDefaults.FunctionStart"/>.</param>
		public SelectorTokenAttribute(string token)
		{
			Token = token;
		}
	}
}
