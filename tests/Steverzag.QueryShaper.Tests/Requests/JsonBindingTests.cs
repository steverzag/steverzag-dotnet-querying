using System.Text.Json;
using System.Text.Json.Serialization;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary>
	/// <see cref="QueryRequest"/> binds from JSON under ASP.NET's web defaults with no setup (decision 041.1–3).
	/// </summary>
	public class JsonBindingTests
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		private static FilterNode Node(string json) => JsonSerializer.Deserialize<FilterNode>(json, Web)!;

		private static FilterValue? Value(string json)
			=> ((ConditionNode)Node($$"""{ "field": "Name", "condition": "Equals", "value": {{json}} }""")).Value;

		private static object Raw(FilterValue value) => value.GetKind() switch
		{
			FilterValueKind.String => (string)value,
			FilterValueKind.Integer => (long)value,
			FilterValueKind.Bool => (bool)value,
			FilterValueKind.Double => (double)value,
			FilterValueKind.Decimal => (decimal)value,
			var kind => throw new ArgumentException($"Unexpected kind {kind}."),
		};

		[Fact]
		public void AFullRequest_Binds()
		{
			var request = Bind("""
				{
				  "search": "phone",
				  "filters": [
				    { "field": "Price", "condition": "GreaterThan", "value": 10 },
				    { "connector": "Or", "filters": [
				        { "field": "CreatedAt", "condition": "GreaterThanOrEqual", "value": "2026-11-01" },
				        { "field": "Tags", "condition": "ContainsAny", "value": ["new", "sale"] } ] }
				  ],
				  "order": [ { "field": "CreatedAt", "direction": "Descending" } ],
				  "page": { "index": 2, "size": 20 }
				}
				""");

			Assert.Equal("phone", request.Search);
			Assert.Equal(2, request.Filters.Count);

			var price = Assert.IsType<ConditionNode>(request.Filters[0]);
			Assert.Equal("Price", price.Field.ToString());
			Assert.Equal(FilterCondition.GreaterThan, price.Condition);
			Assert.Equal(10L, (long)price.Value!.Value);

			var group = Assert.IsType<GroupNode>(request.Filters[1]);
			Assert.Equal(LogicalConnector.Or, group.Connector);
			Assert.Equal(2, group.Filters.Count);
			Assert.Equal("2026-11-01", (string)((ConditionNode)group.Filters[0]).Value!.Value);
			Assert.Equal(new List<object> { "new", "sale" }, (List<object>)((ConditionNode)group.Filters[1]).Value!.Value);

			Assert.Equal(new OrderParameters("CreatedAt", OrderDirection.Descending), Assert.Single(request.Order));
			Assert.Equal(new PageRequest(2, 20), request.Page);
		}

		[Fact]
		public void AnEmptyObject_IsAnEmptyRequest()
		{
			var request = Bind("{}");

			Assert.Null(request.Search);
			Assert.Empty(request.Filters);
			Assert.Empty(request.Order);
			Assert.Null(request.Page);
		}

		[Fact]
		public void NullLists_BecomeEmpty()
		{
			var request = Bind("""{ "filters": null, "order": null }""");

			Assert.Empty(request.Filters);
			Assert.Empty(request.Order);
		}

		[Fact]
		public void APageWithOnlyAnIndex_LeavesTheSizeToTheServer()
		{
			Assert.Equal(new PageRequest(3, null), Bind("""{ "page": { "index": 3 } }""").Page);
			Assert.Equal(new PageRequest(1, null), Bind("""{ "page": {} }""").Page);
		}

		[Fact]
		public void TheDefaults_AreAndAndAscending()
		{
			var request = Bind("""
				{ "filters": [ { "field": "Name", "condition": "IsEmpty" } ], "order": [ { "field": "Name" } ] }
				""");

			var node = Assert.IsType<ConditionNode>(Assert.Single(request.Filters));
			Assert.Equal(LogicalConnector.And, node.Connector);
			Assert.Null(node.Value);
			Assert.Equal(OrderDirection.Ascending, Assert.Single(request.Order).Direction);
		}

		[Fact]
		public void Names_AreCaseInsensitive_UnderTheWebDefaults()
		{
			var node = Assert.IsType<ConditionNode>(Node("""{ "FIELD": "Name", "Condition": "startswith", "Value": "Ha", "connector": "OR" }"""));

			Assert.Equal(FilterCondition.StartsWith, node.Condition);
			Assert.Equal(LogicalConnector.Or, node.Connector);
		}

		[Theory]
		[InlineData("""{ "field": "Name", "condition": 0 }""", "FilterCondition must be written by name")]
		[InlineData("""{ "field": "Name", "condition": "0" }""", "'0' is not a FilterCondition")]
		[InlineData("""{ "field": "Name", "condition": "Equals, NotEquals" }""", "is not a FilterCondition")]
		[InlineData("""{ "field": "Name", "condition": "Between" }""", "'Between' is not a FilterCondition")]
		[InlineData("""{ "field": "Name", "condition": "Equals", "connector": 1 }""", "LogicalConnector must be written by name")]
		[InlineData("""{ "field": "Name", "condition": "Equals", "connector": "Xor" }""", "'Xor' is not a LogicalConnector")]
		public void TheRequestsOwnEnums_AreReadByNameOnly(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => Node(json));
			Assert.Contains(message, ex.Message);
		}

		[Theory]
		[InlineData("""{ "field": "Name", "direction": 1 }""", "An OrderDirection must be written by name (Ascending, Descending), not as a number. (at 'direction' in this order)")]
		[InlineData("""{ "field": "Name", "direction": "Down" }""", "'Down' is not an OrderDirection. Use one of: Ascending, Descending. (at 'direction' in this order)")]
		[InlineData("""{ "field": 5 }""", "A selector must be a string, not a number. (at 'field' in this order)")]
		[InlineData("""{ "field": ["Name"] }""", "A selector must be a string, not an array. (at 'field' in this order)")]
		[InlineData("""{ "direction": "Ascending" }""", "An order needs 'field'.")]
		[InlineData("""{ "field": null }""", "An order needs 'field'.")]
		[InlineData("\"Name\"", "An order must be an object, not a string.")]
		public void AnInvalidOrder_IsRejected(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OrderParameters>(json, Web));
			Assert.Equal(message, ex.Message);
		}

		[Theory]
		[InlineData("""{ "order": [ { "field": "Name" }, { "field": "Name", "direction": "Down" } ] }""", "(at 'direction' in this order)")]
		[InlineData("""{ "order": [ { "field": "Name" }, { "field": "Name", "direction": 1 } ] }""", "(at 'direction' in this order)")]
		[InlineData("""{ "order": [ { "field": "Name" }, { "field": 5 } ] }""", "(at 'field' in this order)")]
		[InlineData("""{ "order": [ { "field": "Name" }, { "direction": "Ascending" } ] }""", "An order needs 'field'.")]
		public void AnOrderError_IsPlacedByThePathToItsOrder_AndTheMessage(string json, string messageEnd)
		{
			var ex = Assert.Throws<JsonException>(() => Bind(json));

			Assert.Equal("$.order[1]", ex.Path);
			Assert.EndsWith(messageEnd, ex.Message);
			Assert.DoesNotContain("System.", ex.Message);
		}

		[Fact]
		public void EnumMessages_UseTheRightArticle()
		{
			Assert.StartsWith("A FilterCondition must be written by name", Assert.Throws<JsonException>(() => Node("""{ "field": "Name", "condition": 0 }""")).Message);
			Assert.StartsWith("A LogicalConnector must be written by name", Assert.Throws<JsonException>(() => Node("""{ "field": "Name", "condition": "Equals", "connector": 1 }""")).Message);
			Assert.StartsWith("'Xor' is not a LogicalConnector.", Assert.Throws<JsonException>(() => Node("""{ "field": "Name", "condition": "Equals", "connector": "Xor" }""")).Message);
		}

		// Apps often add JsonStringEnumConverter to their JSON options, and a converter in the options outranks one on a
		// type. The request's enums must still be read by name only, with QueryShaper's messages.
		private static readonly JsonSerializerOptions WebWithEnumConverter = new(JsonSerializerDefaults.Web)
		{
			Converters = { new JsonStringEnumConverter() },
		};

		private static readonly JsonSerializerOptions WebWithCamelCaseEnumConverter = new(JsonSerializerDefaults.Web)
		{
			Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		};

		public static TheoryData<string, string> EnumErrors => new()
		{
			{ """{ "filters": [ { "field": "Name", "condition": 0 } ] }""", "FilterCondition must be written by name" },
			{ """{ "filters": [ { "field": "Name", "condition": "Bigger" } ] }""", "'Bigger' is not a FilterCondition. Use one of: Equals," },
			{ """{ "filters": [ { "field": "Name", "condition": "Equals", "connector": 1 } ] }""", "LogicalConnector must be written by name" },
			{ """{ "filters": [ { "field": "Name", "condition": "Equals", "connector": "Xor" } ] }""", "'Xor' is not a LogicalConnector" },
			{ """{ "order": [ { "field": "Name", "direction": 1 } ] }""", "OrderDirection must be written by name" },
			{ """{ "order": [ { "field": "Name", "direction": "Down" } ] }""", "'Down' is not an OrderDirection" },
		};

		[Theory]
		[MemberData(nameof(EnumErrors))]
		public void TheRequestsOwnEnums_AreReadByNameOnly_UnderAGlobalEnumConverter(string json, string message)
		{
			foreach (var options in new[] { WebWithEnumConverter, WebWithCamelCaseEnumConverter })
			{
				var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<QueryRequest>(json, options));
				Assert.Contains(message, ex.Message);
				Assert.DoesNotContain("System.", ex.Message);
			}
		}

		[Fact]
		public void TheRequestsOwnEnums_KeepTheirNames_UnderAGlobalEnumConverter()
		{
			var json = """
				{ "filters": [ { "field": "Name", "condition": "startswith", "value": "Ha" },
				               { "field": "Name", "condition": "IsNotEmpty", "connector": "OR" } ],
				  "order": [ { "field": "Name", "direction": "descending" } ] }
				""";

			foreach (var options in new[] { WebWithEnumConverter, WebWithCamelCaseEnumConverter })
			{
				var request = JsonSerializer.Deserialize<QueryRequest>(json, options)!;
				Assert.Equal(FilterCondition.StartsWith, Assert.IsType<ConditionNode>(request.Filters[0]).Condition);
				Assert.Equal(LogicalConnector.Or, request.Filters[1].Connector);
				Assert.Equal(OrderDirection.Descending, request.Order[0].Direction);

				var written = JsonSerializer.Serialize(request, options);
				Assert.Contains("\"condition\":\"StartsWith\"", written);
				Assert.Contains("\"connector\":\"Or\"", written);
				Assert.Contains("\"direction\":\"Descending\"", written);
			}
		}

		[Theory]
		[InlineData("""{ "field": "Name", "condition": "Equals", "filters": [ { "field": "Name", "condition": "IsEmpty" } ] }""", "not both")]
		[InlineData("""{ "condition": "Equals", "filters": [ { "field": "Name", "condition": "IsEmpty" } ] }""", "not both")]
		[InlineData("""{ "value": 1, "filters": [ { "field": "Name", "condition": "IsEmpty" } ] }""", "not both")]
		[InlineData("""{ "value": null, "filters": [ { "field": "Name", "condition": "IsEmpty" } ] }""", "not both")]
		[InlineData("""{ "condition": "Equals", "value": 1 }""", "A filter needs 'field' (a condition) or 'filters' (a group)")]
		[InlineData("{}", "A filter needs 'field'")]
		[InlineData("""{ "field": "Name" }""", "A condition needs 'condition'")]
		[InlineData("""{ "filters": [] }""", "at least one filter node")]
		[InlineData("""{ "filters": [ null ] }""", "A filter can't be null. (at 'filters[0]' in this filter)")]
		[InlineData("[]", "A filter must be an object, not an array")]
		[InlineData("\"Name\"", "A filter must be an object, not a string")]
		public void AnInvalidNode_IsRejected(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => Node(json));
			Assert.Contains(message, ex.Message);
		}

		[Fact]
		public void AnInvalidSelector_IsAJsonException_WithTheSelectorsMessage()
		{
			var ex = Assert.Throws<JsonException>(() => Node("""{ "field": "Age + ", "condition": "Equals", "value": 1 }"""));

			var inner = Assert.IsType<SelectorParseException>(ex.InnerException);
			Assert.Contains(inner.Message, ex.Message);
		}

		[Theory]
		[InlineData("""{ "field": 5, "condition": "Equals" }""", "A selector must be a string, not a number")]
		[InlineData("""{ "field": ["Name"], "condition": "Equals" }""", "A selector must be a string, not an array")]
		public void ANonStringSelector_IsRejected(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => Node(json));
			Assert.Contains(message, ex.Message);
		}

		[Fact]
		public void AnErrorDeepInTheTree_IsPlacedByThePathToItsTopLevelFilter_AndTheMessage()
		{
			var ex = Assert.Throws<JsonException>(() => Bind("""
				{ "filters": [ { "field": "Name", "condition": "IsEmpty" },
				               { "filters": [ { "field": "Name", "condition": "IsEmpty" },
				                              { "filters": [ { "field": "Name", "condition": "Nope" } ] } ] } ] }
				"""));

			Assert.Equal("$.filters[1]", ex.Path);
			Assert.StartsWith("'Nope' is not a FilterCondition.", ex.Message);
			Assert.EndsWith("(at 'filters[1].filters[0].condition' in this filter)", ex.Message);
		}

		[Theory]
		[InlineData("""{ "filters": [ { "field": "Name" } ] }""", "A condition needs 'condition'. (at 'filters[0]' in this filter)")]
		[InlineData("""{ "filters": [ { "filters": [] } ] }""", "(at 'filters[0].filters' in this filter)")]
		[InlineData("""{ "filters": [ { "field": 5, "condition": "IsEmpty" } ] }""", "A selector must be a string, not a number. (at 'filters[0].field' in this filter)")]
		[InlineData("""{ "filters": [ { "field": "Name", "condition": "Equals", "value": {} } ] }""", "not an object. (at 'filters[0].value' in this filter)")]
		[InlineData("""{ "filters": [ { "field": "Name", "condition": "IsEmpty", "connector": true } ] }""", "LogicalConnector must be written by name")]
		public void AnErrorInANestedGroup_NamesItsPlace(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => Node(json));
			Assert.Contains(message, ex.Message);
		}

		[Fact]
		public void ASystemTextJsonError_InsideANode_KeepsItsDetail_WithoutTheNestedPath()
		{
			var ex = Assert.Throws<JsonException>(() => Node("""{ "filters": 5 }"""));

			Assert.DoesNotContain("Path: $", ex.Message);
			Assert.EndsWith("(at 'filters' in this filter)", ex.Message);
		}

		[Theory]
		[InlineData("\"text\"", FilterValueKind.String, "text")]
		[InlineData("\"2026-11-01\"", FilterValueKind.String, "2026-11-01")]
		[InlineData("42", FilterValueKind.Integer, 42L)]
		[InlineData("-7", FilterValueKind.Integer, -7L)]
		[InlineData("9223372036854775807", FilterValueKind.Integer, long.MaxValue)]
		[InlineData("true", FilterValueKind.Bool, true)]
		[InlineData("false", FilterValueKind.Bool, false)]
		[InlineData("1e300", FilterValueKind.Double, 1e300)]
		public void AScalarValue_TakesItsKindFromTheJsonToken(string json, FilterValueKind kind, object expected)
		{
			var value = Value(json)!.Value;

			Assert.Equal(kind, value.GetKind());
			Assert.Equal(expected, Raw(value));
		}

		[Theory]
		[InlineData("9.99", "9.99")]
		[InlineData("10.0", "10.0")]
		[InlineData("9223372036854775808", "9223372036854775808")]
		[InlineData("1e3", "1000")]
		public void ANumberThatIsntALong_IsADecimal(string json, string expected)
		{
			var value = Value(json)!.Value;

			Assert.Equal(FilterValueKind.Decimal, value.GetKind());
			Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), (decimal)value);
		}

		[Fact]
		public void Null_IsNoValue()
		{
			Assert.Null(Value("null"));
		}

		[Fact]
		public void AnArray_IsAListOfScalars()
		{
			var value = Value("""[1, 2.5, "x", true, null]""")!.Value;

			Assert.Equal(FilterValueKind.Enumerable, value.GetKind());
			Assert.Equal(new List<object> { 1L, 2.5m, "x", true, null! }, (List<object>)value);
		}

		[Fact]
		public void AnEmptyArray_IsAnEmptyList()
		{
			Assert.Empty((List<object>)Value("[]")!.Value);
		}

		[Theory]
		[InlineData("""{ "a": 1 }""", "A filter value must be a string, a number, a boolean, null or a list of those, not an object")]
		[InlineData("[[1]]", "A filter value list can only hold strings, numbers, booleans and null, not an array")]
		[InlineData("""[{ "a": 1 }]""", "A filter value list can only hold strings, numbers, booleans and null, not an object")]
		public void AnObjectOrANestedArray_IsRejected(string json, string message)
		{
			var ex = Assert.Throws<JsonException>(() => Value(json));
			Assert.Contains(message, ex.Message);
		}

		[Fact]
		public void ARequest_RoundTrips()
		{
			const string json = """
				{"search":"phone","filters":[{"field":"Price","condition":"GreaterThan","value":9.99,"connector":"And"},{"connector":"Or","filters":[{"field":"Tags","condition":"ContainsAny","value":["new",1,true,null],"connector":"And"},{"field":"Name|$FN_STR_TOLOWER","condition":"IsEmpty","connector":"Or"}]}],"order":[{"field":"CreatedAt","direction":"Descending"}],"page":{"index":2,"size":20}}
				""";

			var request = Bind(json);
			Assert.Equal(json, JsonSerializer.Serialize(request, Web));
		}

		[Fact]
		public void ValuesBuiltInCode_AreWrittenAsJson_AndReadBackAsText()
		{
			ConditionNode node = new()
			{
				Field = "CreatedAt",
				Condition = FilterCondition.Equals,
				Value = new DateTime(2024, 3, 15, 10, 0, 0),
			};

			var json = JsonSerializer.Serialize<FilterNode>(node, Web);

			Assert.Contains("\"value\":\"2024-03-15T10:00:00\"", json);
			Assert.Equal("2024-03-15T10:00:00", (string)((ConditionNode)Node(json)).Value!.Value);
		}
	}
}
