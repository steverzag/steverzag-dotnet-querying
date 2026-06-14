using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors
{
	public interface IEnumerableFunctionSelector<T>
	{
		void ValidateEnumerable(string functionSelectorName)
		{
			var isEnumerable = ExpressionUtils.TryGetEnumerableElementType(typeof(T), out var _);
			if (!isEnumerable)
				throw new InvalidOperationException($"{functionSelectorName} can only be applied to enumerable types. RootType: {typeof(T)}");
		}

		Type ValidateAndGetEnumerableType(string functionSelectorName) 
		{
			
			var isEnumerable = ExpressionUtils.TryGetEnumerableElementType(typeof(T), out var elementType);
			if (!isEnumerable || elementType is null)
				throw new InvalidOperationException($"{functionSelectorName} can only be applied to enumerable types. RootType: {typeof(T)}");
			return elementType;
		}
	}
}
