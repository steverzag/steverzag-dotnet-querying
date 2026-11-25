namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record LongValueSelector<T> : PropertySelector<T>
	{
		public long Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(long);

		public LongValueSelector(long value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}

	internal record LongValueSelector : PropertySelector
	{
		public long Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(long);

		public LongValueSelector(long value) : base(SelectorLiteral.Format(value))
		{
			Value = value;
			PropertyAccess = (_) => Implementations.CreateConstantAccessor(value);
		}
	}
}
