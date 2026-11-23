
namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	public record ArithmeticOperationSelector<T> : PropertySelector<T>
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => typeof(double);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}
	}

	public record ArithmeticOperationSelector : PropertySelector
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => typeof(double);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}
	}
}
