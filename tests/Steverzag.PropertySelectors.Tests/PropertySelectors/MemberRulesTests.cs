using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors.Tests;

public class MemberRulesTests
{
    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }

    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public decimal CreditLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public Address Home { get; set; } = new();
        public List<Note> Notes { get; set; } = [];
        public int Field;
    }

    public class Admin : Account
    {
        public int Level { get; set; }
    }

    public class Note
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public Account Author { get; set; } = new();
    }

    // Picked up by ApplyFromAssembly; nothing else in this assembly derives from MemberRules.
    public sealed class AccountRules : MemberRules<Account>
    {
        public override void Configure(MemberRulesBuilder<Account> rules) => rules
            .Hide(a => a.PasswordHash)
            .Hide(a => a.CreditLimit);
    }

    public sealed class AddressRules : MemberRules<Address>
    {
        public override void Configure(MemberRulesBuilder<Address> rules) => rules.Only(a => a.City);
    }

    public abstract class AbstractRules : MemberRules<Note>
    {
        public override void Configure(MemberRulesBuilder<Note> rules) => rules.Hide(n => n.Name);
    }

    public sealed class RulesWithoutADefaultConstructor(string member) : MemberRules<Note>
    {
        public override void Configure(MemberRulesBuilder<Note> rules) => rules.Hide(n => n.Name == member ? n.Id : n.Id);
    }

    private static bool Rejects(MemberFilter filter, string selector)
    {
        try
        {
            filter.Parse<Account>(selector);
            return false;
        }
        catch (SelectorParseException ex) when (ex.InnerException is SelectorParseException { Detail: var reason } && reason.EndsWith("is not allowed."))
        {
            return true;
        }
    }

    [Fact]
    public void Hide_HidesTheMemberOnItsType_AndNowhereElse()
    {
        var filter = MemberFilter.All.Hide<Account>(a => a.PasswordHash);

        Assert.True(Rejects(filter, "PasswordHash"));
        Assert.True(Rejects(filter, "Notes.Author.PasswordHash"));
        Assert.False(Rejects(filter, "Notes.PasswordHash"));
        Assert.False(Rejects(filter, "Name"));
    }

    [Fact]
    public void Hide_CoversDerivedTypes()
    {
        var filter = MemberFilter.All.Hide<Account>(a => a.PasswordHash);
        var property = typeof(Admin).GetProperty(nameof(Admin.PasswordHash))!;

        Assert.False(filter.Allows(typeof(Admin), property));
        Assert.True(filter.Allows(typeof(Note), typeof(Note).GetProperty(nameof(Note.PasswordHash))!));
    }

    [Fact]
    public void ARuleOnADerivedType_LeavesTheBaseTypeAlone()
    {
        var filter = MemberFilter.All.Hide<Admin>(a => a.Name);
        var name = typeof(Account).GetProperty(nameof(Account.Name))!;

        Assert.False(filter.Allows(typeof(Admin), typeof(Admin).GetProperty(nameof(Admin.Name))!));
        Assert.True(filter.Allows(typeof(Account), name));
    }

    [Fact]
    public void Only_AllowsTheListedMembersOnItsType_AndLeavesOtherTypesAlone()
    {
        var filter = MemberFilter.All.Only<Account>(a => a.Id, a => a.Name, a => a.Notes);

        Assert.False(Rejects(filter, "Id"));
        Assert.False(Rejects(filter, "Notes.PasswordHash"));
        Assert.True(Rejects(filter, "PasswordHash"));
        Assert.True(Rejects(filter, "CreatedAt"));
        Assert.True(Rejects(filter, "Notes.Author.CreatedAt"));
        Assert.False(filter.Allows(typeof(Admin), typeof(Admin).GetProperty(nameof(Admin.Level))!));
    }

    [Fact]
    public void HidingANavigation_HidesEverythingBehindIt()
    {
        var filter = MemberFilter.All.Hide<Account>(a => a.Home);
        var paths = Nodes(new PropertiesExplorer<Account>().ExplorePropertiesTree(4, filter)).Select(n => n.Path).ToList();

        Assert.True(Rejects(filter, "Home.City"));
        Assert.DoesNotContain(paths, p => p.StartsWith("Home"));
        Assert.Contains("Notes.Name", paths);
    }

    [Fact]
    public void ValueTypeMembers_AreAccepted()
    {
        var filter = MemberFilter.All.Hide<Account>(a => a.CreditLimit, a => a.CreatedAt, a => a.Id);

        Assert.True(Rejects(filter, "CreditLimit"));
        Assert.True(Rejects(filter, "CreatedAt.Year"));
        Assert.True(Rejects(filter, "$FN_MATH_MAX(Id, 1)"));
    }

    [Fact]
    public void For_GroupsRules_AndMatchesTheFlatForm()
    {
        var grouped = MemberFilter.Portable
            .For<Account>(a => a.Hide(x => x.PasswordHash).Hide(x => x.CreditLimit))
            .For<Address>(a => a.Only(x => x.City));
        var flat = MemberFilter.Portable
            .Hide<Account>(x => x.PasswordHash)
            .Hide<Account>(x => x.CreditLimit)
            .Only<Address>(x => x.City);

        foreach (var selector in new[] { "PasswordHash", "CreditLimit", "Home.Street", "Home.City", "Name", "CreatedAt.Ticks", "CreatedAt.Year" })
            Assert.Equal(Rejects(flat, selector), Rejects(grouped, selector));

        Assert.True(Rejects(grouped, "Home.Street"));
        Assert.False(Rejects(grouped, "Home.City"));
        Assert.True(Rejects(grouped, "CreatedAt.Ticks"));
    }

    [Fact]
    public void RulesForTheSameType_Compose_AndTheStrictestWins()
    {
        var hides = MemberFilter.All.Hide<Account>(a => a.PasswordHash).Hide<Account>(a => a.CreditLimit);
        Assert.True(Rejects(hides, "PasswordHash"));
        Assert.True(Rejects(hides, "CreditLimit"));

        var onlys = MemberFilter.All.Only<Account>(a => a.Id, a => a.Name).Only<Account>(a => a.Name, a => a.CreatedAt);
        Assert.False(Rejects(onlys, "Name"));
        Assert.True(Rejects(onlys, "Id"));
        Assert.True(Rejects(onlys, "CreatedAt"));

        var both = MemberFilter.All.Only<Account>(a => a.Id, a => a.Name).Hide<Account>(a => a.Name);
        Assert.False(Rejects(both, "Id"));
        Assert.True(Rejects(both, "Name"));
    }

    [Fact]
    public void Rules_ReturnANewFilter_AndLeaveTheOriginalAlone()
    {
        var filter = MemberFilter.Portable.Hide<Account>(a => a.PasswordHash);

        Assert.NotSame(MemberFilter.Portable, filter);
        Assert.False(filter.IsAll);
        Assert.False(Rejects(MemberFilter.Portable, "PasswordHash"));
        Assert.True(MemberFilter.All.IsAll);
    }

    [Fact]
    public void ABuilderKeptAfterFor_DoesNotChangeTheFilter()
    {
        MemberRulesBuilder<Account>? kept = null;
        var filter = MemberFilter.All.For<Account>(a => { kept = a; a.Hide(x => x.PasswordHash); });

        kept!.Hide(x => x.Name);

        Assert.True(Rejects(filter, "PasswordHash"));
        Assert.False(Rejects(filter, "Name"));
    }

    [Fact]
    public void ANestedMember_IsRejected_WithAHintForItsType()
    {
        var ex = Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => a.Home.Street));

        Assert.Contains("Expected a property of Account", ex.Message);
        Assert.Contains("use For<Address>", ex.Message);
    }

    [Fact]
    public void AnythingButADirectProperty_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => a));
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => a.Field));
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => a.Name.ToUpper()));
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => "Name"));
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => a.Id + 1));
        var other = new Account();
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>(a => other.Name));
    }

    [Fact]
    public void EmptyOrNullArguments_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Hide<Account>());
        Assert.Throws<ArgumentException>(() => MemberFilter.All.Only<Account>());
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Hide<Account>(null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Hide<Account>(a => a.Id, null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.For<Account>(null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Apply(null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.Apply(new AccountRules(), null!));
        Assert.Throws<ArgumentNullException>(() => MemberFilter.All.ApplyFromAssembly(null!));
    }

    [Fact]
    public void AHiddenMember_IsReportedAtItsPosition()
    {
        var filter = MemberFilter.All.Apply(new AccountRules());

        var ex = Assert.Throws<SelectorParseException>(() => filter.Parse<Account>("Notes.Author.PasswordHash"));

        Assert.Equal("Property 'PasswordHash' was not found on type 'Account'.", ex.Detail);
        Assert.Equal("Property 'PasswordHash' on 'Account' is not allowed.", Assert.IsType<SelectorParseException>(ex.InnerException).Detail);
        Assert.Equal(13, ex.Position);
    }

    [Fact]
    public void Apply_AddsEachRulesClass()
    {
        var filter = MemberFilter.Portable.Apply(new AccountRules(), new AddressRules());

        Assert.True(Rejects(filter, "PasswordHash"));
        Assert.True(Rejects(filter, "CreditLimit"));
        Assert.True(Rejects(filter, "Home.Street"));
        Assert.False(Rejects(filter, "Home.City"));
        Assert.True(Rejects(filter, "CreatedAt.Ticks"));
        Assert.False(Rejects(filter, "Notes.Name"));
    }

    [Fact]
    public void ApplyFromAssembly_FindsTheConcreteRulesClasses_WithADefaultConstructor()
    {
        var scanned = MemberFilter.Portable.ApplyFromAssembly(typeof(MemberRulesTests).Assembly);
        var applied = MemberFilter.Portable.Apply(new AccountRules(), new AddressRules());

        foreach (var selector in new[] { "PasswordHash", "CreditLimit", "Home.Street", "Home.City", "Notes.Name", "Notes.Id", "CreatedAt.Ticks" })
            Assert.Equal(Rejects(applied, selector), Rejects(scanned, selector));

        Assert.False(Rejects(scanned, "Notes.Name"));
        Assert.False(Rejects(scanned, "Notes.Id"));
        _ = new RulesWithoutADefaultConstructor("x");
    }

    private static IEnumerable<SelectorsMetaDataTree> Nodes(SelectorsMetaDataTree node) =>
        node.Selectors.SelectMany(child => Nodes(child).Prepend(child));
}
