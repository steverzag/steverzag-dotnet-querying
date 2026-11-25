namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record StringValueSelector<T> : PropertySelector<T>
	{
		public string Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(string);

		public StringValueSelector(string value) : base(SelectorDefaults.StringValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateStringConstantAccesor(value);
			};
		}

		protected override string SegmentToString() => Implementations.FormatStringLiteral(Value, PlainValue);
	}

	internal record StringValueSelector : PropertySelector
	{
		public string Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(string);

		public StringValueSelector(string value) : base(SelectorDefaults.StringValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateStringConstantAccesor(value);
			};
		}

		protected override string SegmentToString() => Implementations.FormatStringLiteral(Value, PlainValue);
	}
}
