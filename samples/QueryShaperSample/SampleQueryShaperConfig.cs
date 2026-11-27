using QueryShaperSample.Catalog;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration;

namespace QueryShaperSample;

public class SampleQueryShaperConfig : QueryShaperConfiguration
{
    protected override void OnConfiguring(QueryShaperConfigurationBuilder b) => b
        .AddMap<Product, ProductDto>(m => m
            .ForMember(d => d.ReviewCount, "Reviews|$FN_AGGR_COUNT")
            .ForMember(d => d.AverageStars, "Reviews.Stars|$FN_AGGR_AVG"))
        // The columns a request's "search" looks in, ignoring case; a client can't choose them.
        .ConfigureSearch<Product>(["Name", "Description"]);
}

// Found by ApplyFromAssembly in Program.cs. A filter or order on a hidden member is rejected
// exactly like a member that doesn't exist.
public sealed class ProductMemberRules : MemberRules<Product>
{
    public override void Configure(MemberRulesBuilder<Product> rules) => rules
        .Hide(p => p.InternalNotes);
}
