using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class FieldAccessTests
{
    public struct Point
    {
        public int X;
        public int Y { get; set; }
    }

    public class Inner
    {
        public int Field;
        public int Value { get; set; } = 7;
    }

    public class Item
    {
        public int Field = 3;
        public int Age { get; set; } = 30;
        public Inner Inner { get; set; } = new();
        public List<Inner> Items { get; set; } = [new()];
        public Point? Location { get; set; } = new Point { X = 1, Y = 2 };
        public (int, string) Pair { get; set; } = (1, "a");
        public string Name { get; set; } = "Ann";
    }

    private static Expression Body(PropertySelector selector)
        => SelectorExpressions.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), selector);

    private static SelectorParseException Rejects(string text, bool generic)
        => Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector selector = generic ? (PropertySelector<Item>)text : (PropertySelector)text;
            Body(selector);
        });

    public static TheoryData<string, string, string, bool> Fields
    {
        get
        {
            var data = new TheoryData<string, string, string, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("Field", "Field", "Item", generic);
                data.Add("Inner.Field", "Field", "Inner", generic);
                data.Add("Inner|Field", "Field", "Inner", generic);
                data.Add("Items.Field", "Field", "Inner", generic);
                data.Add("Location.X", "X", "Point", generic);
                data.Add("Pair.Item1", "Item1", "ValueTuple`2", generic);
                data.Add("Inner.Field|$FN_MATH_SQRT", "Field", "Inner", generic);
                data.Add("Age + Inner.Field", "Field", "Inner", generic);
                data.Add("$FN_MATH_MAX(Age, Field)", "Field", "Item", generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void AField_IsNotAMember_InEitherForm(string text, string field, string type, bool generic)
    {
        var exception = Rejects(text, generic);

        Assert.Equal($"Property '{field}' was not found on type '{type}'.", exception.Detail);
    }

    [Theory]
    [InlineData("Inner.Value", 7)]
    [InlineData("Location.Y", 2)]
    [InlineData("Items.Value|$FN_AGGR_SUM", 7)]
    public void PropertiesNextToFields_StillResolve(string text, int expected)
    {
        foreach (var generic in new[] { true, false })
        {
            PropertySelector selector = generic ? (PropertySelector<Item>)text : (PropertySelector)text;
            var parameter = Expression.Parameter(typeof(Item), "x");
            var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
            var value = Expression.Lambda(Expression.Convert(body, typeof(object)), parameter).Compile().DynamicInvoke(new Item());

            Assert.Equal(expected, Convert.ToInt32(value));
        }
    }
}
