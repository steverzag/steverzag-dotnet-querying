using System.Text.Json;
using System.Text.Json.Serialization;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Serialization
{
	internal sealed class PropertySelectorJsonConverter : JsonConverter<PropertySelector>
	{
		public override PropertySelector? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.String)
			{
				throw new JsonException($"A selector must be a string, not {JsonText.Describe(reader.TokenType)}.");
			}

			var text = reader.GetString()!;
			try
			{
				return (PropertySelector)text;
			}
			catch (SelectorParseException ex)
			{
				throw new JsonException(ex.Message, ex);
			}
		}

		public override void Write(Utf8JsonWriter writer, PropertySelector value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
