using System.Globalization;

namespace ExpDataWorker.PropertySelectors.ValueSelectors
{
	internal record TimeOnlyValueSelector<T> : PropertySelector<T>
	{
		public TimeOnly Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(TimeOnly);

		public TimeOnlyValueSelector(TimeOnly value) : base(SelectorDefaults.TimeOnlyValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateTimeOnlyConstantAccesor(value);
			};
		}
	}

	internal record TimeOnlyValueSelector : PropertySelector
	{
		public TimeOnly Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(TimeOnly);

		public TimeOnlyValueSelector(TimeOnly value) : base(SelectorDefaults.TimeOnlyValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateTimeOnlyConstantAccesor(value);
			};
		}
	}
}
