using System.Globalization;
using Steverzag.QueryShaper.Exceptions;

namespace Steverzag.QueryShaper.Filtering
{
	internal static class FilterText
	{
		private static readonly string[] LocalDateTimeFormats =
		[
			"yyyy-MM-dd",
			"yyyy-MM-ddTHH:mm",
			"yyyy-MM-ddTHH:mm:ss",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFF",
		];

		private static readonly string[] OffsetFormats =
		[
			"yyyy-MM-ddTHH:mmzzz",
			"yyyy-MM-ddTHH:mm:sszzz",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
		];

		private static readonly string[] UtcFormats =
		[
			"yyyy-MM-ddTHH:mm'Z'",
			"yyyy-MM-ddTHH:mm:ss'Z'",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFF'Z'",
		];

		private static readonly string[] TimeFormats = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];

		public static bool IsConvertible(Type type) =>
			type == typeof(DateTime)
			|| type == typeof(DateOnly)
			|| type == typeof(TimeOnly)
			|| type == typeof(TimeSpan)
			|| type == typeof(DateTimeOffset)
			|| type == typeof(Guid)
			|| type == typeof(char);

		public static object Parse(string text, Type type, string field) =>
			TryParse(text, type, out var value)
				? value
				: throw new FilterConditionAndValueNotSupportedException(
					$"The filter value for '{field}' is not a valid {type.Name}: '{text}'.{Expected(type)}");

		private static bool TryParse(string text, Type type, out object value)
		{
			var invariant = CultureInfo.InvariantCulture;
			bool ok;
			if (type == typeof(DateTime))
			{
				ok = DateTime.TryParseExact(text, LocalDateTimeFormats, invariant, DateTimeStyles.None, out var local);
				if (ok)
				{
					value = local;
					return true;
				}
				ok = TryParseOffset(text, out var withOffset);
				value = withOffset.UtcDateTime;
				return ok;
			}
			if (type == typeof(DateTimeOffset))
			{
				ok = TryParseOffset(text, out var offset);
				value = offset;
				return ok;
			}
			if (type == typeof(DateOnly))
			{
				ok = DateOnly.TryParseExact(text, "yyyy-MM-dd", invariant, DateTimeStyles.None, out var date);
				value = date;
				return ok;
			}
			if (type == typeof(TimeOnly))
			{
				ok = TimeOnly.TryParseExact(text, TimeFormats, invariant, DateTimeStyles.None, out var time);
				value = time;
				return ok;
			}
			if (type == typeof(TimeSpan))
			{
				ok = TimeSpan.TryParseExact(text, "c", invariant, out var span);
				value = span;
				return ok;
			}
			if (type == typeof(Guid))
			{
				ok = Guid.TryParse(text, out var guid);
				value = guid;
				return ok;
			}
			if (type == typeof(char))
			{
				ok = text.Length == 1;
				value = ok ? text[0] : default(char);
				return ok;
			}
			throw new ArgumentException($"No text conversion for type {type.Name}.", nameof(type));
		}

		private static bool TryParseOffset(string text, out DateTimeOffset value)
		{
			var invariant = CultureInfo.InvariantCulture;
			return DateTimeOffset.TryParseExact(text, OffsetFormats, invariant, DateTimeStyles.None, out value)
				|| DateTimeOffset.TryParseExact(text, UtcFormats, invariant, DateTimeStyles.AssumeUniversal, out value);
		}

		private static string Expected(Type type) => type switch
		{
			_ when type == typeof(DateTime) => " Use ISO 8601: 2026-11-27, 2026-11-27T14:30:00, optionally with Z or an offset (+02:00).",
			_ when type == typeof(DateTimeOffset) => " Use ISO 8601 with Z or an offset: 2026-11-27T14:30:00Z, 2026-11-27T14:30:00+02:00.",
			_ when type == typeof(DateOnly) => " Use ISO 8601: 2026-11-27.",
			_ when type == typeof(TimeOnly) => " Use 24-hour ISO 8601: 14:30 or 14:30:00.",
			_ when type == typeof(TimeSpan) => " Use [-][d.]hh:mm:ss[.fffffff]: 01:30:00 or 2.01:30:00.",
			_ when type == typeof(char) => " Use a string of exactly one character.",
			_ => "",
		};
	}
}
