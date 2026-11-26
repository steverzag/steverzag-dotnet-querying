using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public abstract class FilterTestBase(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		/// <summary>
		/// Applies a single <see cref="FilterNode"/> to the products and runs it on SQLite.
		/// Returns the matching ids in ascending order.
		/// </summary>
		protected int[] FilterIds(string field, FilterCondition condition, FilterValue? value = null)
			=> FilterIds([Node(field, condition, value)]);

		protected int[] FilterIds(IEnumerable<FilterNode> filters)
		{
			using var context = Database.CreateContext();
			return context.Products
				.ApplyLogicsFilter(filters)
				.OrderBy(p => p.Id)
				.Select(p => p.Id)
				.ToArray();
		}

		protected static ConditionNode Node(string field, FilterCondition condition, FilterValue? value = null) => new()
		{
			FieldSelector = field,
			FilterCondition = condition,
			Value = value,
		};

		/// <summary>
		/// Converts a theory-data value into a <see cref="FilterValue"/>. Theory data must stay
		/// serializable, so the conversion happens here and not in the data.
		/// </summary>
		protected static FilterValue ToValue(object value) => value switch
		{
			bool v => v,
			byte v => v,
			short v => v,
			int v => v,
			long v => v,
			float v => v,
			double v => v,
			decimal v => v,
			string v => v,
			Guid v => v,
			DateTime v => v,
			DateOnly v => v,
			TimeOnly v => v,
			List<object> v => v,
			_ => throw new ArgumentException($"No FilterValue conversion for {value.GetType().Name}", nameof(value)),
		};
	}
}
