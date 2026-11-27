using System.Text.Json;
using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Filtering;

namespace Steverzag.QueryShaper.Serialization
{
	internal sealed class FilterValueJsonConverter : JsonConverter<FilterValue>
	{
		public override FilterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is JsonTokenType.StartArray)
			{
				var values = new List<object>();
				while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
				{
					values.Add(reader.TokenType is JsonTokenType.Null ? null! : ReadScalar(ref reader, inList: true));
				}
				return values;
			}

			return ReadScalar(ref reader, inList: false) switch
			{
				long l => l,
				decimal m => m,
				double d => d,
				bool b => b,
				string s => s,
				var other => throw new JsonException($"Unexpected filter value {other}."),
			};
		}

		private static object ReadScalar(ref Utf8JsonReader reader, bool inList)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.String:
					return reader.GetString()!;
				case JsonTokenType.True:
					return true;
				case JsonTokenType.False:
					return false;
				case JsonTokenType.Number:
					if (reader.TryGetInt64(out var integer))
					{
						return integer;
					}
					if (reader.TryGetDecimal(out var number))
					{
						return number;
					}
					return reader.GetDouble();
				default:
					throw new JsonException(inList
						? $"A filter value list can only hold strings, numbers, booleans and null, not {JsonText.Describe(reader.TokenType)}."
						: $"A filter value must be a string, a number, a boolean, null or a list of those, not {JsonText.Describe(reader.TokenType)}.");
			}
		}

		public override void Write(Utf8JsonWriter writer, FilterValue value, JsonSerializerOptions options)
		{
			var raw = value.ToObject();
			if (raw is List<object> list)
			{
				writer.WriteStartArray();
				foreach (var item in list)
				{
					JsonSerializer.Serialize(writer, item, item?.GetType() ?? typeof(object), options);
				}
				writer.WriteEndArray();
				return;
			}

			JsonSerializer.Serialize(writer, raw, raw?.GetType() ?? typeof(object), options);
		}
	}
}
