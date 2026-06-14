using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ElementSelectors
{
	public record ElementFunctionSelector<T> : FunctionPropertySelector<T>
	{

		public ElementFunctionSelector(string PlainValue, List<PropertySelector>? parameters = null) : base(PlainValue, parameters)
		{
		}

		public override Type? InitSelectorTypeToApply()
		{
			var isEnumerable = ExpressionUtils.TryGetEnumerableElementType(typeof(T), out var elementType);
			if (!isEnumerable || elementType is null)
				throw new InvalidOperationException($"ElementFunctionSelectors can only be applied to enumerable types. RootType: {typeof(T)}");
			return elementType;
		}

		public override void InitializeOverloads()
		{
			throw new NotImplementedException();
		}
	}
}
