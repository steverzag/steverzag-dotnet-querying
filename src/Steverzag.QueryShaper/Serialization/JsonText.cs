using System.Text.Json;

namespace Steverzag.QueryShaper.Serialization
{
	internal static class JsonText
	{
		public static string Describe(JsonTokenType token) => token switch
		{
			JsonTokenType.StartObject => "an object",
			JsonTokenType.StartArray => "an array",
			JsonTokenType.String => "a string",
			JsonTokenType.Number => "a number",
			JsonTokenType.True or JsonTokenType.False => "a boolean",
			JsonTokenType.Null => "null",
			_ => token.ToString(),
		};

		public static string Name(JsonSerializerOptions options, string name)
			=> options.PropertyNamingPolicy?.ConvertName(name) ?? name;
	}
}
