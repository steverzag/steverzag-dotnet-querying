using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;
using Steverzag.QueryShaper.Utils;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	[Collection(SqliteDatabaseCollection.Name)]
	public abstract class FilterTestBase(SqliteDatabaseFixture database)
	{
		protected SqliteDatabaseFixture Database { get; } = database;

		/// <summary>
		/// Applies a single <see cref="FilterNode"/> to the products and runs it on SQLite.
		/// Returns the matching ids in ascending order.
		/// </summary>
		protected int[] FilterIds(string field, FilterCondition condition, ValueResult2? value = null)
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

		protected static FilterNode Node(string field, FilterCondition condition, ValueResult2? value = null) => new()
		{
			FieldSelector = field,
			FilterCondition = condition,
			SearchValue = value,
		};

		/// <summary>
		/// Converts a theory-data value into a <see cref="ValueResult2"/>. Theory data must stay
		/// serializable, so the conversion happens here and not in the data.
		/// </summary>
		protected static ValueResult2 ToValue(object value) => value switch
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
			_ => throw new ArgumentException($"No ValueResult2 conversion for {value.GetType().Name}", nameof(value)),
		};
	}
}
