

using ExpDataWorker.PropertySelectors;

namespace ExpDataWorkerBeta.Feature.Ordering
{
	public record OrderParameters(PropertySelector OrderBy, OrderDirection OrderDirection);
}
