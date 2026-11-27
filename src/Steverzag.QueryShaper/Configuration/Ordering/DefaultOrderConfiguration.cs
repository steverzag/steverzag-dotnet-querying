using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper.Configuration.Ordering
{
	internal sealed class DefaultOrderConfiguration
	{
		private readonly Dictionary<Type, IReadOnlyList<OrderParameters>> _orders = [];

		public void Configure<T>(IEnumerable<OrderParameters> order)
		{
			ArgumentNullException.ThrowIfNull(order);

			var list = new List<OrderParameters>();
			foreach (var item in order)
			{
				if (item is null)
				{
					throw new ArgumentException("A default order can't contain null.", nameof(order));
				}

				try
				{
					PropertySelector<T> selector = item is OrderParameters<T> typed ? typed.Field : item.Field;
					list.Add(new OrderParameters<T>(selector, item.Direction));
				}
				catch (SelectorParseException ex)
				{
					throw new ArgumentException(ex.Message, nameof(order), ex);
				}
			}

			if (list.Count == 0)
			{
				throw new ArgumentException("A default order needs at least one entry.", nameof(order));
			}

			_orders[typeof(T)] = list;
		}

		public IReadOnlyList<OrderParameters>? For<T>() => _orders.GetValueOrDefault(typeof(T));
	}
}
