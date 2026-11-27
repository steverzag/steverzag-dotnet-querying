namespace Steverzag.PropertySelectors.MetaData
{
	/// <summary>A node of the tree <see cref="PropertiesExplorer{T}"/> builds: one property path and what it declares.</summary>
	public sealed class SelectorsMetaDataTree
	{
		internal SelectorsMetaDataTree(Type selectorType, string name)
		{
			SelectorType = selectorType;
			Name = name;
		}

		/// <summary>
		/// The type the path declares, which is the type of the expression it builds: <c>Deleted.Year</c> is <c>int?</c> and
		/// <c>Phones.Number</c> is <c>IEnumerable&lt;string&gt;</c>.
		/// </summary>
		public Type SelectorType { get; }

		/// <summary>The property's name, or <c>Root</c> for the root node.</summary>
		public string Name { get; }

		/// <summary>
		/// The selector text for this node, such as <c>Phones.Number</c>, or an empty string for the root. A name that reads
		/// as a literal is escaped, as in <c>Phones.@True</c>.
		/// </summary>
		public string Path { get; init; } = string.Empty;

		/// <summary>The element type when <see cref="SelectorType"/> is a collection, or <see langword="null"/>.</summary>
		public Type? ElementType { get; init; }

		/// <summary>Whether <see cref="SelectorType"/> is a collection, which is where aggregates apply.</summary>
		public bool IsCollection => ElementType is not null;

		/// <summary>A C#-style name for <see cref="SelectorType"/>, such as <c>int?</c> or <c>List&lt;Phone&gt;</c>.</summary>
		public string TypeName => TypeDisplay.Name(SelectorType);

		/// <summary>A C#-style name for <see cref="ElementType"/>, or <see langword="null"/> when it isn't a collection.</summary>
		public string? ElementTypeName => ElementType is null ? null : TypeDisplay.Name(ElementType);

		/// <summary>The properties reachable below this node.</summary>
		public List<SelectorsMetaDataTree> Selectors { get; } = [];
	}
}
