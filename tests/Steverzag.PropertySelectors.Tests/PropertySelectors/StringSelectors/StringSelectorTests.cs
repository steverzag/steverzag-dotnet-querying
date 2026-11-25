using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.StringSelectors;

public class StringSelectorTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    // Instance string functions take their subject from the chained target, so the whole
    // selector chain has to be walked (a single PropertyAccess call only covers one part).
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

    private const string Delimiter = SelectorDefaults.PropertyDelimiter;

    // ── Length ──────────────────────────────────────────────────────────────

    [Fact]
    public void StringLength_OnStringProperty_ReturnsLength()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringLength}";

        var result = Evaluate<int>(selector, new TestItem { Name = "hello" });

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(5, result);
    }

    [Fact]
    public void StringLength_OnNonStringProperty_IsRejected()
    {
        Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)$"Id{Delimiter}{SelectorDefaults.StringLength}");

        PropertySelector nonGeneric = $"Id{Delimiter}{SelectorDefaults.StringLength}";
        Assert.Throws<InvalidOperationException>(() => Evaluate<int>(nonGeneric, new TestItem { Id = 1 }));
    }

    [Fact]
    public void StringLength_WithEmptyParentheses_ReturnsLength()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringLength}()";

        var result = Evaluate<int>(selector, new TestItem { Name = "hello" });

        Assert.Equal(5, result);
    }

    [Fact]
    public void StringLength_WithoutTarget_IsRejected()
    {
        // Applied to the root item itself, which is not a string.
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)SelectorDefaults.StringLength);
        Assert.Equal("$FN_STR_LENGTH must be applied to a string, not a TestItem: '$FN_STR_LENGTH'.", exception.Detail);

        PropertySelector nonGeneric = SelectorDefaults.StringLength;
        Assert.Throws<InvalidOperationException>(() => Evaluate<int>(nonGeneric, new TestItem()));
    }

    // ── Casing ──────────────────────────────────────────────────────────────

    [Fact]
    public void StringToUpper_OnStringProperty_ReturnsUpperCase()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringToUpper}";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello" });

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void StringToLower_OnStringProperty_ReturnsLowerCase()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringToLower}";

        var result = Evaluate<string>(selector, new TestItem { Name = "HELLO" });

        Assert.Equal("hello", result);
    }

    [Fact]
    public void StringToUpper_OnNonGenericSelector_ReturnsUpperCase()
    {
        PropertySelector selector = $"Name{Delimiter}{SelectorDefaults.StringToUpper}";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello" });

        Assert.Equal("HELLO", result);
    }

    // ── Trimming ────────────────────────────────────────────────────────────

    [Fact]
    public void StringTrim_OnStringProperty_TrimsBothEnds()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringTrim}";

        var result = Evaluate<string>(selector, new TestItem { Name = "  hello  " });

        Assert.Equal("hello", result);
    }

    [Fact]
    public void StringTrimStart_OnStringProperty_TrimsLeadingWhitespace()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringTrimStart}";

        var result = Evaluate<string>(selector, new TestItem { Name = "  hello  " });

        Assert.Equal("hello  ", result);
    }

    [Fact]
    public void StringTrimEnd_OnStringProperty_TrimsTrailingWhitespace()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringTrimEnd}";

        var result = Evaluate<string>(selector, new TestItem { Name = "  hello  " });

        Assert.Equal("  hello", result);
    }

    // ── Substring ───────────────────────────────────────────────────────────

    [Fact]
    public void StringSubstring_WithStartIndex_ReturnsTail()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringSubstring}(2)";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello" });

        Assert.Equal("llo", result);
    }

    [Fact]
    public void StringSubstring_WithStartIndexAndLength_ReturnsSlice()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringSubstring}(1, 3)";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello" });

        Assert.Equal("ell", result);
    }

    [Fact]
    public void StringSubstring_WithoutParameters_ThrowsSelectorParseException()
    {
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringSubstring}";
        });
    }

    // ── IndexOf ─────────────────────────────────────────────────────────────

    [Fact]
    public void StringIndexOf_WithStringLiteral_ReturnsIndex()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringIndexOf}(\"ll\")";

        var result = Evaluate<int>(selector, new TestItem { Name = "hello" });

        Assert.Equal(2, result);
    }

    // ── Replace ─────────────────────────────────────────────────────────────

    [Fact]
    public void StringReplace_WithStringLiterals_ReplacesOccurrences()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringReplace}(\"l\", \"r\")";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello" });

        Assert.Equal("herro", result);
    }

    // ── Contains / StartsWith / EndsWith ────────────────────────────────────

    [Fact]
    public void StringContains_WithMatchingLiteral_ReturnsTrue()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringContains}(\"ell\")";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello" });

        Assert.Equal(typeof(bool), selector.Type);
        Assert.True(result);
    }

    [Fact]
    public void StringStartsWith_WithNonMatchingLiteral_ReturnsFalse()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringStartsWith}(\"xyz\")";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello" });

        Assert.False(result);
    }

    [Fact]
    public void StringEndsWith_WithMatchingLiteral_ReturnsTrue()
    {
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringEndsWith}(\"llo\")";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello" });

        Assert.True(result);
    }

    [Fact]
    public void StringStartsWith_WithPropertyParameter_ComparesProperties()
    {
        // The argument is a property of the root item, not of the string target.
        PropertySelector<TestItem> selector = $"Name{Delimiter}{SelectorDefaults.StringStartsWith}(Code)";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello", Code = "he" });

        Assert.True(result);
    }

    [Fact]
    public void StringContains_WithFunctionParameter_ResolvesParameterChain()
    {
        PropertySelector<TestItem> selector =
            $"Name{Delimiter}{SelectorDefaults.StringContains}(Code{Delimiter}{SelectorDefaults.StringTrim})";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello", Code = "  ell  " });

        Assert.True(result);
    }

    // ── Concat ──────────────────────────────────────────────────────────────
    // string.Concat has no instance form, so it keeps taking every value as a parameter.

    [Fact]
    public void StringConcat_WithTwoParameters_ConcatenatesValues()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringConcat}(Name, Code)";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello", Code = "!" });

        Assert.Equal("hello!", result);
    }

    [Fact]
    public void StringConcat_WithThreeParameters_ConcatenatesValues()
    {
        // Value literals are exempt from arithmetic detection, so "-" is safe here;
        // commas and parentheses are still structural and cannot appear in literals.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringConcat}(Name, \"-\", Code)";

        var result = Evaluate<string>(selector, new TestItem { Name = "a", Code = "b" });

        Assert.Equal("a-b", result);
    }

    [Fact]
    public void StringConcat_WithFunctionParameter_ConcatenatesValues()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringConcat}(Name{Delimiter}{SelectorDefaults.StringToUpper}, Code)";

        var result = Evaluate<string>(selector, new TestItem { Name = "hello", Code = "!" });

        Assert.Equal("HELLO!", result);
    }

    // ── IsNullOrEmpty / IsNullOrWhiteSpace ──────────────────────────────────
    // Static members of string keep taking every value as a parameter.

    [Fact]
    public void StringIsNullOrEmpty_WithEmptyProperty_ReturnsTrue()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringIsNullOrEmpty}(Name)";

        var result = Evaluate<bool>(selector, new TestItem { Name = string.Empty });

        Assert.Equal(typeof(bool), selector.Type);
        Assert.True(result);
    }

    [Fact]
    public void StringIsNullOrEmpty_WithFilledProperty_ReturnsFalse()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringIsNullOrEmpty}(Name)";

        var result = Evaluate<bool>(selector, new TestItem { Name = "hello" });

        Assert.False(result);
    }

    [Fact]
    public void StringIsNullOrEmpty_WithWhitespaceProperty_ReturnsFalse()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringIsNullOrEmpty}(Name)";

        var result = Evaluate<bool>(selector, new TestItem { Name = "   " });

        Assert.False(result);
    }

    [Fact]
    public void StringIsNullOrWhiteSpace_WithWhitespaceProperty_ReturnsTrue()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringIsNullOrWhiteSpace}(Name)";

        var result = Evaluate<bool>(selector, new TestItem { Name = "   " });

        Assert.True(result);
    }

    [Fact]
    public void StringIsNullOrEmpty_WithFunctionParameter_EvaluatesFunctionResult()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringIsNullOrEmpty}(Name{Delimiter}{SelectorDefaults.StringTrim})";

        var result = Evaluate<bool>(selector, new TestItem { Name = "   " });

        Assert.True(result);
    }

    [Fact]
    public void StringIsNullOrEmpty_WithNonStringParameter_IsRejected()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)$"{SelectorDefaults.StringIsNullOrEmpty}(Id)");
        Assert.Equal("$FN_STR_ISNULLOREMPTY: argument 1 ('Id') must be a string, not an Int32: '$FN_STR_ISNULLOREMPTY(Id)'.", exception.Detail);

        PropertySelector nonGeneric = $"{SelectorDefaults.StringIsNullOrEmpty}(Id)";
        Assert.Throws<InvalidOperationException>(() => Evaluate<bool>(nonGeneric, new TestItem { Id = 1 }));
    }

    // ── Compare ─────────────────────────────────────────────────────────────

    [Fact]
    public void StringCompare_WithEqualValues_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringCompare}(Name, Code)";

        var result = Evaluate<int>(selector, new TestItem { Name = "hello", Code = "hello" });

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(0, result);
    }

    [Fact]
    public void StringCompare_WithGreaterFirstValue_ReturnsPositive()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringCompare}(Name, Code)";

        var result = Evaluate<int>(selector, new TestItem { Name = "b", Code = "a" });

        Assert.True(result > 0);
    }

    // ── Join ────────────────────────────────────────────────────────────────

    [Fact]
    public void StringJoin_WithTwoValues_JoinsWithSeparator()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringJoin}(\"-\", Name, Code)";

        var result = Evaluate<string>(selector, new TestItem { Name = "a", Code = "b" });

        Assert.Equal(typeof(string), selector.Type);
        Assert.Equal("a-b", result);
    }

    [Fact]
    public void StringJoin_WithSingleValue_ReturnsValue()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.StringJoin}(\"-\", Name)";

        var result = Evaluate<string>(selector, new TestItem { Name = "a" });

        Assert.Equal("a", result);
    }

    [Fact]
    public void StringJoin_WithManyValues_JoinsEveryValue()
    {
        // A comma cannot appear inside a literal: it stays the structural parameter separator.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringJoin}(\"-\", Name, Code, \"tail\", Name)";

        var result = Evaluate<string>(selector, new TestItem { Name = "a", Code = "b" });

        Assert.Equal("a-b-tail-a", result);
    }

    [Fact]
    public void StringJoin_WithFunctionValue_JoinsFunctionResult()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringJoin}(\"-\", Name{Delimiter}{SelectorDefaults.StringToUpper}, Code)";

        var result = Evaluate<string>(selector, new TestItem { Name = "a", Code = "b" });

        Assert.Equal("A-b", result);
    }

    [Fact]
    public void StringJoin_WithSeparatorOnly_ThrowsSelectorParseException()
    {
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestItem> selector = $"{SelectorDefaults.StringJoin}(\"-\")";
        });
    }

    [Fact]
    public void StringJoin_WithNonStringValue_IsRejected()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)$"{SelectorDefaults.StringJoin}(\"-\", Name, Id)");
        Assert.Equal("$FN_STR_JOIN: argument 3 ('Id') must be a string, not an Int32: '$FN_STR_JOIN(\"-\", Name, Id)'.", exception.Detail);

        PropertySelector nonGeneric = $"{SelectorDefaults.StringJoin}(\"-\", Name, Id)";
        Assert.Throws<InvalidOperationException>(() => Evaluate<string>(nonGeneric, new TestItem { Id = 1 }));
    }

    [Fact]
    public void StringJoin_ChainedWithToUpper_AppliesFunctionToResult()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringJoin}(\"-\", Name, Code){Delimiter}{SelectorDefaults.StringToUpper}";

        var result = Evaluate<string>(selector, new TestItem { Name = "a", Code = "b" });

        Assert.Equal("A-B", result);
    }

    // ── Chained functions ───────────────────────────────────────────────────

    [Fact]
    public void StringTrim_ChainedWithToUpper_AppliesBothFunctions()
    {
        PropertySelector<TestItem> selector =
            $"Name{Delimiter}{SelectorDefaults.StringTrim}{Delimiter}{SelectorDefaults.StringToUpper}";

        var result = Evaluate<string>(selector, new TestItem { Name = "  hello  " });

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void StringSubstring_ChainedWithLength_AppliesBothFunctions()
    {
        PropertySelector<TestItem> selector =
            $"Name{Delimiter}{SelectorDefaults.StringSubstring}(1, 3){Delimiter}{SelectorDefaults.StringLength}";

        var result = Evaluate<int>(selector, new TestItem { Name = "hello" });

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(3, result);
    }

    [Fact]
    public void StringReplace_ChainedAfterTrim_ResolvesPropertyParameterFromRoot()
    {
        // Deeper chains keep resolving parameters against the root item.
        PropertySelector<TestItem> selector =
            $"Name{Delimiter}{SelectorDefaults.StringTrim}{Delimiter}{SelectorDefaults.StringReplace}(Code, \"x\")";

        var result = Evaluate<string>(selector, new TestItem { Name = "  hello  ", Code = "l" });

        Assert.Equal("hexxo", result);
    }

    // ── String value selector ───────────────────────────────────────────────

    [Fact]
    public void QuotedString_AsSelector_ProducesStringConstant()
    {
        PropertySelector<TestItem> selector = $"\"hello\"";

        var result = Evaluate<string>(selector, new TestItem());

        Assert.Equal(typeof(string), selector.SelectorType);
        Assert.Equal("hello", result);
    }
}
