using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.ArithmeticSelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

/// <summary>
/// Covers infix arithmetic expressions end to end: the string is tokenized and parsed into an
/// <see cref="ArithmeticOperationSelector{T}"/> tree, the tree is turned into an expression and
/// the expression is executed.
/// </summary>
public class ArithmeticOperationSelectorTests
{
    private class TestNested
    {
        public int Amount { get; set; }
    }

    private class TestItem
    {
        public int Amount { get; set; }
        public double Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestNested Nested { get; set; } = new();
    }

    private static TResult Evaluate<TResult>(PropertySelector<TestItem> selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    // ── Parsing shape ───────────────────────────────────────────────────────

    [Fact]
    public void TopLevelOperator_CreatesArithmeticOperationSelector()
    {
        PropertySelector<TestItem> selector = "Amount + 2";

        var arithmetic = Assert.IsType<ArithmeticOperationSelector<TestItem>>(selector);
        Assert.Equal(ArithmeticOperator.Addition, arithmetic.Operator);
        Assert.Equal("Amount", arithmetic.LeftExpression.PlainValue);
        Assert.NotNull(arithmetic.RightExpression);
    }

    [Theory]
    [InlineData("Amount + 2", ArithmeticOperator.Addition)]
    [InlineData("Amount - 2", ArithmeticOperator.Subtraction)]
    [InlineData("Amount * 2", ArithmeticOperator.Multiplication)]
    [InlineData("Amount / 2", ArithmeticOperator.Division)]
    public void EachOperator_IsParsedIntoTheMatchingOperator(string expression, ArithmeticOperator expected)
    {
        PropertySelector<TestItem> selector = expression;

        var arithmetic = Assert.IsType<ArithmeticOperationSelector<TestItem>>(selector);
        Assert.Equal(expected, arithmetic.Operator);
    }

    [Fact]
    public void LeadingMinus_IsParsedAsNegationWithoutARightOperand()
    {
        PropertySelector<TestItem> selector = "-Amount";

        var arithmetic = Assert.IsType<ArithmeticOperationSelector<TestItem>>(selector);
        Assert.Equal(ArithmeticOperator.Negation, arithmetic.Operator);
        Assert.Null(arithmetic.RightExpression);
    }

    [Fact]
    public void ArithmeticSelector_DeclaresDoubleRegardlessOfTheOperandTypes()
    {
        // Known mismatch: SelectorType is hard-coded to double, while the built expression keeps
        // the widened operand type — int here.
        PropertySelector<TestItem> selector = "Amount + 2";

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(5, Evaluate<int>(selector, new TestItem { Amount = 3 }));
        Assert.ThrowsAny<ArgumentException>(() => Evaluate<double>(selector, new TestItem { Amount = 3 }));
    }

    // ── Evaluation ──────────────────────────────────────────────────────────

    [Fact]
    public void Addition_OnIntegerProperty_AddsTheOperands()
    {
        PropertySelector<TestItem> selector = "Amount + 2";

        Assert.Equal(12, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void Subtraction_OnIntegerProperty_SubtractsTheOperands()
    {
        PropertySelector<TestItem> selector = "Amount - 2";

        Assert.Equal(8, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void Multiplication_OnIntegerProperty_MultipliesTheOperands()
    {
        PropertySelector<TestItem> selector = "Amount * 3";

        Assert.Equal(30, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void Division_OnIntegerOperands_KeepsIntegerSemantics()
    {
        // Both operands widen to int, so the division truncates instead of producing 3.5.
        PropertySelector<TestItem> selector = "Amount / 2";

        Assert.Equal(3, Evaluate<int>(selector, new TestItem { Amount = 7 }));
    }

    [Fact]
    public void Division_ByZero_ThrowsAtEvaluationTime()
    {
        PropertySelector<TestItem> selector = "Amount / 0";

        Assert.Throws<DivideByZeroException>(() => Evaluate<int>(selector, new TestItem { Amount = 7 }));
    }

    [Fact]
    public void Negation_OnIntegerProperty_FlipsTheSign()
    {
        PropertySelector<TestItem> selector = "-Amount";

        Assert.Equal(-10, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void MixedOperandTypes_WidenToTheLargerNumericType()
    {
        PropertySelector<TestItem> selector = "Value * 2";

        Assert.Equal(5d, Evaluate<double>(selector, new TestItem { Value = 2.5 }));
    }

    [Fact]
    public void TwoProperties_AreBothResolvedAgainstTheRootItem()
    {
        PropertySelector<TestItem> selector = "Value + Amount";

        Assert.Equal(12.5d, Evaluate<double>(selector, new TestItem { Value = 2.5, Amount = 10 }));
    }

    [Fact]
    public void NestedPropertyPath_IsResolvedAsASingleOperand()
    {
        // The tokenizer reads a property operand up to the next operator, so dots stay inside it.
        PropertySelector<TestItem> selector = "Nested.Amount + 1";

        Assert.Equal(6, Evaluate<int>(selector, new TestItem { Nested = new TestNested { Amount = 5 } }));
    }

    // ── Precedence and grouping ─────────────────────────────────────────────

    [Fact]
    public void Multiplication_BindsTighterThanAddition()
    {
        PropertySelector<TestItem> selector = "Amount + 2 * 3";

        Assert.Equal(16, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void Parentheses_OverrideTheDefaultPrecedence()
    {
        PropertySelector<TestItem> selector = "(Amount + 2) * 3";

        Assert.Equal(36, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void SameOperators_AreEvaluatedLeftToRight()
    {
        PropertySelector<TestItem> selector = "Amount - 3 - 2";

        Assert.Equal(5, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void NestedParentheses_AreEvaluatedInnermostFirst()
    {
        PropertySelector<TestItem> selector = "((Amount + 2) * 2) - 4";

        Assert.Equal(20, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    // ── Interaction with function selectors ─────────────────────────────────

    [Fact]
    public void FunctionOperand_IsReadAsASingleTokenIncludingItsParentheses()
    {
        // The tokenizer tracks parenthesis depth so a function call is not split on the
        // operators that may appear inside its arguments.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSqrt}(9) + 1";

        Assert.Equal(4d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void FunctionOperandWithInnerArithmetic_IsReadAsASingleToken()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathPower}(2, 3) * 2";

        Assert.Equal(16d, Evaluate<double>(selector, new TestItem()));
    }

    [Theory]
    [InlineData("Name|$FN_STR_SUBSTRING(0, 2)|$FN_STR_LENGTH * 2", 4)]
    [InlineData("Name|$FN_STR_SUBSTRING(1 + 1)|$FN_STR_LENGTH + 1", 4)]
    [InlineData("1 + Name|$FN_STR_SUBSTRING(0, 2)|$FN_STR_LENGTH", 3)]
    [InlineData("(Name|$FN_STR_SUBSTRING(0, 2)|$FN_STR_LENGTH + 1) * 2", 6)]
    [InlineData("Name|$FN_STR_INDEXOF(\"l\") - Amount", -8)]
    public void PropertyChainWithFunctionArguments_IsReadAsASingleToken(string expression, int expected)
    {
        // A chain that starts with a property used to stop at the first '(', which cut the function's arguments off.
        var item = new TestItem { Name = "hello", Amount = 10 };

        Assert.Equal(expected, EvaluateObject((PropertySelector<TestItem>)expression, item));
        Assert.Equal(expected, EvaluateObject((PropertySelector)expression, item));
    }

    // ── Non-generic operator ────────────────────────────────────────────────

    [Fact]
    public void NonGenericOperator_WithLiteralOperands_Evaluates()
    {
        PropertySelector selector = "2 + 3";

        Assert.Equal(5, Evaluate<int>(selector, new TestItem()));
    }

    [Fact]
    public void NonGenericOperator_BuildsNonGenericNodes()
    {
        // It used to parse against PropertySelector<object>, which can't resolve "Amount".
        PropertySelector selector = "Amount + 2";

        var arithmetic = Assert.IsType<ArithmeticOperationSelector>(selector);
        Assert.Equal(ArithmeticOperator.Addition, arithmetic.Operator);
        Assert.Equal("Amount", arithmetic.LeftExpression.PlainValue);
        Assert.IsType<PropertySelector>(arithmetic.LeftExpression);
    }

    [Theory]
    [InlineData("Amount + 2", 12d)]
    [InlineData("Amount - 2", 8d)]
    [InlineData("Amount * 3", 30d)]
    [InlineData("Amount / 4", 2d)]           // integer division, as in the generic path
    [InlineData("-Amount", -10d)]
    [InlineData("Value + Amount", 12.5d)]
    [InlineData("Nested.Amount + 1", 6d)]
    [InlineData("Amount + 2 * 3", 16d)]
    [InlineData("(Amount + 2) * 3", 36d)]
    [InlineData("Amount - 3 - 2", 5d)]
    public void NonGenericOperator_WithPropertyOperands_MatchesTheGenericPath(string expression, double expected)
    {
        var item = new TestItem { Amount = 10, Value = 2.5, Nested = new TestNested { Amount = 5 } };

        Assert.Equal(expected, Convert.ToDouble(EvaluateObject((PropertySelector)expression, item)));
        Assert.Equal(expected, Convert.ToDouble(EvaluateObject((PropertySelector<TestItem>)expression, item)));
    }

    [Fact]
    public void NonGenericOperator_WithFunctionOperand_Evaluates()
    {
        PropertySelector selector = $"{SelectorDefaults.MathSqrt}(9) + Amount";

        Assert.Equal(13d, Evaluate<double>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void NonGenericOperator_UnknownProperty_FailsWhenBuilt()
    {
        // The non-generic selector has no type to check against when it's parsed, so a missing
        // property is reported when the expression is built, by Expression.PropertyOrField.
        PropertySelector selector = "Missing + 2";

        var exception = Assert.Throws<ArgumentException>(() => Evaluate<int>(selector, new TestItem()));
        Assert.Contains("'Missing' is not a member", exception.Message);
    }

    private static object EvaluateObject(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = Expression.Convert(ExpressionUtils.CreateMemberAccess(parameter, selector), typeof(object));
        return Expression.Lambda<Func<TestItem, object>>(body, parameter).Compile()(item);
    }

    // ── Failures ────────────────────────────────────────────────────────────

    [Fact]
    public void NonNumericOperand_ThrowsOnBuild()
    {
        // Operand types are only checked when the expression is built.
        PropertySelector<TestItem> selector = "Name + 1";

        Assert.Throws<ArgumentException>(() => Evaluate<string>(selector, new TestItem()));
    }

    [Fact]
    public void UnbalancedOpeningParenthesis_ThrowsAsASyntaxError()
    {
        // The analyzer ignores everything at a parenthesis depth above zero, so an unclosed '('
        // would swallow the operator and let the whole string fall through to the property path
        // branch. The balance check turns that into a syntax error instead of a later, and much
        // more confusing, "property not found".
        var exception = Assert.Throws<ArgumentException>(() => (PropertySelector<TestItem>)"(Amount + 2");

        Assert.Contains("Unbalanced parentheses", exception.Message);
        Assert.Contains("unclosed", exception.Message);
    }

    [Fact]
    public void StrayClosingParenthesis_ThrowsAsASyntaxError()
    {
        // A stray ')' drives the depth negative, which used to suppress operator detection for
        // the rest of the scan.
        var exception = Assert.Throws<ArgumentException>(() => (PropertySelector<TestItem>)"Amount) + 2");

        Assert.Contains("Unbalanced parentheses", exception.Message);
        Assert.Contains("unexpected ')'", exception.Message);
    }

    [Fact]
    public void UnbalancedParenthesis_IsReportedEvenWithoutAnyOperator()
    {
        // The same swallowing hits the property delimiter, so the check is not arithmetic
        // specific: "Amount(|Amount" used to collapse into a single part.
        var exception = Assert.Throws<ArgumentException>(() => (PropertySelector<TestItem>)"Amount(|Amount");

        Assert.Contains("Unbalanced parentheses", exception.Message);
    }

    [Fact]
    public void BalancedParentheses_AreStillAccepted()
    {
        // Guard against the balance check rejecting well-formed nesting.
        PropertySelector<TestItem> grouped = "(Amount + 2) * 2";
        PropertySelector<TestItem> function = $"{SelectorDefaults.MathPower}({SelectorDefaults.MathSqrt}(9), 2)";

        Assert.Equal(24, Evaluate<int>(grouped, new TestItem { Amount = 10 }));
        Assert.Equal(9d, Evaluate<double>(function, new TestItem()));
    }

    [Fact]
    public void FullyParenthesizedExpression_IsNotDetectedAsArithmetic()
    {
        // Separate, still-open gap: the analyzer only looks for operators at depth zero, so
        // wrapping the WHOLE expression hides every operator and the string falls through to
        // the property path branch. The parentheses balance, so the syntax check cannot catch
        // it; telling a grouping '(' from a function-call '(' is what would be needed, and that
        // is a parser concern rather than a scanning one.
        var exception = Assert.Throws<InvalidOperationException>(() => (PropertySelector<TestItem>)"((Amount + 2) * 2)");

        Assert.Contains("was not found", exception.Message);

        // A single operator left outside the group is enough to make it work again.
        PropertySelector<TestItem> selector = "((Amount + 2) * 2) - 0";
        Assert.Equal(24, Evaluate<int>(selector, new TestItem { Amount = 10 }));
    }

    [Fact]
    public void MissingClosingParenthesis_AfterATopLevelOperator_ThrowsAtParseTime()
    {
        // Here the leading operand keeps the operator visible at depth zero, so the string does
        // reach the arithmetic parser, which rejects the unterminated group.
        var exception = Assert.Throws<Exception>(() => (PropertySelector<TestItem>)"1 * (Amount + 2");

        Assert.Equal("Missing ')'", exception.Message);
    }

    [Fact]
    public void TrailingOperator_ThrowsAtParseTime()
    {
        Assert.Throws<Exception>(() => (PropertySelector<TestItem>)"Amount +");
    }
}
