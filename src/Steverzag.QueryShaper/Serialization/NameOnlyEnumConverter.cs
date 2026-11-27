using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steverzag.QueryShaper.Serialization
{
	internal sealed class NameOnlyEnumConverter<TEnum> : JsonConverter<TEnum>
		where TEnum : struct, Enum
	{
		private static readonly string[] Names = Enum.GetNames<TEnum>();
		private static readonly string TypeName = typeof(TEnum).Name;
		private static readonly string Article = JsonText.Article(TypeName);

		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.String)
			{
				throw new JsonException(
					$"{char.ToUpperInvariant(Article[0])}{Article[1..]} {TypeName} must be written by name ({string.Join(", ", Names)}), not as {JsonText.Describe(reader.TokenType)}.");
			}

			var text = reader.GetString()!;
			foreach (var name in Names)
			{
				if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
				{
					return Enum.Parse<TEnum>(name);
				}
			}

			throw new JsonException($"'{text}' is not {Article} {TypeName}. Use one of: {string.Join(", ", Names)}.");
		}

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
