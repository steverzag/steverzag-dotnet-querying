namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record IntegerValueSelector<T> : PropertySelector<T>
	{
		public int Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(int);

		public IntegerValueSelector(int value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record IntegerValueSelector : PropertySelector
	{
		public int Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(int);

		public IntegerValueSelector(int value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}
