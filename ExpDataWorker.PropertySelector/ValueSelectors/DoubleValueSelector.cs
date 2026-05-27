using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ValueSelectors
{
	internal record DoubleValueSelector<T> : PropertySelector<T>
	{
		public double Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(double);
		public DoubleValueSelector(double value) : base(SelectorDefaults.DoubleValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;
			PropertyAccess = (_) =>
			{
				return Implementations.CreateDoubleConstantAccesor(value);
			};
		}
	}

	internal record DoubleValueSelector : PropertySelector
	{
		public double Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(double);

		public DoubleValueSelector(double value) : base(SelectorDefaults.DoubleValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDoubleConstantAccesor(value);
			};
		}
	}
}
