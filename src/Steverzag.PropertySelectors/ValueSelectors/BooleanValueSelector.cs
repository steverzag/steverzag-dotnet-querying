using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal record BooleanValueSelector<T> : PropertySelector<T>
	{
		public bool Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(bool);
		public BooleanValueSelector(bool value) : base(SelectorDefaults.BooleanValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;
			PropertyAccess = (_) =>
			{
				return Implementations.CreateBoolConstantAccesor(value);
			};
		}

		protected override string SegmentToString() => Implementations.FormatBoolLiteral(Value);
	}

	internal record BooleanValueSelector : PropertySelector
	{
		public bool Value { get; set; }
		public override Type? SelectorType { get; init; } = typeof(bool);

		public BooleanValueSelector(bool value) : base(SelectorDefaults.BooleanValueSpecifier + SelectorDefaults.ValuePrefix + value)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateBoolConstantAccesor(value);
			};
		}

		protected override string SegmentToString() => Implementations.FormatBoolLiteral(Value);
	}
}
