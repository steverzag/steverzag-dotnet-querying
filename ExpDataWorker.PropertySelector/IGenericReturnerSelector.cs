using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors
{
	public interface IGenericReturnerSelector<T>
	{
		void ValidateGeneric(string functionSelectorName) 
		{
			var isGenericType = typeof(T).IsGenericType;
			if (!isGenericType)
				throw new InvalidOperationException($"{functionSelectorName} can only be applied to generic enumerable types. RootType: {typeof(T)}");
		}

		public Type GetSingleGenericArgumentType(string functionSelectorName) 
		{
			var genericArguments = typeof(T).GetGenericArguments();
			if (genericArguments.Length != 1)
				throw new InvalidOperationException($"{functionSelectorName} can only be applied to generic enumerable types with exactly one generic argument. RootType: {typeof(T)}");
			return genericArguments[0];
		}

	}
}
