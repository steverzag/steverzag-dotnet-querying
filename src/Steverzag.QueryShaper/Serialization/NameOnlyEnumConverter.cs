using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steverzag.QueryShaper.Serialization
{
	internal sealed class NameOnlyEnumConverter<TEnum> : JsonConverter<TEnum>
		where TEnum : struct, Enum
	{
		private static readonly string[] Names = Enum.GetNames<TEnum>();

		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.String)
			{
				throw new JsonException(
					$"A {typeof(TEnum).Name} must be written by name ({string.Join(", ", Names)}), not as {JsonText.Describe(reader.TokenType)}.");
			}

			var text = reader.GetString()!;
			foreach (var name in Names)
			{
				if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
				{
					return Enum.Parse<TEnum>(name);
				}
			}

			throw new JsonException($"'{text}' is not a {typeof(TEnum).Name}. Use one of: {string.Join(", ", Names)}.");
		}

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
