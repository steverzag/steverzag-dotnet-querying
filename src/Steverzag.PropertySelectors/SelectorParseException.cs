namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// The selector text is invalid. Every error in a selector is reported with this type, whichever part of the library
	/// finds it, so a caller such as an HTTP endpoint can catch it alone and answer 400.
	/// </summary>
	/// <remarks>
	/// The position refers to <see cref="Selector"/>. That is the whole selector when the error is found while parsing.
	/// In the non-generic form some errors are found later, when the expression is built, and <see cref="Selector"/> is
	/// then the part being built, such as the property path. See "Errors" in <c>docs/property-selectors.md</c>.
	/// </remarks>
	public sealed class SelectorParseException : ArgumentException
	{
		/// <summary>Initializes the exception.</summary>
		/// <param name="detail">What is wrong, without the position.</param>
		/// <param name="selector">The text that <paramref name="position"/> refers to.</param>
		/// <param name="position">The 0-based offset of the token at fault in <paramref name="selector"/>, if known.</param>
		/// <param name="token">The text at fault, if known.</param>
		/// <param name="innerException">The error that caused this one, if any.</param>
		public SelectorParseException(string detail, string selector, int? position = null, string? token = null, Exception? innerException = null)
			: base(detail, innerException)
		{
			Detail = detail;
			Selector = selector;
			Position = position;
			Token = token;
		}

		/// <summary>What is wrong, without the position, for example "Property 'Missing' was not found on type 'Order'."</summary>
		public string Detail { get; }

		/// <summary>The text that <see cref="Position"/> refers to.</summary>
		public string Selector { get; }

		/// <summary>The 0-based offset of the token at fault in <see cref="Selector"/>, or <see langword="null"/> if it isn't known.</summary>
		public int? Position { get; }

		/// <summary>The text at fault, such as a property name, a function token, an operator or an argument, if known.</summary>
		public string? Token { get; }
		internal bool IsLimit { get; init; }

		/// <summary>
		/// <see cref="Detail"/> followed by "Position N in '…'." when the position is known, with the token added when
		/// <see cref="Detail"/> doesn't already name it.
		/// </summary>
		public override string Message
		{
			get
			{
				if (Position is null)
					return Detail;

				var token = Token is not null && !Detail.Contains(Token) ? $" ('{Token}')" : string.Empty;
				return $"{Detail} Position {Position}{token} in '{Selector}'.";
			}
		}

		internal SelectorParseException Within(string outerSelector, int offset)
			=> new(Detail, outerSelector, (Position ?? 0) + offset, Token ?? Selector, InnerException ?? this);

		internal static bool IsSelectorError(Exception exception)
			=> exception is SelectorParseException { IsLimit: false }
				|| (exception is ArgumentException and not SelectorParseException and not ArgumentNullException)
				|| exception is InvalidOperationException or NotSupportedException or FormatException;

		internal static SelectorParseException From(Exception exception, string selector, int offset, string token)
			=> exception is SelectorParseException parseException
				? parseException.Within(selector, offset)
				: new(DetailOf(exception), selector, offset, token, exception);

		private static string DetailOf(Exception exception)
		{
			if (exception is ArgumentException { ParamName: { } name } argumentException)
			{
				var suffix = $" (Parameter '{name}')";
				var message = argumentException.Message;
				return message.EndsWith(suffix, StringComparison.Ordinal) ? message[..^suffix.Length] : message;
			}

			return exception.Message;
		}
	}
}
