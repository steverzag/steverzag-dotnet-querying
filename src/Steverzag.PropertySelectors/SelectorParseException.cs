namespace Steverzag.PropertySelectors
{
	public class SelectorParseException : ArgumentException
	{
		public SelectorParseException(string detail, string selector, int? position = null, string? token = null, Exception? innerException = null)
			: base(detail, innerException)
		{
			Detail = detail;
			Selector = selector;
			Position = position;
			Token = token;
		}

		public string Detail { get; }
		public string Selector { get; }
		public int? Position { get; }
		public string? Token { get; }

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
			=> exception is SelectorParseException
				|| (exception is ArgumentException and not ArgumentNullException)
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
