namespace Steverzag.PropertySelectors.MetaData
{
	/// <summary>Formats types with C#-style names, the way the metadata tree and the function catalog show them.</summary>
	public static class TypeDisplay
	{
		private static readonly Dictionary<Type, string> Aliases = new()
		{
			[typeof(bool)] = "bool",
			[typeof(byte)] = "byte",
			[typeof(sbyte)] = "sbyte",
			[typeof(char)] = "char",
			[typeof(short)] = "short",
			[typeof(ushort)] = "ushort",
			[typeof(int)] = "int",
			[typeof(uint)] = "uint",
			[typeof(long)] = "long",
			[typeof(ulong)] = "ulong",
			[typeof(float)] = "float",
			[typeof(double)] = "double",
			[typeof(decimal)] = "decimal",
			[typeof(string)] = "string",
			[typeof(object)] = "object",
		};

		/// <summary>Returns a C#-style name for a type, such as <c>int?</c>, <c>List&lt;Phone&gt;</c> or <c>decimal[]</c>.</summary>
		/// <param name="type">The type.</param>
		/// <returns>The name. Types are written without their namespace.</returns>
		public static string Name(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);

			if (Aliases.TryGetValue(type, out var alias))
				return alias;

			if (Nullable.GetUnderlyingType(type) is { } underlying)
				return Name(underlying) + "?";

			if (type.IsArray)
				return $"{Name(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";

			if (!type.IsGenericType)
				return type.Name;

			var name = type.Name[..type.Name.IndexOf('`')];
			return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
		}
	}
}
