using System.Reflection;
using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors.Tests;

public class MemberFilterTests
{
    public class Line
    {
        public int Weight { get; set; }
        public DateTime When { get; set; }
        public string Secret { get; set; } = "";
    }

    public class Item
    {
        public int Age { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public DateOnly Day { get; set; }
        public TimeSpan Duration { get; set; }
        public string Secret { get; set; } = "";
        public Line Main { get; set; } = new();
        public List<Line> Lines { get; set; } = [];
    }

    private static readonly MemberFilter NoSecrets = MemberFilter.All.Except((_, p) => p.Name == "Secret");

    [Theory]
    [InlineData("Created.Ticks")]
    [InlineData("Deleted.Ticks")]
    [InlineData("Main.When.Ticks")]
    [InlineData("Lines.When.Ticks")]
    [InlineData("Lines|$FN_ELM_FIRST|When.Ticks")]
    [InlineData("Created|Ticks")]
    [InlineData("$FN_MATH_MAX(Created.Ticks, 1L)")]
    [InlineData("Created.Ticks + 1")]
    [InlineData("(Age + 1) * Created.Ticks")]
    [InlineData("Lines|$FN_ORDER_ASC(When.Ticks)")]
    [InlineData("Created.TimeOfDay")]
    [InlineData("Created.TimeOfDay.Hours")]
    [InlineData("Duration.Hours")]
    public void Portable_RejectsAHiddenMember_WhereverItIsWritten(string selector)
    {
        var ex = Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Item>(selector));

        var member = selector.Contains("Ticks") ? "Ticks" : selector.Contains("TimeOfDay") ? "TimeOfDay" : "Hours";
        Assert.Equal(selector, ex.Selector);
        Assert.Equal(selector.IndexOf(member, StringComparison.Ordinal), ex.Position);
        Assert.Equal(member, ex.Token);
    }

    [Theory]
    [InlineData("Created.Ticks", "Ticks", "DateTime", "Property 'Ticks' on 'DateTime' is not allowed.")]
    [InlineData("Deleted.Ticks", "Ticks", "DateTime", "Property 'Ticks' on 'DateTime' is not allowed.")]
    [InlineData("Duration.Hours", "Hours", "TimeSpan", "Property 'Hours' on 'TimeSpan' is not allowed.")]
    public void Portable_ReportsAHiddenMemberAsNotFound_AndKeepsTheReasonInside(string selector, string member, string type, string reason)
    {
        var ex = Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Item>(selector));

