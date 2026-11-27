
namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal record ArithmeticOperationSelector<T> : PropertySelector<T>, IArithmeticOperationSelector
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => ArithmeticUtils.DeclaredType(this);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}

		internal override Type? InitSelectorTypeToApply() => null;

		private protected override string SegmentToString() => ArithmeticOperationFormatter.FormatSegment(this, ChildSelector);
	}

	internal record ArithmeticOperationSelector : PropertySelector, IArithmeticOperationSelector
	{
		public PropertySelector LeftExpression { get; init; } = null!;
		public PropertySelector? RightExpression { get; init; }
		public ArithmeticOperator Operator { get; set; }
		public override Type? SelectorType => ArithmeticUtils.DeclaredType(this);

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}

		private protected override string SegmentToString() => ArithmeticOperationFormatter.FormatSegment(this, ChildSelector);
	}

	internal interface IArithmeticOperationSelector
	{
		PropertySelector LeftExpression { get; }
		PropertySelector? RightExpression { get; }
		ArithmeticOperator Operator { get; }
	}

	internal static class ArithmeticOperationFormatter
	{
		internal static string FormatSegment(IArithmeticOperationSelector operation, PropertySelector? child)
			=> child is null ? Format(operation) : $"({Format(operation)})";

		internal static string Format(IArithmeticOperationSelector operation)
		{
			var precedence = Precedence(operation.Operator);

			if (operation.Operator == ArithmeticOperator.Negation)
				return "-" + FormatOperand(operation.LeftExpression, precedence, wrapOnEqual: true);

			var left = FormatOperand(operation.LeftExpression, precedence, wrapOnEqual: false);
			var right = FormatOperand(operation.RightExpression!, precedence, wrapOnEqual: true);

			return $"{left} {Symbol(operation.Operator)} {right}";
		}

		internal static int OperatorPosition(IArithmeticOperationSelector operation)
			=> operation.Operator == ArithmeticOperator.Negation
				? 0
				: FormatOperand(operation.LeftExpression, Precedence(operation.Operator), wrapOnEqual: false).Length + 1;

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

		internal static char Symbol(ArithmeticOperator op) => op switch
		{
			ArithmeticOperator.Addition => '+',
			ArithmeticOperator.Subtraction => '-',
			ArithmeticOperator.Multiplication => '*',
			ArithmeticOperator.Division => '/',
			_ => throw new NotSupportedException($"Operator {op}")
		};
	}
}
