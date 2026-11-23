using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Ordering
{
    public record OrderParameters<T> : OrderParameters
    {
        public new PropertySelector<T> OrderBy { get; init; }

        // Known gap: the base string holds only the selector's first segment (PlainValue), e.g. "Reviews" for
        // "Reviews|$FN_AGGR_COUNT". ApplyOrder uses the typed OrderBy, so ordering is correct; only readers of the
        // base string see the short form. See ROADMAP Phase 3 (PropertySelector<T>.ToString).
        public OrderParameters(PropertySelector<T> orderBy, OrderDirection orderDirection)
            : base(orderBy.PlainValue, orderDirection)
        {
            OrderBy = orderBy;
        }

	}

    public record OrderParameters(string OrderBy, OrderDirection OrderDirection);
}
