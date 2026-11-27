namespace Steverzag.PropertySelectors.MetaData
{
	public sealed class SelectorsMetaDataTree(Type selectorType, string name, string? description = default)
	{
		public Type SelectorType { get; } = selectorType;
		public string Name { get; } = name;
		public string? Description { get; init; } = description;
		public string Path { get; init; } = string.Empty;
		public Type? ElementType { get; init; }
		public bool IsCollection => ElementType is not null;
		public string TypeName => TypeDisplay.Name(SelectorType);
		public string? ElementTypeName => ElementType is null ? null : TypeDisplay.Name(ElementType);
		public List<SelectorsMetaDataTree> Selectors { get; } = [];
	}
}
