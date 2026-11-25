namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record BooleanValueSelector<T> : PropertySelector<T>
	{
		public bool Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(bool);

		public BooleanValueSelector(bool value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record BooleanValueSelector : PropertySelector
	{
		public bool Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(bool);

		public BooleanValueSelector(bool value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}
