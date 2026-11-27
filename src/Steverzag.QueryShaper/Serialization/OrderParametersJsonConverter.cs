using System.Text.Json;
using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper.Serialization
{
	internal sealed class OrderParametersJsonConverter : JsonConverter<OrderParameters>
	{
		public override OrderParameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.StartObject)
			{
				throw new JsonException($"An order must be an object, not {JsonText.Describe(reader.TokenType)}.");
			}

			var shape = JsonSerializer.Deserialize<OrderShape>(ref reader, options)!;
			if (shape.Field is null)
			{
				throw new JsonException($"An order needs '{JsonText.Name(options, nameof(OrderShape.Field))}'.");
			}

			return new OrderParameters(shape.Field, shape.Direction);
		}

		public override void Write(Utf8JsonWriter writer, OrderParameters value, JsonSerializerOptions options)
			=> JsonSerializer.Serialize(writer, new OrderShape { Field = value.Field, Direction = value.Direction }, options);

		private sealed class OrderShape
		{
			public string? Field { get; set; }

			public OrderDirection Direction { get; set; }
		}
	}
}
