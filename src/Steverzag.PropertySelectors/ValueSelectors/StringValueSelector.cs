namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record StringValueSelector<T> : PropertySelector<T>, IStringLiteralSelector
	{
		public string Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(string);

		public StringValueSelector(string value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record StringValueSelector : PropertySelector, IStringLiteralSelector
	{
		public string Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(string);

		public StringValueSelector(string value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal interface IStringLiteralSelector
	{
		string Value { get; }
	}
}
