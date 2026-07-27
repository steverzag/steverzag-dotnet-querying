using System.Globalization;

namespace ExpDataWorker.PropertySelectors.ValueSelectors
{
	internal record DateTimeValueSelector<T> : PropertySelector<T>
	{
		public DateTime Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(DateTime);

		public DateTimeValueSelector(DateTime value) : base(SelectorDefaults.DateTimeValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDateTimeConstantAccesor(value);
			};
		}
	}

	internal record DateTimeValueSelector : PropertySelector
	{
		public DateTime Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(DateTime);

		public DateTimeValueSelector(DateTime value) : base(SelectorDefaults.DateTimeValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("O", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDateTimeConstantAccesor(value);
			};
		}
	}
}
