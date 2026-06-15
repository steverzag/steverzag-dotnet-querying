using ExpDataWorker.PropertySelectors;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpDataWorkerBeta.Feature.Ordering
{
    public record OrderParameters<T> : OrderParameters
    {
        public new PropertySelector<T> OrderBy { get; init; }

        public OrderParameters(PropertySelector<T> orderBy, OrderDirection orderDirection)
            : base(orderBy.PlainValue, orderDirection)
        {
            OrderBy = orderBy;
        }

        internal static OrderParameters<T> Create(OrderParameters op)
            => new(op.OrderBy, op.OrderDirection);

	}

    public record OrderParameters(string OrderBy, OrderDirection OrderDirection);
}
