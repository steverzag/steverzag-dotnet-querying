using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Ordering
{
    public record OrderParameters<T> : OrderParameters
    {
        public new PropertySelector<T> OrderBy { get; init; }

        public OrderParameters(PropertySelector<T> orderBy, OrderDirection orderDirection)
            : base(orderBy.ToString(), orderDirection)
        {
            OrderBy = orderBy;
        }

	}

    public record OrderParameters(string OrderBy, OrderDirection OrderDirection);
}
