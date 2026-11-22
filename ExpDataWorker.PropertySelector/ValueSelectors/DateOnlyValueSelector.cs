using System.Globalization;

namespace ExpDataWorker.PropertySelectors.ValueSelectors
{
	internal record DateOnlyValueSelector<T> : PropertySelector<T>
	{
		public DateOnly Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(DateOnly);

		public DateOnlyValueSelector(DateOnly value) : base(SelectorDefaults.DateOnlyValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDateOnlyConstantAccesor(value);
			};
		}
	}

	internal record DateOnlyValueSelector : PropertySelector
	{
		public DateOnly Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(DateOnly);

		public DateOnlyValueSelector(DateOnly value) : base(SelectorDefaults.DateOnlyValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDateOnlyConstantAccesor(value);
			};
		}
	}
}
