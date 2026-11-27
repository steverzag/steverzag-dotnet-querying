namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record DecimalValueSelector<T> : PropertySelector<T>
	{
		public decimal Value { get; set; }
		public override Type? SelectorType { get; internal init; } = typeof(decimal);

		public DecimalValueSelector(decimal value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record DecimalValueSelector : PropertySelector
	{
		public decimal Value { get; set; }
		public override Type? SelectorType { get; internal init; } = typeof(decimal);

		public DecimalValueSelector(decimal value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}
