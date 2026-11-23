using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record DoubleValueSelector<T> : PropertySelector<T>
	{
		public double Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(double);
		public DoubleValueSelector(double value) : base(SelectorDefaults.DoubleValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("R", CultureInfo.InvariantCulture))
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

		public DoubleValueSelector(double value) : base(SelectorDefaults.DoubleValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString("R", CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateDoubleConstantAccesor(value);
			};
		}
	}
}
