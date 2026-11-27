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

		public static string Article(string noun) => "AEIOUaeiou".Contains(noun[0]) ? "an" : "a";

		// A nested Deserialize restarts its path at "$", so a converter rethrows a nested error with the place inside
		// itself in the message and no Path; System.Text.Json then sets Path to where the converter started.
		public static JsonException At(string path, string detail, string what, Exception? inner = null)
			=> new(path.Length == 0 ? detail : $"{detail} (at '{path}' in this {what})", inner);

		public static JsonException Relocate(JsonException nested, string what)
			=> At(Relative(nested.Path), WithoutLocation(nested.Message), what, nested.InnerException ?? nested);

		private static string Relative(string? path) => path switch
		{
			null or "$" => "",
			_ when path.StartsWith("$.") => path[2..],
			_ when path.StartsWith('$') => path[1..],
			_ => path,
		};

		private static string WithoutLocation(string message)
		{
			var index = message.IndexOf(" Path: ", StringComparison.Ordinal);
			return index < 0 ? message : message[..index];
		}
	}
}
