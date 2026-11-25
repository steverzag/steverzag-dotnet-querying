namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record DoubleValueSelector<T> : PropertySelector<T>
	{
		public double Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(double);

		public DoubleValueSelector(double value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record DoubleValueSelector : PropertySelector
	{
		public double Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(double);

		public DoubleValueSelector(double value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}
