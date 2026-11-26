using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

public class NonNumericOperandTests
{
    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public DateTime Created { get; set; } = new(2024, 1, 1);
        public DateTime? Deleted { get; set; }
    }

    private static void ParseAndBuild(string text, bool generic)
    {
        PropertySelector selector = generic ? (PropertySelector<Item>)text : (PropertySelector)text;
        ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), selector);
    }

    private static string Needs(char op, string operand, string type) => $"'{op}' needs numeric operands: '{operand}' is {type}.";

    public static TheoryData<string, bool, string, string, int, string> Rejected
    {
        get
        {
            var data = new TheoryData<string, bool, string, string, int, string>();

            foreach (var generic in new[] { true, false })
            {
                data.Add("\"a\" + 1", generic, Needs('+', "\"a\"", "String"), "\"a\" + 1", 4, "+");
                data.Add("1 + \"a\"", generic, Needs('+', "\"a\"", "String"), "1 + \"a\"", 2, "+");
                data.Add("true * 2", generic, Needs('*', "true", "Boolean"), "true * 2", 5, "*");
                data.Add("Age - \"ab\"|$FN_STR_LENGTH * \"x\"", generic, Needs('*', "\"x\"", "String"), "Age - \"ab\"|$FN_STR_LENGTH * \"x\"", 26, "*");
                data.Add("-\"a\"", generic, "'-' needs a numeric operand: '\"a\"' is String.", "-\"a\"", 0, "-");
                data.Add("$FN_MATH_MAX(Age, \"a\" / 2)", generic, Needs('/', "\"a\"", "String"), "$FN_MATH_MAX(Age, \"a\" / 2)", 22, "/");
            }

            data.Add("Name + 1", true, Needs('+', "Name", "String"), "Name + 1", 5, "+");
            data.Add("Created * 2", true, Needs('*', "Created", "DateTime"), "Created * 2", 8, "*");
            data.Add("Deleted + 1", true, Needs('+', "Deleted", "DateTime?"), "Deleted + 1", 8, "+");
            data.Add("(Age + 1) * Name", true, Needs('*', "Name", "String"), "(Age + 1) * Name", 10, "*");
            data.Add("Age + Name * 2", true, Needs('*', "Name", "String"), "Age + Name * 2", 11, "*");
            data.Add("-Name", true, "'-' needs a numeric operand: 'Name' is String.", "-Name", 0, "-");

            data.Add("Name + 1", false, Needs('+', "Name", "String"), "Name + 1", 5, "+");
            data.Add("Created * 2", false, Needs('*', "Created", "DateTime"), "Created * 2", 8, "*");
            data.Add("Deleted + 1", false, Needs('+', "Deleted", "DateTime?"), "Deleted + 1", 8, "+");
            data.Add("(Age + 1) * Name", false, Needs('*', "Name", "String"), "(Age + 1) * Name", 10, "*");
            data.Add("Age + Name * 2", false, Needs('*', "Name", "String"), "Name * 2", 5, "*");
            data.Add("-Name", false, "'-' needs a numeric operand: 'Name' is String.", "-Name", 0, "-");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void ANonNumericOperand_IsASelectorParseException_AtTheOperator(string text, bool generic, string detail, string selector, int position, string token)
    {
        var exception = Assert.Throws<SelectorParseException>(() => ParseAndBuild(text, generic));

        Assert.Equal(detail, exception.Detail);
        Assert.Equal(selector, exception.Selector);
        Assert.Equal(position, exception.Position);
        Assert.Equal(token, exception.Token);
    }

    [Theory]
    [InlineData("\"a\" + 1", true)]
    [InlineData("\"a\" + 1", false)]
    [InlineData("Name + 1", true)]
    [InlineData("-\"a\"", false)]
    public void ALiteralOrTypedOperand_IsRejectedWhenParsed_NotWhenBuilt(string text, bool generic)
    {
        Assert.Throws<SelectorParseException>(() => generic ? (PropertySelector<Item>)text : (PropertySelector)text);
    }
}
