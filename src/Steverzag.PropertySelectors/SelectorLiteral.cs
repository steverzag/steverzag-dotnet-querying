using System.Globalization;

namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// Writes .NET values as selector literals, so code can put a value into selector text safely, for example as a
	/// function argument. Each literal parses back to the same value and type.
	/// </summary>
	public static class SelectorLiteral
	{
		/// <summary>Writes a value of any supported type as a literal.</summary>
		/// <param name="value">
		/// A <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="short"/>, <see cref="byte"/>,
		/// <see cref="long"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>, <see cref="DateTime"/>,
		/// <see cref="DateOnly"/> or <see cref="TimeOnly"/>. A <see cref="short"/> or <see cref="byte"/> is written as an
		/// <see cref="int"/>, and a <see cref="float"/> as a <see cref="double"/>.
		/// </param>
		/// <returns>The literal.</returns>
		/// <exception cref="NotSupportedException">The type has no selector literal.</exception>
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

		/// <summary>Writes a quoted string, doubling any quote inside it: <c>say "hi"</c> becomes <c>"say ""hi"""</c>.</summary>
		/// <param name="value">The string.</param>
		/// <returns>The literal.</returns>
		public static string Format(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

		/// <summary>Writes <c>true</c> or <c>false</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(bool value) => value ? "true" : "false";

		/// <summary>Writes an integer, such as <c>42</c> or <c>-3</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

		/// <summary>Writes a long integer with an <c>L</c> suffix, such as <c>5L</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(long value) => value.ToString(CultureInfo.InvariantCulture) + "L";

		/// <summary>Writes a decimal with an <c>m</c> suffix, such as <c>9.99m</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture) + "m";

		/// <summary>Writes a double, always with a <c>.</c> or an exponent so it reads back as a double: <c>2.0</c>, <c>1E+20</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		/// <exception cref="ArgumentOutOfRangeException">The value is NaN or an infinity, which have no literal.</exception>
		public static string Format(double value)
		{
			if (!double.IsFinite(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, "NaN and infinities have no selector literal.");

			var text = value.ToString("R", CultureInfo.InvariantCulture);
			return text.AsSpan().IndexOfAny('.', 'E') < 0 ? text + ".0" : text;
		}

		/// <summary>Writes a <c>$FN_DT_DATETIME</c> literal in the round-trip format, which keeps the value's kind.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(DateTime value)
			=> $"{SelectorDefaults.DateTimeLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";

		/// <summary>Writes a <c>$FN_DT_DATEONLY</c> literal, such as <c>$FN_DT_DATEONLY("2024-01-31")</c>.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(DateOnly value)
			=> $"{SelectorDefaults.DateOnlyLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";

		/// <summary>Writes a <c>$FN_DT_TIMEONLY</c> literal in the round-trip format.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The literal.</returns>
		public static string Format(TimeOnly value)
			=> $"{SelectorDefaults.TimeOnlyLiteral}({Format(value.ToString("O", CultureInfo.InvariantCulture))})";
	}
}
