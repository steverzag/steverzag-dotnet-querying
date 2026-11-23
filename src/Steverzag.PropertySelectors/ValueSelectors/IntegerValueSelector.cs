using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record IntegerValueSelector<T> : PropertySelector<T>
	{
		public int Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(int);

		public IntegerValueSelector(int value) : base(SelectorDefaults.IntegerValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString(CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateIntConstantAccesor(value);
			};
		}
	}

	internal record IntegerValueSelector : PropertySelector
	{
		public int Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(int);

		public IntegerValueSelector(int value) : base(SelectorDefaults.IntegerValueSpecifier + SelectorDefaults.ValuePrefix + value.ToString(CultureInfo.InvariantCulture))
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateIntConstantAccesor(value);
			};
		}
	}
}
