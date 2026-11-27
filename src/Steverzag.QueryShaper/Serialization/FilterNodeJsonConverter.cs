using System.Text.Json;
using System.Text.Json.Serialization;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Filtering;

namespace Steverzag.QueryShaper.Serialization
{
	// The whole node is read in one call into a plain shape (System.Text.Json tracks paths inside it), then turned into
	// nodes here. Path names where the node starts; the message names the place inside it (JsonText.Relocate).
	internal sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
	{
		public override FilterNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.StartObject)
			{
				throw new JsonException($"A filter must be an object, not {JsonText.Describe(reader.TokenType)}.");
			}

			NodeShape shape;
			try
			{
				shape = JsonSerializer.Deserialize<NodeShape>(ref reader, options)!;
			}
			catch (JsonException ex)
			{
				throw JsonText.Relocate(ex, "filter");
			}

			return ToNode(shape, "", options);
		}

		private static FilterNode ToNode(NodeShape shape, string path, JsonSerializerOptions options)
		{
			var field = JsonText.Name(options, nameof(NodeShape.Field));
			var filters = JsonText.Name(options, nameof(NodeShape.Filters));
			var condition = JsonText.Name(options, nameof(NodeShape.Condition));

			if (shape.Filters is not null)
			{
				if (shape.Field is not null || shape.Condition is not null || shape.HasValue)
				{
					throw At(path, $"A filter is either a condition ('{field}', '{condition}', value) or a group ('{filters}'), not both.");
				}

				var children = new List<FilterNode>(shape.Filters.Count);
				for (var i = 0; i < shape.Filters.Count; i++)
				{
					var childPath = Join(path, $"{filters}[{i}]");
					var child = shape.Filters[i] ?? throw At(childPath, "A filter can't be null.");
					children.Add(ToNode(child, childPath, options));
				}

				try
				{
					return new GroupNode { Filters = children, Connector = shape.Connector };
				}
				catch (ArgumentException ex)
				{
					throw At(Join(path, filters), ex.Message, ex);
				}
			}

			if (shape.Field is null)
			{
				throw At(path, $"A filter needs '{field}' (a condition) or '{filters}' (a group).");
			}

			if (shape.Condition is null)
			{
				throw At(path, $"A condition needs '{condition}'.");
			}

			return new ConditionNode
			{
				Field = shape.Field,
				Condition = shape.Condition.Value,
				Value = shape.Value,
				Connector = shape.Connector,
			};
		}

		public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
			=> JsonSerializer.Serialize(writer, ToShape(value), options);

		private static NodeShape ToShape(FilterNode node) => node switch
		{
			ConditionNode condition => new NodeShape
			{
				Field = condition.Field,
				Condition = condition.Condition,
				Value = condition.Value,
				Connector = condition.Connector,
			},
			GroupNode group => new NodeShape { Filters = [.. group.Filters.Select(ToShape)], Connector = group.Connector },
			_ => throw new JsonException($"Unknown filter node type {node.GetType().Name}."),
		};

		private static JsonException At(string path, string detail, Exception? inner = null)
			=> JsonText.At(path, detail, "filter", inner);

		private static string Join(string path, string child) => path.Length == 0 ? child : $"{path}.{child}";

		private sealed class NodeShape
		{
			private FilterValue? _value;

			[JsonConverter(typeof(PropertySelectorJsonConverter))]
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public PropertySelector? Field { get; set; }

			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			[JsonConverter(typeof(NameOnlyEnumConverter<FilterCondition>))]
			public FilterCondition? Condition { get; set; }

			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public FilterValue? Value
			{
				get => _value;
				set
				{
					_value = value;
					HasValue = true;
				}
			}

			[JsonIgnore]
			public bool HasValue { get; private set; }

			[JsonConverter(typeof(NameOnlyEnumConverter<LogicalConnector>))]
			public LogicalConnector Connector { get; set; }

			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public List<NodeShape?>? Filters { get; set; }
		}
	}
}
