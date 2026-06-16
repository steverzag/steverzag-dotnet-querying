namespace ExpDataWorkerBeta.Feature.Configuration.Mapping
{
	/// <summary>
	/// Runtime-facing contract for projecting one entity type onto another using the maps registered
	/// at configuration time. Registration lives on the concrete <see cref="Mapper"/>.
	/// </summary>
	public interface IMapper
	{
		IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source);
	}
}
