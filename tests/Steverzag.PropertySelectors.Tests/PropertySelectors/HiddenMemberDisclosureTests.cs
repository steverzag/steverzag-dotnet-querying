namespace Steverzag.PropertySelectors.Tests;

/// <summary>A hidden member must look exactly like a missing one, and the case hint must not name it.</summary>
public class HiddenMemberDisclosureTests
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
        public Address Home { get; set; } = new();
        public Address Work { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public List<Account> Friends { get; set; } = [];
    }

    // The same shape without the hidden member: what a client sees for a name that really doesn't exist.
    public class Bare
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static readonly MemberFilter NoHash = MemberFilter.All.Hide<Account>(a => a.PasswordHash);

    private static SelectorParseException Fails<T>(MemberFilter filter, string selector)
        => Assert.Throws<SelectorParseException>(() => filter.Parse<T>(selector));

    [Theory]
    [InlineData("PasswordHash", 0)]
    [InlineData("Friends.PasswordHash", 8)]
    [InlineData("Friends|PasswordHash", 8)]
    [InlineData("$FN_MATH_MAX(Id, PasswordHash|$FN_STR_LENGTH)", 17)]
    [InlineData("Id + PasswordHash|$FN_STR_LENGTH", 5)]
    public void AHiddenMember_IsReportedLikeAMissingOne(string selector, int position)
    {
        var hidden = Fails<Account>(NoHash, selector);

        Assert.Equal("Property 'PasswordHash' was not found on type 'Account'.", hidden.Detail);
        Assert.Equal(position, hidden.Position);
        Assert.Equal("PasswordHash", hidden.Token);
        Assert.Equal(selector, hidden.Selector);
    }

    [Fact]
    public void AHiddenMember_AndAMissingOne_HaveTheSameMessage()
    {
        var hidden = Fails<Account>(NoHash, "PasswordHash");
        var missing = Fails<Bare>(MemberFilter.All, "PasswordHash");

        Assert.Equal(missing.Message.Replace("'Bare'", "'Account'"), hidden.Message);
    }

    [Theory]
    [InlineData("passwordHash")]
    [InlineData("PASSWORDHASH")]
    [InlineData("Friends.passwordhash")]
    [InlineData("Friends|passwordHash")]
    [InlineData("$FN_MATH_MAX(Id, passwordHash|$FN_STR_LENGTH)")]
    public void AWronglyCasedHiddenMember_GetsNoHint(string selector)
    {
        var ex = Fails<Account>(NoHash, selector);

        Assert.Contains("was not found on type 'Account'.", ex.Detail);
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            Assert.DoesNotContain("case-sensitive", e.Message);
            Assert.DoesNotContain("PasswordHash", e.Message);
            Assert.DoesNotContain("not allowed", e.Message);
        }
    }

    [Theory]
    [InlineData("name", "Name")]
    [InlineData("Friends.name", "Name")]
    [InlineData("home.Street", "Home")]
    public void AWronglyCasedVisibleMember_StillGetsTheHint(string selector, string real)
        => Assert.EndsWith($"Property names are case-sensitive: '{real}'.", Fails<Account>(NoHash, selector).Detail);

    [Fact]
    public void WithoutAFilter_TheHintIsUnchanged()
    {
        var ex = Assert.Throws<SelectorParseException>(() => { PropertySelector<Account> _ = "passwordHash"; });
        Assert.EndsWith("Property names are case-sensitive: 'PasswordHash'.", ex.Detail);
    }

    [Fact]
    public void ARuleThatDependsOnTheSource_HidesTheHintToo()
    {
        var filter = MemberFilter.All.Except(c => c.Source?.Name == nameof(Account.Work));

        Assert.Equal("Property 'Street' was not found on type 'Address'.", Fails<Account>(filter, "Work.Street").Detail);
        Assert.Equal("Property 'street' was not found on type 'Address'.", Fails<Account>(filter, "Work.street").Detail);
        Assert.EndsWith("Property names are case-sensitive: 'Street'.", Fails<Account>(filter, "Home.street").Detail);
    }

    [Fact]
    public void TheReason_IsKeptAsTheInnerException_ForServerLogs()
    {
        var ex = Fails<Account>(NoHash, "Friends.PasswordHash");

        var reason = Assert.IsType<SelectorParseException>(ex.InnerException);
        Assert.Equal("Property 'PasswordHash' on 'Account' is not allowed.", reason.Detail);
        Assert.Equal(8, reason.Position);
    }

    [Fact]
    public void ADateMemberAfterAnother_IsReportedAsNotFound_WithTheReasonInside()
    {
        var ex = Assert.Throws<SelectorParseException>(() => MemberFilter.Portable.Parse<Account>("CreatedAt.Date.Year"));

        Assert.Equal("Property 'Year' was not found on type 'DateTime'.", ex.Detail);
        Assert.Equal("Property 'Year' on 'DateTime' is not allowed after 'Date'.", Assert.IsType<SelectorParseException>(ex.InnerException).Detail);
    }
}
