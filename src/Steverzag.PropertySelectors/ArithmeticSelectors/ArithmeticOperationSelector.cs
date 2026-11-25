
namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	public record ArithmeticOperationSelector<T> : PropertySelector<T>, IArithmeticOperationSelector
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => typeof(double);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}

		protected override string SegmentToString() => ArithmeticOperationFormatter.Format(this);
	}

	public record ArithmeticOperationSelector : PropertySelector, IArithmeticOperationSelector
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => typeof(double);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}

		protected override string SegmentToString() => ArithmeticOperationFormatter.Format(this);
	}

	internal interface IArithmeticOperationSelector
	{
		PropertySelector LeftExpression { get; }
		PropertySelector? RightExpression { get; }
		ArithmeticOperator Operator { get; }
	}

	internal static class ArithmeticOperationFormatter
	{
		internal static string Format(IArithmeticOperationSelector operation)
		{
			var precedence = Precedence(operation.Operator);

			if (operation.Operator == ArithmeticOperator.Negation)
				return "-" + FormatOperand(operation.LeftExpression, precedence, wrapOnEqual: true);

			var left = FormatOperand(operation.LeftExpression, precedence, wrapOnEqual: false);
			var right = FormatOperand(operation.RightExpression!, precedence, wrapOnEqual: true);

			return $"{left} {Symbol(operation.Operator)} {right}";
		}

		private static string FormatOperand(PropertySelector operand, int parentPrecedence, bool wrapOnEqual)
		{
			var text = operand.ToString();

			if (operand is not IArithmeticOperationSelector inner || operand.ChildSelector is not null)
				return text;

			var innerPrecedence = Precedence(inner.Operator);
			var wrap = innerPrecedence < parentPrecedence || (wrapOnEqual && innerPrecedence == parentPrecedence);

			return wrap ? $"({text})" : text;
		}

		private static int Precedence(ArithmeticOperator op) => op switch
		{
			ArithmeticOperator.Addition or ArithmeticOperator.Subtraction => 1,
			ArithmeticOperator.Multiplication or ArithmeticOperator.Division => 2,
			_ => 3
		};

		private static char Symbol(ArithmeticOperator op) => op switch
		{
			ArithmeticOperator.Addition => '+',
			ArithmeticOperator.Subtraction => '-',
			ArithmeticOperator.Multiplication => '*',
			ArithmeticOperator.Division => '/',
			_ => throw new NotSupportedException($"Operator {op}")
		};
	}
}