        Assert.Equal($"Property '{member}' was not found on type '{type}'.", ex.Detail);
        Assert.Equal(reason, Assert.IsType<SelectorParseException>(ex.InnerException).Detail);
    }

    [Theory]
    [InlineData("Created.Year")]
    [InlineData("Created.Month")]
    [InlineData("Created.Day")]
    [InlineData("Created.Hour")]
    [InlineData("Created.Minute")]
    [InlineData("Created.DayOfYear")]
    [InlineData("Deleted.Year")]
    [InlineData("Day.Year")]
    [InlineData("Day.DayOfYear")]
    [InlineData("Lines.When.Month")]
    [InlineData("Created")]
    [InlineData("Duration")]
    [InlineData("Secret")]
    [InlineData("Main.Secret")]
    [InlineData("Lines|$FN_ORDER_ASC(When.Day)")]
    [InlineData("Created.Year + Age")]
    public void Portable_AcceptsAllowedMembers_AndLeavesTheCallersOwnTypesAlone(string selector)
    {
        var filtered = MemberFilter.Portable.Parse<Item>(selector);
        PropertySelector<Item> plain = selector;

        Assert.Equal(plain.ToString(), filtered.ToString());
        Assert.Equal(plain.Type, filtered.Type);
    }

    [Theory]
    [InlineData("Created.Ticks")]
    [InlineData("Created.TimeOfDay.Hours")]
    [InlineData("Secret")]
    public void All_AcceptsEverything_AsAPlainParseDoes(string selector)
    {
        var filtered = MemberFilter.All.Parse<Item>(selector);
        PropertySelector<Item> plain = selector;

        Assert.True(MemberFilter.All.IsAll);
        Assert.Equal(plain.ToString(), filtered.ToString());
        Assert.Equal(plain.Type, filtered.Type);
    }

    [Theory]
    [InlineData("Secret", 0)]
    [InlineData("Main.Secret", 5)]
    [InlineData("Lines.Secret", 6)]
    [InlineData("$FN_STR_CONCAT(\"a\", Secret)", 20)]
    [InlineData("Secret|$FN_STR_LENGTH", 0)]
    [InlineData("Lines|$FN_ELM_FIRST|Secret", 20)]
    public void Except_HidesTheCallersOwnMembers(string selector, int position)
    {
        var ex = Assert.Throws<SelectorParseException>(() => NoSecrets.Parse<Item>(selector));

        Assert.Equal(position, ex.Position);
        Assert.Equal("Secret", ex.Token);
    }

    [Fact]
    public void Except_AndOnly_ComposeWithoutChangingTheOriginal()
    {
        var filter = MemberFilter.Portable.Except((_, p) => p.Name == "Secret").Only((t, _) => t != typeof(Line));

        Assert.Throws<SelectorParseException>(() => filter.Parse<Item>("Secret"));
        Assert.Throws<SelectorParseException>(() => filter.Parse<Item>("Created.Ticks"));
        Assert.Throws<SelectorParseException>(() => filter.Parse<Item>("Main.Weight"));
        filter.Parse<Item>("Main");
        filter.Parse<Item>("Created.Year");

        MemberFilter.Portable.Parse<Item>("Secret");
        MemberFilter.Portable.Parse<Item>("Main.Weight");
        Assert.False(filter.IsAll);
        Assert.False(MemberFilter.All.Except((_, _) => false).IsAll);
    }

    [Fact]
    public void TheFilter_SeesTheTypeTheMemberIsReadFrom()
    {
        var seen = new List<(Type, string)>();
        var recording = MemberFilter.All.Only((t, p) => { seen.Add((t, p.Name)); return true; });

        recording.Parse<Item>("Deleted.Year");
        recording.Parse<Item>("Lines.When.Day");

        Assert.Equal(
            [(typeof(Item), "Deleted"), (typeof(DateTime), "Year"), (typeof(Item), "Lines"), (typeof(Line), "When"), (typeof(DateTime), "Day")],
            seen);
    }

    [Fact]
    public void TheFilter_AppliesOnlyInsideParse()
    {
        Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Item>("Created.Ticks"));

        PropertySelector<Item> afterAFailure = "Created.Ticks";
        Assert.Equal(typeof(long), afterAFailure.Type);

        MemberFilter.Portable.Parse<Item>("Created.Year");
        PropertySelector<Item> afterASuccess = "Created.Ticks";
        Assert.Equal(typeof(long), afterASuccess.Type);
    }

    [Fact]
    public void TheFilter_StillReportsOtherErrorsFirst()
    {
        var ex = Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Item>("Created.Nope"));

        Assert.StartsWith("Property 'Nope' was not found", ex.Detail);
    }

    [Fact]
    public void Allows_AndTheComposers_RejectNull()
    {
        var property = typeof(Item).GetProperty(nameof(Item.Age))!;

        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Allows(null!, property));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Allows(typeof(Item), null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Allows((MemberContext)null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Except((Func<Type, PropertyInfo, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Only((Func<Type, PropertyInfo, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Except((Func<MemberContext, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Only((Func<MemberContext, bool>)null!));
    }

    [Fact]
    public void Portable_AllowsExactlyTheProbedMembers()
    {
        string[] Allowed(Type type) => [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => MemberFilter.Portable.Allows(type, p)).Select(p => p.Name).Order()];

        Assert.Equal(["Date", "Day", "DayOfYear", "Hour", "Minute", "Month", "Year"], Allowed(typeof(DateTime)));
        Assert.Equal(["Day", "DayOfYear", "Month", "Year"], Allowed(typeof(DateOnly)));
        Assert.Empty(Allowed(typeof(DateTimeOffset)));
        Assert.Empty(Allowed(typeof(TimeOnly)));
        Assert.Empty(Allowed(typeof(TimeSpan)));
    }

    [Fact]
    public void Portable_NamesOnlyRealMembers()
    {
        foreach (var (type, names) in PortableMembers.AllowedMembers)
        {
            foreach (var name in names)
                Assert.NotNull(type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance));
        }
    }

    [Theory]
    [InlineData("Created.Date.Year", "Year", "Date")]
    [InlineData("Created.Date.Hour", "Hour", "Date")]
    [InlineData("Deleted.Date.Day", "Day", "Date")]
    [InlineData("Lines.When.Date.Month", "Month", "Date")]
    [InlineData("Main.When.Date.Year", "Year", "Date")]
    [InlineData("Created.Date|Year", "Year", "Date")]
    [InlineData("Created.Date|Date", "Date", "Date")]
    [InlineData("(Created.Date)|Year", "Year", "Date")]
    [InlineData("Created|Date|Year", "Year", "Date")]
    [InlineData("$FN_MATH_MAX(Created.Date.Year, 1)", "Year", "Date")]
    [InlineData("Created.Date.Year + 1", "Year", "Date")]
    public void Portable_RejectsADateMemberReadFromAnotherDateMember(string selector, string member, string source)
    {
        var ex = Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Item>(selector));

        Assert.Equal($"Property '{member}' was not found on type 'DateTime'.", ex.Detail);
        Assert.Equal($"Property '{member}' on 'DateTime' is not allowed after '{source}'.", Assert.IsType<SelectorParseException>(ex.InnerException).Detail);
        Assert.Equal(selector.LastIndexOf(member, StringComparison.Ordinal), ex.Position);
    }

    [Theory]
    [InlineData("Created.Date")]
    [InlineData("Created|Year")]
    [InlineData("Created|Date")]
    [InlineData("Main.When.Year")]
    [InlineData("Main|When.Year")]
    [InlineData("Lines.When.Day")]
    [InlineData("Created|$FN_DT_ADDDAYS(1)|Year")]
    public void Portable_AcceptsOneDateMemberAfterAValue_ThatNoDateMemberProduced(string selector)
    {
        MemberFilter.Portable.Parse<Item>(selector);
    }

    [Fact]
    public void AContextPredicate_SeesTheMemberTheValueWasReadFrom()
    {
        var seen = new List<MemberContext>();
        var recording = MemberFilter.All.Only(context => { seen.Add(context); return true; });

        recording.Parse<Item>("Main.When|Day");
        recording.Parse<Item>("Deleted.Year");

        var main = typeof(Item).GetProperty(nameof(Item.Main))!;
        var when = typeof(Line).GetProperty(nameof(Line.When))!;
        var deleted = typeof(Item).GetProperty(nameof(Item.Deleted))!;
        Assert.Equal(
            [
                new MemberContext(typeof(Item), main),
                new MemberContext(typeof(Line), when, main, typeof(Item)),
                new MemberContext(typeof(DateTime), typeof(DateTime).GetProperty(nameof(DateTime.Day))!, when, typeof(Line)),
                new MemberContext(typeof(Item), deleted),
                new MemberContext(typeof(DateTime), typeof(DateTime).GetProperty(nameof(DateTime.Year))!, deleted, typeof(Item)),
            ],
            seen);
    }

    [Fact]
    public void AContextRule_HidesAMemberOnlyAfterItsSource()
    {
        var filter = MemberFilter.All.Except(context => context.Source?.Name == nameof(Item.Main) && context.Property.Name == nameof(Line.Secret));

        Assert.Throws<SelectorParseException>(() => filter.Parse<Item>("Main.Secret"));
        Assert.Throws<SelectorParseException>(() => filter.Parse<Item>("Main|Secret"));
        filter.Parse<Item>("Lines.Secret");
        filter.Parse<Item>("Main.Weight");
        Assert.True(filter.Allows(typeof(Line), typeof(Line).GetProperty(nameof(Line.Secret))!));
    }

    [Fact]
    public void TheSourceDoesNotCrossAFunction_OrLeakIntoTheNextParse()
    {
        var sources = new List<string?>();
        var recording = MemberFilter.All.Only(context => { if (context.Type == typeof(DateTime)) sources.Add(context.Source?.Name); return true; });

        recording.Parse<Item>("Created|$FN_DT_ADDDAYS(1)|Year");
        recording.Parse<Item>("Lines|$FN_ELM_FIRST|When.Day");
        recording.Parse<Item>("$FN_MATH_MAX(Created.Year, Age)");

        Assert.Equal([null, "When", "Created"], sources);
    }

    public class ExplorerTests
    {
        [Fact]
        public void AContextRule_HidesTheMembersBehindItsSource()
        {
            var filter = MemberFilter.All.Except(context => context.Source?.Name == nameof(Item.Main));
            var paths = Nodes(new PropertiesExplorer<Item>().ExplorePropertiesTree(4, filter)).Select(n => n.Path).ToList();

            Assert.Contains("Main", paths);
            Assert.DoesNotContain(paths, p => p.StartsWith("Main."));
            Assert.Contains("Lines.Weight", paths);
        }

        private static IEnumerable<SelectorsMetaDataTree> Nodes(SelectorsMetaDataTree node) =>
            node.Selectors.SelectMany(child => Nodes(child).Prepend(child));

        private static List<string> Paths(MemberFilter? filter)
            => [.. Nodes(new PropertiesExplorer<Item>().ExplorePropertiesTree(4, filter)).Select(n => n.Path)];

        [Fact]
        public void WithoutAFilter_TheTreeListsEverything()
        {
            var paths = Paths(null);

            Assert.Equal(paths, Paths(MemberFilter.All));
            Assert.Contains("Created.Ticks", paths);
            Assert.Contains("Created.TimeOfDay.Hours", paths);
            Assert.Contains("Secret", paths);
        }

        [Fact]
        public void Portable_HidesMembersAndTheirSubtrees()
        {
            var paths = Paths(MemberFilter.Portable);

            Assert.DoesNotContain(paths, p => p.EndsWith(".Ticks"));
            Assert.DoesNotContain(paths, p => p.Contains("TimeOfDay"));
            Assert.DoesNotContain("Duration.Hours", paths);
            Assert.Contains("Duration", paths);
            Assert.Contains("Created.Year", paths);
            Assert.Contains("Deleted.Year", paths);
            Assert.Contains("Lines.When.Month", paths);
            Assert.Contains("Day.DayOfYear", paths);
            Assert.Contains("Secret", paths);
        }

        [Fact]
        public void EveryPathInAFilteredTree_ParsesThroughTheSameFilter()
        {
            var filter = MemberFilter.Portable.Except((_, p) => p.Name == "Secret");
            var paths = Paths(filter);

            Assert.DoesNotContain(paths, p => p.EndsWith("Secret"));
            foreach (var path in paths)
                filter.Parse<Item>(path);
        }
    }
}
