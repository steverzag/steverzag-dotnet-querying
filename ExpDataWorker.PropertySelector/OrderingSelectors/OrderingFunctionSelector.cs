using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.OrderingSelectors
{
	public record OrderingFunctionSelector<T> : FunctionPropertySelector<T>
	{

		public OrderingFunctionSelector(string PlainValue, List<PropertySelector>? parameters = null) : base(PlainValue, parameters)
		{
		}

		public override Type? InitSelectorTypeToApply()
		{
			var isEnumerable = ExpressionUtils.TryGetEnumerableElementType(typeof(T), out var elementType);
			if (!isEnumerable || elementType is null)
				throw new InvalidOperationException($"ElementFunctionSelectors can only be applied to enumerable types. RootType: {typeof(T)}");
			return typeof(IOrderedQueryable<>).MakeGenericType(elementType);
		}

		public override void InitializeOverloads()
		{
			throw new NotImplementedException();
		}
	}
}
