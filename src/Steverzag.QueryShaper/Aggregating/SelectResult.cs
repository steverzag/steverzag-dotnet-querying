namespace Steverzag.QueryShaper.Aggregating
{
	public sealed class SelectResult(List<SelectValue> Values)
	{
		public List<SelectValue> Values { get; } = Values;
	}

	public sealed class SelectValue(string Selector, object? Value)
	{
		public string Selector { get; } = Selector;
		public object? Value { get; } = Value;
	}
}
