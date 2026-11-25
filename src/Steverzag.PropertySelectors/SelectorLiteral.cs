using System.Globalization;

namespace Steverzag.PropertySelectors
{
	public static class SelectorLiteral
	{
		public static string Format(object value)
		{
			ArgumentNullException.ThrowIfNull(value);

			return value switch
			{
				string s => Format(s),
				bool b => Format(b),
				int i => Format(i),
				short s => Format((int)s),
				byte b => Format((int)b),
				long l => Format(l),
				double d => Format(d),
				float f => Format((double)f),
				decimal m => Format(m),
				DateTime dt => Format(dt),
				DateOnly d => Format(d),
				TimeOnly t => Format(t),
				_ => throw new NotSupportedException($"Values of type {value.GetType().Name} have no selector literal.")
			};
		}

		public static string Format(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

		public static string Format(bool value) => value ? "true" : "false";

		public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

		public static string Format(long value) => value.ToString(CultureInfo.InvariantCulture) + "L";

		public static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture) + "m";

		public static string Format(double value)
		{
			if (!double.IsFinite(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, "NaN and infinities have no selector literal.");

			var text = value.ToString("R", CultureInfo.InvariantCulture);
			return text.AsSpan().IndexOfAny('.', 'E') < 0 ? text + ".0" : text;
		}

		public static string Format(DateTime value)
			=> $"{SelectorDefaults.DateTimeLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";

		public static string Format(DateOnly value)
			=> $"{SelectorDefaults.DateOnlyLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";

		public static string Format(TimeOnly value)
			=> $"{SelectorDefaults.TimeOnlyLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";
	}
}
